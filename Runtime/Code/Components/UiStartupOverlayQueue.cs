using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

		/// <summary>
		/// Enable verbose Debug.Log output for each add / advance / completion event.
		/// Off by default — flip on from your bootstrap code while diagnosing ordering issues.
		/// </summary>
		public static bool EnableLogging = false;

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

			runner.StartCoroutine(RunCoroutine());
		}

		private static IEnumerator RunCoroutine()
		{
			while (true)
			{
				PurgeDestroyed();

				IUiStartupOverlay candidate = FindLowestPriority();
				if (candidate == null)
					break;

				if (EnableLogging)
					Debug.Log($"[UiStartupOverlayQueue] Showing '{Describe(candidate)}' id='{candidate.OverlayId}' priority={candidate.Priority}");

				bool done = false;
				candidate.Show(() => done = true);
				yield return new WaitUntil(() => done);

				s_shownIdsThisSession.Add(candidate.OverlayId);
				s_pending.Remove(candidate);
			}

			if (EnableLogging)
				Debug.Log("[UiStartupOverlayQueue] All overlays completed");

			s_running = false;
			var cb = s_onAllDone;
			s_onAllDone = null;
			cb?.Invoke();
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
