using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Tests interaction between SingleKey and KeyWithModifiers policies.
	/// </summary>
	public sealed class TestPlayerSettingsKeyBindings_ModifierPolicy
	{
		[SetUp]
		public void SetUp()
		{
			PlayerSettings.Instance.Clear();
		}

		[TearDown]
		public void TearDown()
		{
			PlayerSettings.Instance.Clear();
		}

		[Test]
		public void SingleKey_And_ModifierBindings_ResolveConflicts_Bidirectionally()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var optionsSingle = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.SingleKey
			};

			var optionsWithModifiers = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			// -----------------------------------------------------------------
			// Phase 1:
			// A = SingleKey (LEFTSHIFT)
			// B = KeyWithModifiers (Shift+A)
			// Expected: A cleared, B stays
			// -----------------------------------------------------------------

			PlayerSetting psA =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.LeftShift, optionsSingle);

			PlayerSetting psB =
				new PlayerSetting("cat", "grp", "Action B", KeyCode.A, optionsWithModifiers);

			mgr.Add(new List<PlayerSetting> { psA, psB });

			psB.Value = new KeyBinding(
				KeyCode.A,
				KeyBinding.EModifiers.Shift);

			KeyBinding b1 = psB.GetValue<KeyBinding>();
			Assert.That(b1.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(b1.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));

			KeyBinding a1 = psA.GetValue<KeyBinding>();
			Assert.That(a1.KeyCode, Is.EqualTo(KeyCode.None));

			// -----------------------------------------------------------------
			// Phase 2:
			// Reset, then reverse order
			// B = KeyWithModifiers (Shift+A)
			// A = SingleKey (LEFTSHIFT)
			// Expected: B cleared, A stays
			// -----------------------------------------------------------------

			mgr.Clear();

			psA =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.None, optionsSingle);

			psB =
				new PlayerSetting("cat", "grp", "Action B", KeyCode.A, optionsWithModifiers);

			mgr.Add(new List<PlayerSetting> { psA, psB });

			psB.Value = new KeyBinding(
				KeyCode.A,
				KeyBinding.EModifiers.Shift);

			psA.Value = new KeyBinding(KeyCode.LeftShift);

			KeyBinding a2 = psA.GetValue<KeyBinding>();
			Assert.That(a2.KeyCode, Is.EqualTo(KeyCode.LeftShift));

			KeyBinding b2 = psB.GetValue<KeyBinding>();
			Assert.That(b2.KeyCode, Is.EqualTo(KeyCode.None));
		}

		[Test]
		public void KeyWithModifiers_DifferentModifiers_DoNotConflict()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var optionsWithModifiers = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			// A = Shift + A
			PlayerSetting psA =
				new PlayerSetting("cat", "grp", "Action Shift+A", KeyCode.A, optionsWithModifiers);

			// B = Ctrl + A
			PlayerSetting psB =
				new PlayerSetting("cat", "grp", "Action Ctrl+A", KeyCode.B, optionsWithModifiers);

			mgr.Add(new List<PlayerSetting> { psA, psB });

			psA.Value = new KeyBinding(
				KeyCode.A,
				KeyBinding.EModifiers.Shift);

			psB.Value = new KeyBinding(
				KeyCode.A,
				KeyBinding.EModifiers.Ctrl);

			KeyBinding a = psA.GetValue<KeyBinding>();
			KeyBinding b = psB.GetValue<KeyBinding>();

			// Both bindings must survive
			Assert.That(a.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(a.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));

			Assert.That(b.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(b.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Ctrl));
		}


		[Test]
		public void PlainKey_And_ModifierKey_DoNotConflict_Bidirectionally()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			// -----------------------------------------------------------------
			// Phase 1:
			// A = A
			// B = Shift + A
			// -----------------------------------------------------------------

			PlayerSetting psPlain =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.A, options);

			PlayerSetting psShift =
				new PlayerSetting("cat", "grp", "Action Shift+A", KeyCode.B, options);

			mgr.Add(new List<PlayerSetting> { psPlain, psShift });

			psPlain.Value = new KeyBinding(KeyCode.A);
			psShift.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);

			KeyBinding plain1 = psPlain.GetValue<KeyBinding>();
			KeyBinding shift1 = psShift.GetValue<KeyBinding>();

			Assert.That(plain1.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(plain1.Modifiers, Is.EqualTo(KeyBinding.EModifiers.None));

			Assert.That(shift1.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(shift1.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));

			// -----------------------------------------------------------------
			// Phase 2:
			// Reset
			// A = Shift + A
			// B = A
			// -----------------------------------------------------------------

			mgr.Clear();

			psShift =
				new PlayerSetting("cat", "grp", "Action Shift+A", KeyCode.A, options);

			psPlain =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.B, options);

			mgr.Add(new List<PlayerSetting> { psShift, psPlain });

			psShift.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);
			psPlain.Value = new KeyBinding(KeyCode.A);

			KeyBinding shift2 = psShift.GetValue<KeyBinding>();
			KeyBinding plain2 = psPlain.GetValue<KeyBinding>();

			Assert.That(shift2.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(shift2.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));

			Assert.That(plain2.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(plain2.Modifiers, Is.EqualTo(KeyBinding.EModifiers.None));
		}

	}
}
