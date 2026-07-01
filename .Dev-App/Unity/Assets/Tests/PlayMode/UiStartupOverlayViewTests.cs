using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Runtime tests for <see cref="UiStartupOverlayView"/> — including adversarial cases
	/// ("reintreten"): a panel that throws from Show(), a panel that never closes (timeout), and
	/// a scene-style interruption via Stop(). The view is exercised standalone (no UiMain); the
	/// full-screen click-catcher and the per-panel timeout are injected via reflection.
	/// </summary>
	public class UiStartupOverlayViewTests
	{
		// ---- Test panel -------------------------------------------------------------------------

		private class TestPanel : UiPanel
		{
			public enum Mode { Normal, Skip, ThrowOnShow, NeverClose }

			public Mode PanelMode = Mode.Normal;
			public int ShowOrder = -1;   // order in which Show() was invoked (-1 = never)
			public int ShowCalls;        // how many times Show() was invoked
			public bool WasActuallyShown; // true only when it really presented (base.Show ran)

			private static int s_showCounter;

			public static void ResetCounter() => s_showCounter = 0;

			public override void Show( bool _instant = false, Action _onFinish = null )
			{
				ShowOrder = s_showCounter++;
				ShowCalls++;

				switch (PanelMode)
				{
					case Mode.Skip:
						EvOnEndHide.Invoke(this); // decline without ever presenting
						return;

					case Mode.ThrowOnShow:
						throw new Exception("test boom");

					case Mode.NeverClose:
						WasActuallyShown = true;
						base.Show(true, _onFinish); // present, but the test never closes it
						return;

					default:
						WasActuallyShown = true;
						base.Show(true, _onFinish); // present; the driver closes it
						return;
				}
			}
		}

		// ---- Fixture ----------------------------------------------------------------------------

		private readonly List<GameObject> m_spawned = new();

		[SetUp]
		public void SetUp() => TestPanel.ResetCounter();

		[TearDown]
		public void TearDown()
		{
			foreach (var go in m_spawned)
			{
				if (go != null)
					UnityEngine.Object.DestroyImmediate(go);
			}
			m_spawned.Clear();
		}

		private (UiStartupOverlayView view, CanvasGroup catcher, TestPanel[] panels) BuildView(
			float _timeoutSeconds, params TestPanel.Mode[] _modes )
		{
			var root = new GameObject("UiStartupOverlayViewTest");
			root.SetActive(false); // build the whole hierarchy before Awake runs
			m_spawned.Add(root);

			var view = root.AddComponent<UiStartupOverlayView>(); // RequireComponent adds Canvas etc.

			var catcherGo = new GameObject("ClickCatcher");
			catcherGo.transform.SetParent(root.transform, false);
			var catcher = catcherGo.AddComponent<CanvasGroup>();

			var panels = new TestPanel[_modes.Length];
			for (int i = 0; i < _modes.Length; i++)
			{
				var panelGo = new GameObject($"Panel{i}");
				panelGo.transform.SetParent(root.transform, false);
				var panel = panelGo.AddComponent<TestPanel>();
				panel.PanelMode = _modes[i];
				panels[i] = panel;
			}

			SetPrivateField(view, "m_clickCatcher", catcher);
			SetPrivateField(view, "m_overlayTimeoutSeconds", _timeoutSeconds);

			root.SetActive(true); // Awake -> CachePanels (catcher excluded, panels cached in order)
			return (view, catcher, panels);
		}

		private static void SetPrivateField( object _target, string _name, object _value )
		{
			var field = _target.GetType().GetField(_name, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.IsNotNull(field, $"Private field '{_name}' not found — test needs updating");
			field.SetValue(_target, _value);
		}

		// Drive the sequence to completion: each frame, close any presented Normal panel (simulating
		// the user), while leaving NeverClose panels alone so they hit the timeout.
		private static IEnumerator DriveToCompletion( UiStartupOverlayView _view, TestPanel[] _panels, int _maxFrames = 600 )
		{
			int guard = 0;
			yield return null; // let Start() normalize panels hidden, and the first Show() run

			while (_view.IsRunning && guard++ < _maxFrames)
			{
				foreach (var p in _panels)
				{
					if (p.PanelMode == TestPanel.Mode.Normal && p.IsVisible)
						p.Hide(true);
				}
				yield return null;
			}

			Assert.IsFalse(_view.IsRunning, "Sequence did not complete within the frame budget");
		}

		// ---- Tests ------------------------------------------------------------------------------

		[UnityTest]
		public IEnumerator RunsPanelsInSiblingOrder_AndBlocksThenReleases()
		{
			var (view, catcher, panels) = BuildView(60f,
				TestPanel.Mode.Normal, TestPanel.Mode.Normal, TestPanel.Mode.Normal);

			yield return null; // Start()

			Assert.IsFalse(catcher.blocksRaycasts, "Catcher should not block before Run()");

			bool allDone = false;
			view.Run(() => allDone = true);

			Assert.IsTrue(view.IsRunning, "Run() should mark the view running");
			Assert.IsTrue(catcher.blocksRaycasts, "Catcher must block for the whole run");

			yield return DriveToCompletion(view, panels);

			Assert.IsTrue(allDone, "onAllDone must fire on completion");
			Assert.IsFalse(catcher.blocksRaycasts, "Catcher must be released after the run");

			Assert.AreEqual(0, panels[0].ShowOrder);
			Assert.AreEqual(1, panels[1].ShowOrder);
			Assert.AreEqual(2, panels[2].ShowOrder);
			foreach (var p in panels)
				Assert.IsTrue(p.WasActuallyShown, $"{p.name} should have been shown");
		}

		[UnityTest]
		public IEnumerator DecliningPanel_IsNotPresented_ButSequenceContinues()
		{
			var (view, catcher, panels) = BuildView(60f,
				TestPanel.Mode.Normal, TestPanel.Mode.Skip, TestPanel.Mode.Normal);

			yield return null; // let Start() normalize panels hidden before Run()

			bool allDone = false;
			view.Run(() => allDone = true);
			yield return DriveToCompletion(view, panels);

			Assert.IsTrue(allDone);
			Assert.IsFalse(catcher.blocksRaycasts);
			Assert.IsTrue(panels[0].WasActuallyShown);
			Assert.IsFalse(panels[1].WasActuallyShown, "Skipped panel must never present");
			Assert.IsTrue(panels[2].WasActuallyShown, "Sequence must continue past a skip");
		}

		[UnityTest]
		public IEnumerator ThrowingPanel_IsSkipped_SequenceSurvives()
		{
			LogAssert.Expect(LogType.Error, new Regex("threw from Show"));

			var (view, catcher, panels) = BuildView(60f,
				TestPanel.Mode.Normal, TestPanel.Mode.ThrowOnShow, TestPanel.Mode.Normal);

			yield return null; // let Start() normalize panels hidden before Run()

			bool allDone = false;
			view.Run(() => allDone = true);
			yield return DriveToCompletion(view, panels);

			Assert.IsTrue(allDone, "A throwing panel must not kill the sequence");
			Assert.IsFalse(catcher.blocksRaycasts, "Blocker must still be released");
			Assert.IsTrue(panels[0].WasActuallyShown);
			Assert.IsFalse(panels[1].WasActuallyShown);
			Assert.IsTrue(panels[2].WasActuallyShown, "Sequence must continue past a throwing panel");
		}

		[UnityTest]
		public IEnumerator PanelThatNeverCloses_TimesOut_AndAdvances()
		{
			LogAssert.Expect(LogType.Error, new Regex("did not raise EvOnEndHide"));

			var (view, catcher, panels) = BuildView(0.2f,
				TestPanel.Mode.NeverClose, TestPanel.Mode.Normal);

			yield return null; // let Start() normalize panels hidden before Run()

			bool allDone = false;
			view.Run(() => allDone = true);
			yield return DriveToCompletion(view, panels); // never closes panel0 -> timeout kicks in

			Assert.IsTrue(allDone, "Timeout must let the sequence advance and finish");
			Assert.IsFalse(catcher.blocksRaycasts);
			Assert.IsTrue(panels[0].WasActuallyShown);
			Assert.IsTrue(panels[1].WasActuallyShown, "Must advance to the next panel after the timeout");
		}

		[UnityTest]
		public IEnumerator Stop_PausesAndReleases_ThenRunResumesAtNextPanel()
		{
			var (view, catcher, panels) = BuildView(60f,
				TestPanel.Mode.Normal, TestPanel.Mode.Normal);

			yield return null; // let Start() normalize panels hidden before Run()

			view.Run();
			yield return null; // panel0 presented

			Assert.IsTrue(panels[0].IsVisible, "Panel0 should be presented before Stop()");

			view.Stop();
			Assert.IsFalse(view.IsRunning, "Stop() must halt the run");
			Assert.IsFalse(catcher.blocksRaycasts, "Stop() must release the blocker");

			// Resume: the current panel (panel0) was consumed, so the run continues at panel1.
			bool allDone = false;
			view.Run(() => allDone = true);
			Assert.IsTrue(catcher.blocksRaycasts, "Resume must re-engage the blocker");

			yield return DriveToCompletion(view, panels);

			Assert.IsTrue(allDone);
			Assert.AreEqual(1, panels[0].ShowCalls, "Panel0 must not be shown again after resume");
			Assert.IsTrue(panels[1].WasActuallyShown, "Resume must present the next panel");
		}
	}
}
