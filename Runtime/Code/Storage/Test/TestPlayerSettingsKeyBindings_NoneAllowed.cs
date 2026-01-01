using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Unit tests for key binding validation (None not allowed).
	/// </summary>
	/// <remarks>
	/// This file is part of the storage unit test suite.
	/// </remarks>
	public sealed class TestPlayerSettingsKeyBindings_NoneAllowed
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
		public void MultipleNoneBindings_AreAllowed_AndRemainStable()
		{
			PlayerSettings mgr = PlayerSettings.Instance;

			var options = new PlayerSettingOptions();
			options.IsSaveable = false;

			// Default keys must still be unique
			PlayerSetting ps1 = new PlayerSetting("cat", "grp", "Key 1", KeyCode.Mouse0, options);
			PlayerSetting ps2 = new PlayerSetting("cat", "grp", "Key 2", KeyCode.Mouse1, options);

			mgr.Add(new List<PlayerSetting> { ps1, ps2 });

			// Both set to None
			ps1.Value = KeyCode.None;
			ps2.Value = KeyCode.None;

			Assert.That(ps1.GetValue<KeyCode>(), Is.EqualTo(KeyCode.None));
			Assert.That(ps2.GetValue<KeyCode>(), Is.EqualTo(KeyCode.None));

			Dictionary<KeyCode, KeyCode> map = GetKeyCodeMap(mgr);

			// Both defaults still exist as keys
			Assert.That(map.ContainsKey(KeyCode.Mouse0), Is.True);
			Assert.That(map.ContainsKey(KeyCode.Mouse1), Is.True);

			// Both map to None
			Assert.That(map[KeyCode.Mouse0], Is.EqualTo(KeyCode.None));
			Assert.That(map[KeyCode.Mouse1], Is.EqualTo(KeyCode.None));
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
