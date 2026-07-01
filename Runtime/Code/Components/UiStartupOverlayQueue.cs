using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	/// <summary>
	/// Sequential runner for <see cref="IUiStartupOverlay"/>s. Overlays are added by
	/// <see cref="UiInitialActiveState"/> (or any other orchestrator) when their host
	/// GameObject is activated; the dashboard then calls <see cref="Run"/> once and
	/// the queue walks through the pending entries in ascending
	/// <see cref="IUiStartupOverlay.Priority"/> order.
	///
	/// State model:
	/// <list type="bullet">
	///   <item><b>Pending list</b>: overlays whose <see cref="IUiStartupOverlay.OverlayId"/>
	///         hasn't been processed yet this app session. An overlay leaves this list only
	///         once it has actually been shown (its <c>onClosed</c> fired).</item>
	///   <item><b>Shown-this-session set</b>: every OverlayId that has been processed.
	///         New <see cref="Add"/> calls with a matching id are silently rejected, so
	///         re-instantiated overlays (after a scene switch) don't fire again.</item>
	/// </list>
	///
	/// Execution model: a single coroutine on <see cref="CoRoutineRunner"/> walks the
	/// pending list, calls <see cref="IUiStartupOverlay.Show"/>, waits for the supplied
	/// callback to fire, then moves on. Destroyed (Unity-null) entries are purged at the
	/// top of every iteration, so overlays don't need to deregister on destroy.
	///
	/// Use <see cref="ResetSession"/> to clear the shown-id set (e.g., for QA / cheats /
	/// editor menu items that want to replay the sequence within one play session).
	/// </summary>
	public static class UiStartupOverlayQueue
	{
		private static readonly List<IUiStartupOverlay> s_pending = new();
		private static readonly HashSet<string> s_shownIdsThisSession = new();
		private static bool s_running;
		private static Action s_onAllDone;
		private static Coroutine s_coroutine;
		private static IUiStartupOverlay s_currentCandidate;

		/// <summary>
		/// Enable verbose Debug.Log output for each add / advance / completion event.
		/// Off by default — flip on from your bootstrap code while diagnosing ordering issues.
		/// </summary>
		public static bool EnableLogging = false;

		/// <summary>
		/// Full-screen input blocker, kept blocking for the ENTIRE duration of a <see cref="Run"/>
		/// sequence. The queue engages it synchronously when the run starts and releases it once
		/// every overlay has been processed (or the run is stopped / cleared / throws).
		///
		/// This closes the input gaps that exist <i>before</i> the first overlay's own
		/// click-catcher appears and <i>between</i> consecutive overlays. Normally assigned
		/// automatically by <see cref="UiStartupOverlayBlocker"/> from its Awake — see that
		/// component for the required scene setup.
		///
		/// SHOULD be assigned before <see cref="Run"/>; an unassigned blocker is asserted in
		/// development builds (and the sequence then runs unblocked, as before — never dead-locked).
		/// </summary>
		public static UiStartupOverlayBlocker Blocker { get; set; }

		/// <summary>
		/// Failsafe timeout (unscaled seconds): the longest the queue waits for a single overlay's
		/// onClosed callback before logging an error and forcibly advancing. Guards against an
		/// overlay that shows but never reports closed, which would otherwise leave the
		/// <see cref="Blocker"/> active forever.
		///
		/// This is a last-resort recovery for a <i>broken</i> overlay, NOT a UX dismissal timer:
		/// these overlays are usually closed by a user tap, so the value must comfortably exceed
		/// the longest plausible time a user dwells on one (reading a changelog, etc.). Firing it
		/// while an overlay is legitimately open force-advances the queue. Set &lt;= 0 to wait
		/// indefinitely.
		/// </summary>
		public static float OverlayTimeoutSeconds = 120f;

		/// <summary>
		/// Queue an overlay for the next (or currently running) <see cref="Run"/>. Silently
		/// rejected if the overlay is null, already pending, has an empty <see cref="IUiStartupOverlay.OverlayId"/>,
		/// or has already been processed this session.
		/// </summary>
		public static void Add( IUiStartupOverlay _overlay )
		{
			if (_overlay == null)
				return;

			if (string.IsNullOrEmpty(_overlay.OverlayId))
			{
				Debug.LogWarning($"[UiStartupOverlayQueue] '{Describe(_overlay)}' has empty OverlayId — ignored");
				return;
			}

			if (s_shownIdsThisSession.Contains(_overlay.OverlayId))
				return;

			if (s_pending.Contains(_overlay))
				return;

			s_pending.Add(_overlay);

			if (EnableLogging)
				Debug.Log($"[UiStartupOverlayQueue] Added '{Describe(_overlay)}' id='{_overlay.OverlayId}' priority={_overlay.Priority}");
		}

		/// <summary>
		/// Forget every overlay that's been processed this session. Subsequent <see cref="Add"/>
		/// calls will accept ids that were previously rejected. Useful for cheat / QA workflows
		/// that need to replay the startup sequence without restarting the app.
		/// </summary>
		public static void ResetSession()
		{
			s_shownIdsThisSession.Clear();
		}

		/// <summary>
		/// Test/diagnostic hook. Clears the pending list AND the session-shown set.
		/// </summary>
		public static void Clear()
		{
			s_pending.Clear();
			s_shownIdsThisSession.Clear();
			s_running = false;
			s_onAllDone = null;
			s_currentCandidate = null;
			if (s_coroutine != null && CoRoutineRunner.Instance != null)
				CoRoutineRunner.Instance.StopCoroutine(s_coroutine);
			s_coroutine = null;

			// StopCoroutine does not unwind the iterator's finally, so release the blocker here.
			SetBlocking(false);
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
		}

		/// <summary>
		/// Halt the running coroutine without invoking <c>onAllDone</c>. The currently-displaying
		/// overlay (if any) is marked as shown so it won't repeat next session; pending overlays
		/// remain pending so the next <see cref="Run"/> call resumes from where this one left off.
		///
		/// Call this BEFORE triggering a scene change from inside an overlay's action — otherwise
		/// the queue would advance to the next overlay during the scene transition (its
		/// <c>Show</c> would fire while the user is no longer on the dashboard, marking it as
		/// shown but never letting them actually see it).
		/// </summary>
		public static void Stop()
		{
			// Always drop the scene hook, even on the early-out below, so it can't outlive the run.
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;

			if (!s_running)
				return;

			if (s_currentCandidate != null && !string.IsNullOrEmpty(s_currentCandidate.OverlayId))
			{
				s_shownIdsThisSession.Add(s_currentCandidate.OverlayId);
				s_pending.Remove(s_currentCandidate);
			}

			if (s_coroutine != null && CoRoutineRunner.Instance != null)
				CoRoutineRunner.Instance.StopCoroutine(s_coroutine);

			if (EnableLogging)
				Debug.Log($"[UiStartupOverlayQueue] Stopped — current='{Describe(s_currentCandidate)}' marked shown, {s_pending.Count} remain pending");

			s_coroutine = null;
			s_currentCandidate = null;
			s_running = false;
			s_onAllDone = null;

			// StopCoroutine does not unwind the iterator's finally, so release the blocker here.
			SetBlocking(false);
		}

		/// <summary>
		/// Walk all pending overlays in priority order, then invoke <paramref name="_onAllDone"/>.
		/// Re-entrant calls are ignored (a warning is logged and the callback fires immediately).
		/// </summary>
		public static void Run( Action _onAllDone = null )
		{
			if (s_running)
			{
				Debug.LogWarning("[UiStartupOverlayQueue] Run() called while already running — ignored");
				_onAllDone?.Invoke();
				return;
			}

			s_running = true;
			s_onAllDone = _onAllDone;

			var runner = CoRoutineRunner.Instance;
			if (runner == null)
			{
				Debug.LogError("[UiStartupOverlayQueue] CoRoutineRunner not available — completing immediately");
				s_running = false;
				s_onAllDone = null;
				_onAllDone?.Invoke();
				return;
			}

			// Raise the input blocker synchronously, before the first overlay shows, so the gap
			// before its own click-catcher appears is covered. Only do so once we're committed to
			// starting the coroutine (which owns lowering it again via its finally block).
			Debug.Assert(Blocker != null, "[UiStartupOverlayQueue] Blocker must be assigned before Run() — UI behind the overlays will not be blocked");
			SetBlocking(true);

			// Auto-teardown if the active scene changes mid-sequence (two of the event dialogs load
			// another scene). Guarantees the blocker is released and the coroutine stopped even if a
			// caller forgets Stop(). Defensive '-=' first so a stray subscription can't accumulate.
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			SceneManager.activeSceneChanged += OnActiveSceneChanged;

			s_coroutine = runner.StartCoroutine(RunCoroutine());
		}

		private static IEnumerator RunCoroutine()
		{
			// finally runs on normal completion AND on exceptions thrown inside the loop, so the
			// blocker can never be stranded on those paths. (The manual Stop()/Clear() abort paths
			// release it themselves, since Unity's StopCoroutine does not unwind this finally.)
			try
			{
				while (true)
				{
					PurgeDestroyed();

					s_currentCandidate = FindLowestPriority();
					if (s_currentCandidate == null)
						break;

					if (EnableLogging)
						Debug.Log($"[UiStartupOverlayQueue] Showing '{Describe(s_currentCandidate)}' id='{s_currentCandidate.OverlayId}' priority={s_currentCandidate.Priority}");

					bool done = false;
					s_currentCandidate.Show(() => done = true);

					float elapsed = 0f;
					yield return new WaitUntil(() =>
					{
						if (done)
							return true;
						if (OverlayTimeoutSeconds <= 0f)
							return false;
						elapsed += Time.unscaledDeltaTime;
						return elapsed >= OverlayTimeoutSeconds;
					});

					if (!done)
						Debug.LogError($"[UiStartupOverlayQueue] '{Describe(s_currentCandidate)}' did not invoke its onClosed within {OverlayTimeoutSeconds}s — forcing advance");

					s_shownIdsThisSession.Add(s_currentCandidate.OverlayId);
					s_pending.Remove(s_currentCandidate);
					s_currentCandidate = null;
				}
			}
			finally
			{
				SetBlocking(false);
				SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			}

			if (EnableLogging)
				Debug.Log("[UiStartupOverlayQueue] All overlays completed");

			s_coroutine = null;
			s_currentCandidate = null;
			s_running = false;
			var cb = s_onAllDone;
			s_onAllDone = null;
			cb?.Invoke();
		}

		private static void SetBlocking( bool _blocking )
		{
			// UnityEngine.Object '!= null' correctly treats a destroyed (e.g. scene-unloaded)
			// blocker as null, so a stale reference after a scene change can't throw here.
			if (Blocker != null)
				Blocker.SetBlocking(_blocking);
		}

		private static void OnActiveSceneChanged( Scene _from, Scene _to )
		{
			// We left the scene mid-sequence. Stop() releases the blocker, stops the coroutine,
			// marks the current overlay as shown and keeps the rest pending for the next Run() on
			// return — and removes this handler itself. The blocker GameObject lives in the
			// unloaded scene, so releasing it here (before it is destroyed) is the clean path.
			Stop();
		}

		private static void PurgeDestroyed()
		{
			for (int i = s_pending.Count - 1; i >= 0; i--)
			{
				var entry = s_pending[i];
				if (entry == null || (entry is UnityEngine.Object obj && obj == null))
					s_pending.RemoveAt(i);
			}
		}

		private static IUiStartupOverlay FindLowestPriority()
		{
			IUiStartupOverlay best = null;
			int bestPriority = int.MaxValue;
			foreach (var entry in s_pending)
			{
				if (entry.Priority < bestPriority)
				{
					bestPriority = entry.Priority;
					best = entry;
				}
			}
			return best;
		}

		private static string Describe( IUiStartupOverlay _overlay )
		{
			if (_overlay is UnityEngine.Object obj && obj != null)
				return obj.name;
			return _overlay?.GetType().Name ?? "<null>";
		}
	}
}
