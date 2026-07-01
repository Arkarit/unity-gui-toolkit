using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	/// <summary>
	/// Dynamically-created host AND sequencer for the startup-overlay sequence (changelog, sale /
	/// event popups, …). Instantiated by <see cref="UiMain"/> from its optional prefab reference
	/// (<c>m_startupOverlayViewPrefab</c>), so it lives on its own layered canvas above the blocked
	/// screens and — being parented under the DontDestroyOnLoad <see cref="UiMain"/> — survives
	/// scene changes and keeps its identity (and its position in the sequence) across them.
	///
	/// Prefab layout — the click-catcher must come BEFORE the overlay panels so the panels render
	/// above it on this shared canvas:
	/// <code>
	/// UiStartupOverlayView   (Canvas + this component, Layer = Top)
	///  ├ ClickCatcher        (full-screen; its CanvasGroup wired into m_clickCatcher)
	///  ├ ChangeLog           (UiPanel)
	///  ├ SalePanel           (UiPanel)
	///  └ EventPanel …        (UiPanel)
	/// </code>
	///
	/// Sequencing model: <see cref="Run"/> walks the child <see cref="UiPanel"/>s in sibling order
	/// (non-UiPanel children and the click-catcher are ignored). Each panel is shown via
	/// <see cref="UiPanel.Show"/>; the queue advances when the panel raises
	/// <see cref="UiPanel.EvOnEndHide"/>. That single event is the contract for BOTH "the user
	/// dismissed me" and "I decline to show" — a panel that shouldn't appear simply raises
	/// EvOnEndHide without ever showing itself (no interface required; documented limitation).
	///
	/// The full-screen click-catcher blocks everything behind this canvas for the whole run,
	/// closing the input gaps before the first panel and between panels; the panels, above it, stay
	/// interactive. On a scene change (a panel navigating away) the run pauses and resumes from the
	/// next panel when <see cref="Run"/> is called again — the persistent object keeps the index.
	/// </summary>
	public class UiStartupOverlayView : UiView
	{
		[Tooltip("CanvasGroup of the full-screen click-catcher child (placed below the overlay " +
			"panels). Blocking is engaged for the whole run and normalized off on awake — the " +
			"serialized state is irrelevant.")]
		[SerializeField, Optional] private CanvasGroup m_clickCatcher;

		[Tooltip("Failsafe (unscaled seconds): longest wait for a panel's EvOnEndHide before " +
			"logging an error and advancing. Guards a panel that never reports done. Must exceed " +
			"the longest plausible user dwell (reading a changelog). <= 0 waits indefinitely.")]
		[SerializeField] private float m_overlayTimeoutSeconds = 120f;

		[Tooltip("Verbose UiLog output for each show / advance / completion / state change.")]
		[SerializeField] private bool m_enableLogging;

		private readonly List<UiPanel> m_panels = new();
		private int m_index;
		private bool m_running;
		private Coroutine m_coroutine;
		private Action m_onAllDone;

		/// <summary>True while a sequence is running (between <see cref="Run"/> and completion/stop).</summary>
		public bool IsRunning => m_running;

		protected override void Awake()
		{
			base.Awake();
			CachePanels();
			SetLive(false); // start dormant; Run() switches the view live
		}

		protected override void Start()
		{
			base.Start();

			// Normalize every overlay panel to hidden regardless of how it was authored, so nothing
			// lingers visible before its turn. Runs after all Awakes, so panel state is initialized.
			foreach (var panel in m_panels)
			{
				if (panel != null)
					panel.Hide(true);
			}
		}

		protected override void OnDestroy()
		{
			UnsubscribeSceneChange();
			base.OnDestroy();
		}

		/// <summary>
		/// Run (or resume) the startup-overlay sequence, then invoke <paramref name="_onAllDone"/>.
		/// Re-entrant calls are ignored. If the sequence is already finished, the callback fires
		/// immediately.
		/// </summary>
		public void Run( Action _onAllDone = null )
		{
			Log($"Run() — index {m_index}/{m_panels.Count}, running={m_running}");

			if (m_running)
			{
				UiLog.LogWarning("[UiStartupOverlayView] Run() called while already running — ignored");
				return;
			}

			if (m_index >= m_panels.Count)
			{
				Log("Run() — nothing pending, invoking onAllDone immediately");
				_onAllDone?.Invoke();
				return;
			}

			m_running = true;
			m_onAllDone = _onAllDone;

			// Go live synchronously (covers the gap before the first panel) and watch for scene
			// changes so a panel that navigates away pauses the run instead of advancing blindly.
			SetLive(true);
			SubscribeSceneChange();

			m_coroutine = StartCoroutine(RunCoroutine());
		}

		/// <summary>
		/// Pause the run: the current panel is treated as done (the sequence resumes at the next one
		/// on the following <see cref="Run"/>), the blocker is released and the coroutine stopped.
		/// Call this before deliberately navigating to another scene from within a panel; the scene
		/// change is also caught automatically as a safety net.
		/// </summary>
		public void Stop()
		{
			Log($"Stop() — index {m_index}/{m_panels.Count}, running={m_running}");

			UnsubscribeSceneChange();

			if (!m_running)
				return;

			// The coroutine only ever yields while a panel is showing, so the current index points
			// at that panel — mark it done so the resume continues past it.
			if (m_index < m_panels.Count)
				m_index++;

			if (m_coroutine != null)
			{
				StopCoroutine(m_coroutine);
				m_coroutine = null;
			}

			// StopCoroutine does not unwind RunCoroutine's finally, so go dormant here.
			SetLive(false);
			m_running = false;
			m_onAllDone = null;
		}

		private IEnumerator RunCoroutine()
		{
			// finally covers normal completion AND exceptions; the Stop() abort path releases the
			// blocker itself (StopCoroutine does not unwind this finally).
			try
			{
				while (m_index < m_panels.Count)
				{
					var panel = m_panels[m_index];
					if (panel != null)
						yield return ShowPanel(panel);

					m_index++;
				}
			}
			finally
			{
				SetLive(false);
				UnsubscribeSceneChange();
				m_running = false;
				m_coroutine = null;
			}

			Log("All overlays completed");

			var cb = m_onAllDone;
			m_onAllDone = null;
			cb?.Invoke();
		}

		private IEnumerator ShowPanel( UiPanel _panel )
		{
			Log($"Showing '{_panel.name}' ({m_index + 1}/{m_panels.Count})");

			bool done = false;
			void OnHidden( UiPanel _ ) => done = true;

			// EvOnEndHide is the sole "this panel is done" signal — raised on user-dismiss and,
			// by convention, immediately by a panel that declines to show.
			_panel.EvOnEndHide.AddListener(OnHidden);

			// A panel that throws from Show() must not take the whole sequence down — log and
			// advance to the next one instead. (yield break is kept out of the catch to stay within
			// the C# rules for yield in try/catch.)
			bool threw = false;
			try
			{
				_panel.Show();
			}
			catch (Exception e)
			{
				threw = true;
				_panel.EvOnEndHide.RemoveListener(OnHidden);
				UiLog.LogError($"[UiStartupOverlayView] '{_panel.name}' threw from Show() — skipping: {e}");
			}

			if (threw)
				yield break;

			float elapsed = 0f;
			yield return new WaitUntil(() =>
			{
				if (done)
					return true;
				if (m_overlayTimeoutSeconds <= 0f)
					return false;
				elapsed += Time.unscaledDeltaTime;
				return elapsed >= m_overlayTimeoutSeconds;
			});

			_panel.EvOnEndHide.RemoveListener(OnHidden);

			if (done)
				Log($"'{_panel.name}' done");
			else
				UiLog.LogError($"[UiStartupOverlayView] '{_panel.name}' did not raise EvOnEndHide within {m_overlayTimeoutSeconds}s — forcing advance");
		}

		private void CachePanels()
		{
			m_panels.Clear();
			var catcher = m_clickCatcher != null ? m_clickCatcher.gameObject : null;

			var t = transform;
			int n = t.childCount;
			for (int i = 0; i < n; i++)
			{
				var child = t.GetChild(i);
				if (catcher != null && child.gameObject == catcher)
					continue;

				var panel = child.GetComponent<UiPanel>();
				if (panel != null)
					m_panels.Add(panel);
			}

			Log($"CachePanels() — found {m_panels.Count} overlay panel(s) among {n} child(ren)" +
				(m_clickCatcher == null ? " (no click-catcher assigned!)" : ""));
		}

		// Dormant when idle, live only for the duration of a run. Disabling the Canvas makes the
		// whole view render nothing and receive no input, so its full-screen click-catcher can't
		// overlay or block other scenes while the persistent view just sits around waiting.
		private void SetLive( bool _live )
		{
			Log($"SetLive({_live}) — canvas + click-catcher blocking {(_live ? "ON" : "OFF")}");

			if (Canvas != null)
				Canvas.enabled = _live;
			if (m_clickCatcher != null)
				m_clickCatcher.blocksRaycasts = _live;
		}

		private void OnActiveSceneChanged( Scene _from, Scene _to )
		{
			Log($"activeSceneChanged '{_from.name}' -> '{_to.name}' — pausing run");
			Stop();
		}

		private void SubscribeSceneChange()
		{
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
		}

		private void UnsubscribeSceneChange()
		{
			SceneManager.activeSceneChanged -= OnActiveSceneChanged;
		}

		private void Log( string _message )
		{
			if (m_enableLogging)
				UiLog.Log($"[UiStartupOverlayView] {_message}");
		}
	}
}
