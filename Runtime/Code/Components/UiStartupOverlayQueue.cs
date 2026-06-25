using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Sequential runner for <see cref="IUiStartupOverlay"/>s. Overlays self-register on Awake
	/// and the orchestrator (typically a dashboard screen) calls <see cref="Run"/> once when the
	/// user is ready to see them. Entries fire in ascending <see cref="IUiStartupOverlay.Priority"/>
	/// order; each one signals completion via the callback passed to its <c>Show</c>, and only
	/// then does the queue advance to the next.
	///
	/// The order survives any overlay being inactive: an overlay that has nothing to show just
	/// invokes its <c>onClosed</c> callback immediately, and the queue moves on.
	///
	/// The registry is static and survives scene reloads — registrations from previously loaded
	/// scenes are pruned on <see cref="Run"/> (null/destroyed references are filtered out).
	/// </summary>
	public static class UiStartupOverlayQueue
	{
		private static readonly List<IUiStartupOverlay> s_registered = new();
		private static bool s_running;

		/// <summary>
		/// Add an overlay to the registry. Safe to call multiple times — duplicate registrations
		/// are ignored.
		/// </summary>
		public static void Register( IUiStartupOverlay _overlay )
		{
			if (_overlay == null)
				return;
			if (s_registered.Contains(_overlay))
				return;
			s_registered.Add(_overlay);
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
			s_running = false;
		}

		/// <summary>
		/// Walk all registered overlays in priority order. Re-entrant calls are ignored
		/// (a warning is logged and <paramref name="_onAllDone"/> fires immediately).
		/// </summary>
		public static void Run( Action _onAllDone = null )
		{
			if (s_running)
			{
				Debug.LogWarning("[UiStartupOverlayQueue] Run() called while already running — ignored");
				_onAllDone?.Invoke();
				return;
			}

			// Snapshot in priority order. Re-registration during a run won't disturb iteration,
			// and a destroyed MonoBehaviour (Unity-null) is filtered out before sorting.
			var snapshot = new List<IUiStartupOverlay>(s_registered.Count);
			foreach (var entry in s_registered)
			{
				if (entry is UnityEngine.Object obj && obj == null)
					continue;
				snapshot.Add(entry);
			}
			snapshot.Sort(( a, b ) => a.Priority.CompareTo(b.Priority));

			s_running = true;
			Next(snapshot, 0, _onAllDone);
		}

		private static void Next( List<IUiStartupOverlay> _queue, int _index, Action _done )
		{
			if (_index >= _queue.Count)
			{
				s_running = false;
				_done?.Invoke();
				return;
			}

			var overlay = _queue[_index];

			// Guard: if the overlay was destroyed mid-run, skip it.
			if (overlay is UnityEngine.Object obj && obj == null)
			{
				Next(_queue, _index + 1, _done);
				return;
			}

			bool advanced = false;
			overlay.Show(() =>
			{
				if (advanced)
				{
					Debug.LogWarning($"[UiStartupOverlayQueue] Overlay '{overlay}' invoked its onClosed callback more than once");
					return;
				}
				advanced = true;
				Next(_queue, _index + 1, _done);
			});
		}
	}
}
