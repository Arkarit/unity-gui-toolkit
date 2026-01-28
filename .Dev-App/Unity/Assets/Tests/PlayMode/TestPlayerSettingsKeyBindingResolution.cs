using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Unit tests for runtime key binding resolution.
	/// </summary>
	public sealed class TestPlayerSettingsKeyBindingResolution
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
		public void ResolveKey_SingleBinding_ReturnsBoundKey()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			// A default = A
			PlayerSetting psA =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.A, options);

			mgr.Add(new List<PlayerSetting> { psA });

			// Explicitly bind A -> A
			psA.Value = new KeyBinding(KeyCode.A);

			KeyBinding original = psA.GetDefaultValue<KeyBinding>();
			KeyBinding resolved = mgr.ResolveKey(original);

			Assert.That(resolved.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(resolved.Modifiers, Is.EqualTo(KeyBinding.EModifiers.None));
		}

		[Test]
		public void ResolveKey_MultipleBindings_ReturnsCorrectBindings()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			// A default = A
			PlayerSetting psA =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.A, options);

			// B default = B
			PlayerSetting psB =
				new PlayerSetting("cat", "grp", "Action Shift+A", KeyCode.B, options);

			mgr.Add(new List<PlayerSetting> { psA, psB });

			// Bindings:
			// A -> A
			// B -> Shift + A
			psA.Value = new KeyBinding(KeyCode.A);
			psB.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);

			KeyBinding originalA = psA.GetDefaultValue<KeyBinding>();
			KeyBinding originalB = psB.GetDefaultValue<KeyBinding>();

			KeyBinding resolvedA = mgr.ResolveKey(originalA);
			KeyBinding resolvedB = mgr.ResolveKey(originalB);

			Assert.That(resolvedA.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(resolvedA.Modifiers, Is.EqualTo(KeyBinding.EModifiers.None));

			Assert.That(resolvedB.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(resolvedB.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));
		}

		[Test]
		public void ResolveKey_UnchangedBinding_ReturnsOriginal()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting ps =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.A, options);

			mgr.Add(new List<PlayerSetting> { ps });

			KeyBinding original = ps.GetDefaultValue<KeyBinding>();
			KeyBinding resolved = mgr.ResolveKey(original);

			Assert.That(resolved, Is.EqualTo(original));
		}

		[Test]
		public void ResolveKey_NoneBinding_ReturnsNone()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting ps =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.A, options);

			mgr.Add(new List<PlayerSetting> { ps });

			ps.Value = new KeyBinding(KeyCode.None);

			KeyBinding original = ps.GetDefaultValue<KeyBinding>();
			KeyBinding resolved = mgr.ResolveKey(original);

			Assert.That(resolved.KeyCode, Is.EqualTo(KeyCode.None));
		}

		[Test]
		public void ResolveKey_AfterConflictResolution_ReturnsUpdatedBinding()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting psA =
				new PlayerSetting("cat", "grp", "Action A", KeyCode.A, options);

			PlayerSetting psB =
				new PlayerSetting("cat", "grp", "Action B", KeyCode.B, options);

			mgr.Add(new List<PlayerSetting> { psA, psB });

			// Force duplicate: B takes A
			psB.Value = new KeyBinding(KeyCode.A);

			KeyBinding resolvedA = mgr.ResolveKey(psA.GetDefaultValue<KeyBinding>());
			KeyBinding resolvedB = mgr.ResolveKey(psB.GetDefaultValue<KeyBinding>());

			bool aIsNone = resolvedA.KeyCode == KeyCode.None;
			bool bIsNone = resolvedB.KeyCode == KeyCode.None;

			Assert.That(aIsNone ^ bIsNone, Is.True, "Exactly one binding must be None.");
		}

		[Test]
		public void ResolveKey_DifferentModifiers_AreResolvedIndependently()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting psShift =
				new PlayerSetting("cat", "grp", "Action Shift+A", KeyCode.A, options);

			PlayerSetting psCtrl =
				new PlayerSetting("cat", "grp", "Action Ctrl+A", KeyCode.B, options);

			mgr.Add(new List<PlayerSetting> { psShift, psCtrl });

			psShift.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);
			psCtrl.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Ctrl);

			KeyBinding rShift = mgr.ResolveKey(psShift.GetDefaultValue<KeyBinding>());
			KeyBinding rCtrl = mgr.ResolveKey(psCtrl.GetDefaultValue<KeyBinding>());

			Assert.That(rShift.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(rShift.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));

			Assert.That(rCtrl.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(rCtrl.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Ctrl));
		}

		[Test]
		public void ResolveKey_OneModifier_IsResolvedIndependently()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting psShift =
				new PlayerSetting("cat", "grp", "Action Shift+A", KeyCode.A, options);

			PlayerSetting psCtrl =
				new PlayerSetting("cat", "grp", "Action Ctrl+A", KeyCode.B, options);

			mgr.Add(new List<PlayerSetting> { psShift, psCtrl });

			psShift.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);
			psCtrl.Value = new KeyBinding(KeyCode.A);

			KeyBinding rShift = mgr.ResolveKey(psShift.GetDefaultValue<KeyBinding>());
			KeyBinding rCtrl = mgr.ResolveKey(psCtrl.GetDefaultValue<KeyBinding>());

			Assert.That(rShift.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(rShift.Modifiers, Is.EqualTo(KeyBinding.EModifiers.Shift));

			Assert.That(rCtrl.KeyCode, Is.EqualTo(KeyCode.A));
			Assert.That(rCtrl.Modifiers, Is.EqualTo(KeyBinding.EModifiers.None));
		}

		[Test]
		public void ResolveKey_UnknownBinding_ReturnsOriginal()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var unknown = new KeyBinding(KeyCode.Z, KeyBinding.EModifiers.Shift);

			KeyBinding resolved = mgr.ResolveKey(unknown);

			Assert.That(resolved, Is.EqualTo(unknown));
		}

		[Test]
		public void GetKey_SingleKey_Works()
		{
			PlayerSettings mgr = PlayerSettings.Instance;
			var input = new MockInputProxy();
			mgr.InputProxy = input;

			var options = new PlayerSettingOptions { IsSaveable = false };
			PlayerSetting ps =
				new PlayerSetting("cat", "grp", "A", KeyCode.A, options);

			mgr.Add(new List<PlayerSetting> { ps });

			input.Press(KeyCode.A);

			Assert.That(mgr.GetKey(ps.GetDefaultValue<KeyBinding>()), Is.True);
		}

		[Test]
		public void GetKey_ModifierBinding_RequiresModifier()
		{
			PlayerSettings mgr = PlayerSettings.Instance;
			var input = new MockInputProxy();
			mgr.InputProxy = input;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting ps =
				new PlayerSetting("cat", "grp", "Shift+A", KeyCode.A, options);

			mgr.Add(new List<PlayerSetting> { ps });

			ps.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);

			input.Press(KeyCode.A); // Shift NOT pressed

			Assert.That(
				mgr.GetKey(ps.GetDefaultValue<KeyBinding>()),
				Is.False,
				"Shift+A must not fire without Shift");
		}

		[Test]
		public void GetKey_ModifierBinding_WithModifier_Works()
		{
			PlayerSettings mgr = PlayerSettings.Instance;
			var input = new MockInputProxy();
			mgr.InputProxy = input;

			var options = new PlayerSettingOptions
			{
				IsSaveable = false,
				KeyPolicy = EKeyPolicy.KeyWithModifiers
			};

			PlayerSetting ps =
				new PlayerSetting("cat", "grp", "Shift+A", KeyCode.A, options);

			mgr.Add(new List<PlayerSetting> { ps });

			ps.Value = new KeyBinding(KeyCode.A, KeyBinding.EModifiers.Shift);

			input.Press(KeyCode.LeftShift);
			input.Press(KeyCode.A);

			Assert.That(mgr.GetKey(ps.GetDefaultValue<KeyBinding>()), Is.True);
		}


	}
}
