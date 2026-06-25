using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Sequential runner for <see cref="IUiStartupOverlay"/>s. Overlays self-register on Awake
	/// (or whenever they come online) and the orchestrator — typically a dashboard screen —
	/// calls <see cref="Run"/> once when the user is ready to see them. Entries fire in
	/// ascending <see cref="IUiStartupOverlay.Priority"/> order; each one signals completion
	/// via the callback passed to its <c>Show</c>, and only then does the queue advance to
	/// the next.
	///
	/// The order survives any overlay being inactive: an overlay that has nothing to show
	/// just invokes its <c>onClosed</c> callback immediately, and the queue moves on.
	///
	/// Late-registration tolerance: the queue does NOT snapshot the registered list when
	/// <see cref="Run"/> is called. Instead it re-scans the live registry after every
	/// overlay completes, looking for the next lowest-priority entry whose
	/// <see cref="IUiStartupOverlay.OverlayId"/> hasn't been processed yet this session.
	///
	/// Session tracking by id: each overlay declares a stable <see cref="IUiStartupOverlay.OverlayId"/>.
	/// The queue records that id when an overlay's <c>Show</c> is invoked and skips any
	/// future registrations with the same id for the remainder of the app session. This is
	/// crucial because UI scenes typically destroy and re-create their overlays — the new
	/// instances report the same id and get skipped.
	///
	/// Use <see cref="ResetSession"/> to clear the shown-id set (e.g., for QA / cheats /
	/// editor menu items that want to replay the sequence within one play session).
	/// </summary>
	public static class UiStartupOverlayQueue
	{
		private static readonly List<IUiStartupOverlay> s_registered = new();
		private static readonly HashSet<string> s_shownIdsThisSession = new();
		private static bool s_running;
		private static Action s_onAllDone;

		/// <summary>
		/// Enable verbose Debug.Log output for each registration / advance / completion event.
		/// Off by default — flip on from your bootstrap code while diagnosing ordering issues.
		/// </summary>
		public static bool EnableLogging = false;

		/// <summary>
		/// Add an overlay to the registry. Safe to call multiple times — duplicate instances
		/// are ignored. Overlays whose <see cref="IUiStartupOverlay.OverlayId"/> has already
		/// been processed this session will be picked up by <see cref="Next"/> but immediately
		/// skipped.
		/// </summary>
		public static void Register( IUiStartupOverlay _overlay )
		{
			if (_overlay == null)
				return;
			if (s_registered.Contains(_overlay))
				return;
			s_registered.Add(_overlay);

			if (EnableLogging)
				Debug.Log($"[UiStartupOverlayQueue] Registered overlay '{Describe(_overlay)}' id='{_overlay.OverlayId}' priority={_overlay.Priority}");
		}

		public static void Unregister( IUiStartupOverlay _overlay )
		{
			s_registered.Remove(_overlay);
		}

		/// <summary>
		/// Forget every overlay that's been processed this session. Subsequent <see cref="Run"/>
		/// calls will re-process all registered overlays from scratch. Useful for cheat /
		/// QA workflows that need to replay the startup sequence without restarting the app.
		/// </summary>
		public static void ResetSession()
		{
			s_shownIdsThisSession.Clear();
		}

		/// <summary>
		/// Test/diagnostic hook. Clears every registration AND the session-shown set.
		/// Production code should rely on per-overlay Unregister in OnDestroy.
		/// </summary>
		public static void Clear()
		{
			s_registered.Clear();
			s_shownIdsThisSession.Clear();
			s_running = false;
			s_onAllDone = null;
		}

		/// <summary>
		/// Walk all registered overlays in priority order, re-scanning after each entry so late
		/// registrations are still honoured. Re-entrant calls are ignored (a warning is logged
		/// and <paramref name="_onAllDone"/> fires immediately).
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
			Next();
		}

		private static void Next()
		{
			// Live rescan of the registered list. Pick the lowest-priority entry whose
			// OverlayId hasn't been processed this session, skipping destroyed Unity objects.
			IUiStartupOverlay candidate = null;
			int bestPriority = int.MaxValue;
			foreach (var entry in s_registered)
			{
				if (entry == null)
					continue;
				if (entry is UnityEngine.Object obj && obj == null)
					continue;
				if (string.IsNullOrEmpty(entry.OverlayId))
				{
					Debug.LogWarning($"[UiStartupOverlayQueue] Overlay '{Describe(entry)}' has empty OverlayId — skipped");
					continue;
				}
				if (s_shownIdsThisSession.Contains(entry.OverlayId))
					continue;
				if (entry.Priority < bestPriority)
				{
					bestPriority = entry.Priority;
					candidate = entry;
				}
			}

			if (candidate == null)
			{
				if (EnableLogging)
					Debug.Log("[UiStartupOverlayQueue] All overlays completed");
				s_running = false;
				var cb = s_onAllDone;
				s_onAllDone = null;
				cb?.Invoke();
				return;
			}

			// Mark as processed BEFORE Show. An overlay that decides to skip (e.g. wrong season)
			// still counts as processed for the session — its decision wouldn't change on the
			// next dashboard revisit.
			s_shownIdsThisSession.Add(candidate.OverlayId);

			if (EnableLogging)
				Debug.Log($"[UiStartupOverlayQueue] Showing overlay '{Describe(candidate)}' id='{candidate.OverlayId}' priority={candidate.Priority}");

			bool advanced = false;
			candidate.Show(() =>
			{
				if (advanced)
				{
					Debug.LogWarning($"[UiStartupOverlayQueue] Overlay '{Describe(candidate)}' invoked its onClosed callback more than once");
					return;
				}
				advanced = true;
				Next();
			});
		}

		private static string Describe( IUiStartupOverlay _overlay )
		{
			if (_overlay is UnityEngine.Object obj && obj != null)
				return obj.name;
			return _overlay?.GetType().Name ?? "<null>";
		}
	}
}
