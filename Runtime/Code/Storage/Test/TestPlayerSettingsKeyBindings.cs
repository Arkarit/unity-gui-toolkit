using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
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
		public void DuplicateBoundKey_IsResolved_AndKeyCodeMapIsConsistent()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options1 = new PlayerSettingOptions();
			options1.IsSaveable = false;

			var options2 = new PlayerSettingOptions();
			options2.IsSaveable = false;

			// Default keys must be unique (your Add() enforces this).
			PlayerSetting ps1 = new PlayerSetting("cat", "grp", "Key 1", KeyCode.Mouse0, options1);
			PlayerSetting ps2 = new PlayerSetting("cat", "grp", "Key 2", KeyCode.Mouse1, options2);

			mgr.Add(new List<PlayerSetting> { ps1, ps2 });

			// Force duplicate binding: bind ps2 to the same bound key as ps1.
			ps2.Value = ps1.GetValue<KeyCode>();

			KeyCode v1 = ps1.GetValue<KeyCode>();
			KeyCode v2 = ps2.GetValue<KeyCode>();

			bool ps1IsNone = v1 == KeyCode.None;
			bool ps2IsNone = v2 == KeyCode.None;

			Assert.That(ps1IsNone ^ ps2IsNone, Is.True, "Exactly one setting must be set to None.");

			Dictionary<KeyCode, KeyCode> map = GetKeyCodeMap(mgr);

			Assert.That(map.ContainsKey(KeyCode.Mouse0), Is.True);
			Assert.That(map.ContainsKey(KeyCode.Mouse1), Is.True);

			Assert.That(map[KeyCode.Mouse0], Is.EqualTo(ps1.GetValue<KeyCode>()));
			Assert.That(map[KeyCode.Mouse1], Is.EqualTo(ps2.GetValue<KeyCode>()));
		}

		private static Dictionary<KeyCode, KeyCode> GetKeyCodeMap( PlayerSettings _mgr )
		{
			FieldInfo fi = typeof(PlayerSettings).GetField(
				"m_keyCodes",
				BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.That(fi, Is.Not.Null, "Field 'm_keyCodes' not found via reflection.");

			return (Dictionary<KeyCode, KeyCode>)fi.GetValue(_mgr);
		}
	}
}
