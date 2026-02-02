using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// System tests for PlayerSettings click/drag callback behavior driven by IInputProxy.
	/// Verifies that OnClick and drag callbacks are triggered (or not triggered) as expected.
	/// </summary>
	public sealed class TestPlayerSettingsClickAndDragCallbacks
	{
		private MockInputProxy m_input;

		[SetUp]
		public void SetUp()
		{
			PlayerSettings.Instance.Clear();
			PlayerSettings.Instance.ManualUpdate = true;

			m_input = new MockInputProxy();
			PlayerSettings.Instance.InputProxy = m_input;

			// Make the tests explicit and stable.
			PlayerSettings.Instance.DragTreshold = 5.0f;
		}

		[TearDown]
		public void TearDown()
		{
			PlayerSettings.Instance.Clear();
			PlayerSettings.Instance.InputProxy = new UnityInputProxy();
			PlayerSettings.Instance.ManualUpdate = false;
		}

		[Test]
		public void OnClick_FiresOnRelease_WhenSupportDragEnabled_ButBelowThreshold()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				SupportDrag = true,
			};

			var ps = new PlayerSetting(
				"cat", "grp", "Action",
				KeyCode.A,
				options);

			mgr.Add(new List<PlayerSetting> { ps });

			int downCount = 0;
			int upCount = 0;
			int whileCount = 0;

			int clickCount = 0;
			int beginDragCount = 0;
			int whileDragCount = 0;
			int endDragCount = 0;

			mgr.AddKeyDownListener(KeyCode.A, (_) => downCount++);
			mgr.AddKeyUpListener(KeyCode.A, (_) => upCount++);
			mgr.AddKeyPressedListener(KeyCode.A, (_) => whileCount++);

			ps.OnClick.AddListener((_) => clickCount++);
			ps.OnBeginDrag.AddListener((_, _, _, _) => beginDragCount++);
			ps.WhileDrag.AddListener((_, _, _, _) => whileDragCount++);
			ps.OnEndDrag.AddListener((_, _, _, _) => endDragCount++);

			// Frame 1: press A at mouse pos (0,0,0)
			m_input.MousePosition = Vector3.zero;
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: move slightly (below threshold), still holding
			m_input.MousePosition = new Vector3(1, 0, 0);
			mgr.Update(2);

			// Frame 3: release A -> should click (no drag)
			m_input.Release(KeyCode.A);
			mgr.Update(3);

			Assert.That(downCount, Is.EqualTo(1));
			Assert.That(upCount, Is.EqualTo(1));
			Assert.That(whileCount, Is.EqualTo(1)); // Only frame 2 while active

			Assert.That(clickCount, Is.EqualTo(1));
			Assert.That(beginDragCount, Is.EqualTo(0));
			Assert.That(whileDragCount, Is.EqualTo(0));
			Assert.That(endDragCount, Is.EqualTo(0));
		}

		[Test]
		public void Drag_FiresBeginWhileEnd_WhenMovedBeyondThreshold_NoClick()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				SupportDrag = true,
			};

			var ps = new PlayerSetting(
				"cat", "grp", "DragAction",
				KeyCode.A,
				options);

			mgr.Add(new List<PlayerSetting> { ps });

			int downCount = 0;
			int upCount = 0;
			int whileCount = 0;

			int clickCount = 0;
			int beginDragCount = 0;
			int whileDragCount = 0;
			int endDragCount = 0;

			mgr.AddKeyDownListener(KeyCode.A, (_) => downCount++);
			mgr.AddKeyUpListener(KeyCode.A, (_) => upCount++);
			mgr.AddKeyPressedListener(KeyCode.A, (_) => whileCount++);

			Vector3 beginStart = default;
			Vector3 beginCurr = default;
			Vector3 endStart = default;
			Vector3 endCurr = default;

			ps.OnClick.AddListener((_) => clickCount++);
			ps.OnBeginDrag.AddListener((_, _start, _, _curr) =>
			{
				beginDragCount++;
				beginStart = _start;
				beginCurr = _curr;
			});
			ps.WhileDrag.AddListener((_, _, _, _) => whileDragCount++);
			ps.OnEndDrag.AddListener((_, _start, _, _curr) =>
			{
				endDragCount++;
				endStart = _start;
				endCurr = _curr;
			});

			// Frame 1: press A at (0,0,0) -> key down, start measuring drag distance
			m_input.MousePosition = Vector3.zero;
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: move beyond threshold -> begin drag should fire, and while drag should also fire in same frame
			m_input.MousePosition = new Vector3(10, 0, 0);
			mgr.Update(2);

			// Frame 3: keep holding -> while drag should fire again
			m_input.MousePosition = new Vector3(11, 0, 0);
			mgr.Update(3);

			// Frame 4: release -> end drag + key up, no click
			m_input.Release(KeyCode.A);
			mgr.Update(4);

			Assert.That(downCount, Is.EqualTo(1));
			Assert.That(upCount, Is.EqualTo(1));
			Assert.That(whileCount, Is.EqualTo(2)); // Frames 2 and 3 while active

			Assert.That(clickCount, Is.EqualTo(0));
			Assert.That(beginDragCount, Is.EqualTo(1));
			Assert.That(whileDragCount, Is.EqualTo(2)); // Frames 2 and 3 while dragging
			Assert.That(endDragCount, Is.EqualTo(1));

			Assert.That(beginStart, Is.EqualTo(Vector3.zero));
			Assert.That(beginCurr, Is.EqualTo(new Vector3(10, 0, 0)));

			Assert.That(endStart, Is.EqualTo(Vector3.zero));
			Assert.That(endCurr, Is.EqualTo(new Vector3(11, 0, 0)));
		}

		[Test]
		public void SupportDragFalse_NeverFiresDrag_EvenBeyondThreshold_ClickOnRelease()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				SupportDrag = false,
			};

			var ps = new PlayerSetting(
				"cat", "grp", "NoDragAction",
				KeyCode.A,
				options);

			mgr.Add(new List<PlayerSetting> { ps });

			int clickCount = 0;
			int beginDragCount = 0;
			int whileDragCount = 0;
			int endDragCount = 0;

			ps.OnClick.AddListener((_) => clickCount++);
			ps.OnBeginDrag.AddListener((_, _, _, _) => beginDragCount++);
			ps.WhileDrag.AddListener((_, _, _, _) => whileDragCount++);
			ps.OnEndDrag.AddListener((_, _, _, _) => endDragCount++);

			// Frame 1: press at (0,0,0)
			m_input.MousePosition = Vector3.zero;
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: move far beyond threshold while holding
			m_input.MousePosition = new Vector3(100, 0, 0);
			mgr.Update(2);

			// Frame 3: release -> should click, no drag
			m_input.Release(KeyCode.A);
			mgr.Update(3);

			Assert.That(clickCount, Is.EqualTo(1));

			Assert.That(beginDragCount, Is.EqualTo(0));
			Assert.That(whileDragCount, Is.EqualTo(0));
			Assert.That(endDragCount, Is.EqualTo(0));
		}

		[Test]
		public void DragDoesNotStartOnPressFrame_BeginsOnlyAfterMovementFrame()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				SupportDrag = true,
			};

			var ps = new PlayerSetting(
				"cat", "grp", "DragTiming",
				KeyCode.A,
				options);

			mgr.Add(new List<PlayerSetting> { ps });

			int beginDragCount = 0;
			int whileDragCount = 0;

			ps.OnBeginDrag.AddListener((_, _, _, _) => beginDragCount++);
			ps.WhileDrag.AddListener((_, _, _, _) => whileDragCount++);

			// Frame 1: press at (0,0,0) and update -> must NOT drag yet
			m_input.MousePosition = Vector3.zero;
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			Assert.That(beginDragCount, Is.EqualTo(0));
			Assert.That(whileDragCount, Is.EqualTo(0));

			// Frame 2: move beyond threshold -> drag begins now
			m_input.MousePosition = new Vector3(10, 0, 0);
			mgr.Update(2);

			Assert.That(beginDragCount, Is.EqualTo(1));
			Assert.That(whileDragCount, Is.EqualTo(1));

			// Frame 3: keep moving -> while drag again, but begin still only once
			m_input.MousePosition = new Vector3(11, 0, 0);
			mgr.Update(3);

			Assert.That(beginDragCount, Is.EqualTo(1));
			Assert.That(whileDragCount, Is.EqualTo(2));
		}
	}
}
