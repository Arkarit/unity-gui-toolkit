using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Unit tests for PlayerSettings key binding callbacks (OnDown / WhilePressed / OnUp).
	/// </summary>
	public sealed class TestPlayerSettingsKeyBindingCallbacks
	{
		private MockInputProxy m_input;

		[SetUp]
		public void SetUp()
		{
			PlayerSettings.Instance.Clear();
			PlayerSettings.Instance.ManualUpdate = true;
			m_input = new MockInputProxy();
			PlayerSettings.Instance.InputProxy = m_input;
		}

		[TearDown]
		public void TearDown()
		{
			PlayerSettings.Instance.Clear();
			PlayerSettings.Instance.InputProxy = new UnityInputProxy();
			PlayerSettings.Instance.ManualUpdate = false;
		}

		// ------------------------------------------------------------
		// 1) OnKeyDown fires exactly once
		// ------------------------------------------------------------

		[Test]
		public void OnKeyDown_FiresOnce_WhenKeyIsPressed()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var ps = new PlayerSetting(
				"cat", "grp", "Action",
				KeyCode.A,
				new PlayerSettingOptions { IsSaveable = false });

			mgr.Add(new List<PlayerSetting> { ps });

			int downCount = 0;
			mgr.AddKeyDownListener(KeyCode.A, (_) => downCount++);

			// Frame 1: press A
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: keep holding A
			mgr.Update(2);

			Assert.That(downCount, Is.EqualTo(1));
		}

		// ------------------------------------------------------------
		// 2) WhilePressed fires every frame while active
		// ------------------------------------------------------------

		[Test]
		public void WhilePressed_FiresEveryFrame_WhileKeyIsHeld()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var ps = new PlayerSetting(
				"cat", "grp", "Action",
				KeyCode.A,
				new PlayerSettingOptions { IsSaveable = false });

			mgr.Add(new List<PlayerSetting> { ps });

			int pressedCount = 0;
			mgr.AddKeyPressedListener(KeyCode.A, (_) => pressedCount++);

			// Frame 1: press. This doesn't invoke the WhilePressed callback, which is only called in frame 2 and 3
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: hold
			mgr.Update(2);

			// Frame 3: hold
			mgr.Update(3);

			Assert.That(pressedCount, Is.EqualTo(2));
		}

		// ------------------------------------------------------------
		// 3) OnKeyUp fires exactly once
		// ------------------------------------------------------------

		[Test]
		public void OnKeyUp_FiresOnce_WhenKeyIsReleased()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var ps = new PlayerSetting(
				"cat", "grp", "Action",
				KeyCode.A,
				new PlayerSettingOptions { IsSaveable = false });

			mgr.Add(new List<PlayerSetting> { ps });

			int upCount = 0;
			mgr.AddKeyUpListener(KeyCode.A, (_) => upCount++);

			// Frame 1: press
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: nothing
			mgr.Update(2);
			
			// Frame 3: release
			m_input.Release(KeyCode.A);
			mgr.Update(3);

			// Frame 4: nothing
			mgr.Update(4);

			Assert.That(upCount, Is.EqualTo(1));
		}

		// ------------------------------------------------------------
		// 4) Modifier-Up triggers OnKeyUp
		// ------------------------------------------------------------

		[Test]
		public void ModifierRelease_TriggersKeyUp_ForModifierBinding()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			var ps = new PlayerSetting(
				"cat", "grp", "Shift+A",
				new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift),
				options);

			mgr.Add(new List<PlayerSetting> { ps });

			int upCount = 0;
			mgr.AddKeyUpListener(new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift), (_) => upCount++);

			// Frame 1: press Shift + A
			m_input.Press(KeyCode.LeftShift);
			m_input.Press(KeyCode.A);
			mgr.Update(1);

			// Frame 2: release Shift only
			m_input.Release(KeyCode.LeftShift);
			mgr.Update(2);

			Assert.That(upCount, Is.EqualTo(1));
		}

		// ------------------------------------------------------------
		// 5) No phantom Up without prior Down
		// ------------------------------------------------------------

		[Test]
		public void OnKeyUp_DoesNotFire_IfKeyWasNeverActive()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var ps = new PlayerSetting(
				"cat", "grp", "Action",
				KeyCode.A,
				new PlayerSettingOptions { IsSaveable = false });

			mgr.Add(new List<PlayerSetting> { ps });

			int upCount = 0;
			mgr.AddKeyUpListener(KeyCode.A, (_) => upCount++);

			// Frame 1: release A without ever pressing
			m_input.Release(KeyCode.A);
			mgr.Update(1);

			Assert.That(upCount, Is.EqualTo(0));
		}
	}
}
