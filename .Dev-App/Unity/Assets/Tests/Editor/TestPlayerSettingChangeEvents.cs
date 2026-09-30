using NUnit.Framework;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the rule that a player setting only reports a change when its value actually changed.
	///
	/// The case behind it: the settings dialog restored every value on Cancel and fired for each of them,
	/// changed or not. A cheat whose listener is not idempotent - it switched the running season event -
	/// then undid game state every time the dialog was closed.
	///
	/// OnChanged is a plain UnityAction and fires in Edit Mode. The global EvPlayerSettingChanged is a CEvent
	/// and stays silent here, so it is not what these tests count.
	/// </summary>
	[EditorAware]
	public class TestPlayerSettingChangeEvents
	{
		private enum ETestMode
		{
			First,
			Second,
		}

		private static PlayerSetting Create( object _defaultValue, System.Action _onChanged )
		{
			var setting = new PlayerSetting("Test", "Test", "TestSetting", _defaultValue,
				new PlayerSettingOptions { IsSaveable = false, OnChanged = _ => _onChanged() });
			setting.AllowInvokeEvents = true;
			return setting;
		}

		[Test]
		public void SettingADifferentValue_Fires()
		{
			int fired = 0;
			var setting = Create(false, () => fired++);

			setting.Value = true;

			Assert.AreEqual(1, fired);
		}

		[Test]
		public void SettingTheSameValue_DoesNotFire()
		{
			int fired = 0;
			var setting = Create("a", () => fired++);

			setting.Value = "a";
			Assert.AreEqual(0, fired, "the default again");

			setting.Value = "b";
			setting.Value = "b";
			Assert.AreEqual(1, fired, "one change, then the same value again");
		}

		[Test]
		public void AnEnumSetting_ComparesByValue()
		{
			// An enum setting stores an int until it is first set, and the enum afterwards
			int fired = 0;
			var setting = Create(ETestMode.First, () => fired++);

			setting.Value = ETestMode.First;
			Assert.AreEqual(0, fired, "the stored int and the enum are the same value");

			setting.Value = ETestMode.Second;
			Assert.AreEqual(1, fired);
		}

		[Test]
		public void RestoringAnUneditedValue_ReportsNoChange()
		{
			var setting = Create(0.5f, () => { });

			setting.TempSaveValue();

			Assert.IsFalse(setting.TempRestoreValue());
			Assert.AreEqual(0.5f, setting.GetValue<float>());
		}

		[Test]
		public void RestoringAnEditedValue_ReportsTheChange_AndPutsTheValueBack()
		{
			var setting = Create(0.5f, () => { });

			setting.TempSaveValue();
			setting.Value = 0.8f;

			Assert.IsTrue(setting.TempRestoreValue());
			Assert.AreEqual(0.5f, setting.GetValue<float>());
		}

		[Test]
		public void AnEditThatWasUndoneByHand_ReportsNoChange()
		{
			var setting = Create("a", () => { });

			setting.TempSaveValue();
			setting.Value = "b";
			setting.Value = "a";

			Assert.IsFalse(setting.TempRestoreValue(), "the value is what it was when the dialog opened");
		}
	}
}
