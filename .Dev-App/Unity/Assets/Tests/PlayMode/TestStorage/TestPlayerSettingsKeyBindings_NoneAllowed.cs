using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	/// <summary>
	/// Unit tests for key binding validation (None allowed).
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

			// Default bindings must still be unique
			PlayerSetting ps1 = new PlayerSetting("cat", "grp", "Key 1", KeyCode.Mouse0, options);
			PlayerSetting ps2 = new PlayerSetting("cat", "grp", "Key 2", KeyCode.Mouse1, options);

			mgr.Add(new List<PlayerSetting> { ps1, ps2 });

			// Both set to None
			ps1.Value = new KeyBinding(KeyCode.None);
			ps2.Value = new KeyBinding(KeyCode.None);

			Assert.That(ps1.GetValue<KeyBinding>().KeyCode, Is.EqualTo(KeyCode.None));
			Assert.That(ps2.GetValue<KeyBinding>().KeyCode, Is.EqualTo(KeyCode.None));

			Dictionary<int, KeyBinding> map = GetKeyBindingMap(mgr);

			KeyBinding default1 = ps1.GetDefaultValue<KeyBinding>();
			KeyBinding default2 = ps2.GetDefaultValue<KeyBinding>();

			// Both defaults still exist as keys
			Assert.That(map.ContainsKey(default1.Encoded), Is.True);
			Assert.That(map.ContainsKey(default2.Encoded), Is.True);

			// Both map to None
			Assert.That(map[default1.Encoded].KeyCode, Is.EqualTo(KeyCode.None));
			Assert.That(map[default2.Encoded].KeyCode, Is.EqualTo(KeyCode.None));
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
