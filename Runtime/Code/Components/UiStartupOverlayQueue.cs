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
	/// overlay completes, looking for the next lowest-priority entry that hasn't already
	/// been shown this run. This is important because overlay GameObjects driven by chained
	/// <see cref="UiInitialActiveState"/> components or async asset loading can come online
	/// well after the first <c>Run</c> tick — by re-scanning, a slot that priority-wise
	/// would have run earlier still gets its turn if it shows up in time.
	///
	/// Once the scan finds nothing left, the run is complete; overlays that register after
	/// that point would need a fresh <see cref="Run"/> call to be processed.
	/// </summary>
	public static class UiStartupOverlayQueue
	{
		private static readonly List<IUiStartupOverlay> s_registered = new();
		private static readonly HashSet<IUiStartupOverlay> s_shownThisRun = new();
		private static bool s_running;
		private static Action s_onAllDone;

		/// <summary>
		/// Enable verbose Debug.Log output for each registration / advance / completion event.
		/// Off by default — flip on from your bootstrap code while diagnosing ordering issues.
		/// </summary>
		public static bool EnableLogging = false;

		/// <summary>
		/// Add an overlay to the registry. Safe to call multiple times — duplicate registrations
		/// are ignored. If a run is currently in progress and this overlay has a higher priority
		/// than the next-to-be-shown entry, it will be picked up on the next rescan.
		/// </summary>
		public static void Register( IUiStartupOverlay _overlay )
		{
			if (_overlay == null)
				return;
			if (s_registered.Contains(_overlay))
				return;
			s_registered.Add(_overlay);

			if (EnableLogging)
				Debug.Log($"[UiStartupOverlayQueue] Registered overlay '{Describe(_overlay)}' with priority {_overlay.Priority}");
		}

		public static void Unregister( IUiStartupOverlay _overlay )
		{
			s_registered.Remove(_overlay);
		}

		/// <summary>
		/// Test/diagnostic hook. Clears every registration. Production code should rely on
		/// per-overlay Unregister in OnDestroy.
		/// </summary>
		public static void Clear()
		{
			s_registered.Clear();
			s_shownThisRun.Clear();
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

			s_shownThisRun.Clear();
			s_running = true;
			s_onAllDone = _onAllDone;
			Next();
		}

		private static void Next()
		{
			// Live rescan of the registered list. Pick the lowest-priority entry that hasn't
			// been shown this run, skipping destroyed Unity objects.
			IUiStartupOverlay candidate = null;
			int bestPriority = int.MaxValue;
			foreach (var entry in s_registered)
			{
				if (entry == null)
					continue;
				if (entry is UnityEngine.Object obj && obj == null)
					continue;
				if (s_shownThisRun.Contains(entry))
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

			s_shownThisRun.Add(candidate);

			if (EnableLogging)
				Debug.Log($"[UiStartupOverlayQueue] Showing overlay '{Describe(candidate)}' with priority {candidate.Priority}");

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
