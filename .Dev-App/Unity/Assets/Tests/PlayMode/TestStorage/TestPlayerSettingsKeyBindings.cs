using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Unit tests for player settings key binding persistence.
	/// </summary>
	/// <remarks>
	/// This file is part of the storage unit test suite.
	/// </remarks>
	public sealed class TestPlayerSettingsKeyBindings
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
		public void DuplicateBoundKey_IsResolved_AndKeyBindingMapIsConsistent()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options1 = new PlayerSettingOptions();
			options1.IsSaveable = false;

			var options2 = new PlayerSettingOptions();
			options2.IsSaveable = false;

			// Default bindings must be unique (your Add() enforces this).
			PlayerSetting ps1 = new PlayerSetting("cat", "grp", "Key 1", KeyCode.Mouse0, options1);
			PlayerSetting ps2 = new PlayerSetting("cat", "grp", "Key 2", KeyCode.Mouse1, options2);

			mgr.Add(new List<PlayerSetting> { ps1, ps2 });

			// Force duplicate binding: bind ps2 to the same bound key as ps1.
			ps2.Value = ps1.GetValue<KeyBinding>();

			KeyBinding v1 = ps1.GetValue<KeyBinding>();
			KeyBinding v2 = ps2.GetValue<KeyBinding>();

			bool ps1IsNone = v1.KeyCode == KeyCode.None;
			bool ps2IsNone = v2.KeyCode == KeyCode.None;

			Assert.That(ps1IsNone ^ ps2IsNone, Is.True, "Exactly one setting must be set to None.");

			Dictionary<int, KeyBinding> map = GetKeyBindingMap(mgr);

			KeyBinding d1 = ps1.GetDefaultValue<KeyBinding>();
			KeyBinding d2 = ps2.GetDefaultValue<KeyBinding>();

			Assert.That(map.ContainsKey(d1.Encoded), Is.True);
			Assert.That(map.ContainsKey(d2.Encoded), Is.True);

			Assert.That(map[d1.Encoded], Is.EqualTo(ps1.GetValue<KeyBinding>()));
			Assert.That(map[d2.Encoded], Is.EqualTo(ps2.GetValue<KeyBinding>()));
		}

		private static Dictionary<int, KeyBinding> GetKeyBindingMap( PlayerSettings _mgr )
		{
			FieldInfo fi = typeof(PlayerSettings).GetField(
				"m_keyBindings",
				BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.That(fi, Is.Not.Null, "Field 'm_keyBindings' not found via reflection.");

			return (Dictionary<int, KeyBinding>)fi.GetValue(_mgr);
		}
	}
}
