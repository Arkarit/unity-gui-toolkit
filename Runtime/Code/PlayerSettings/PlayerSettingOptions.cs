using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// <summary>
	/// Player setting type enum.
	/// Usually default (Auto) can be used for int, float, bool and string.
	/// Only if the player setting needs special handling (e.g. radio buttons), the type needs to be explicitly defined
	/// </summary>
	public enum EPlayerSettingType
	{
		Auto,       //!< Automatically determined
		Language,   //!< Special case language type
		Radio,      //!< Special case radio buttons
	}
	
	public enum EKeyPolicy
	{
		SingleKey,
		KeyWithModifiers
	}

	/// <summary>
	/// Additional options for player settings.
	/// Used to keep PlayerSetting ctor small and clear.
	/// Also, we keep all options for all PlayerSetting flavors in one simple class without hierarchy to keep things simple
	/// </summary>
	public class PlayerSettingOptions
	{
		public static readonly List<KeyCode> KeyCodeNoMouseList =       //!< Convenient filter list for all mouse keys forbidden or only mouse keys allowed, depending on KeyCodeFilterListIsWhitelist
		new()
		{
			KeyCode.Mouse0,
			KeyCode.Mouse1,
			KeyCode.Mouse2,
			KeyCode.Mouse3,
			KeyCode.Mouse4,
			KeyCode.Mouse5,
			KeyCode.Mouse6,
#if UNITY_2023_1_OR_NEWER
			KeyCode.WheelDown,
			KeyCode.WheelUp,
#endif
		};

		public EPlayerSettingType Type = EPlayerSettingType.Auto;           //!< Player setting type. Usually left default (Auto: automatically determined)
		public string Key = null;                                           //!< Key. If left null or empty, player setting title is used as key.
		public List<string> Icons;                                          //!< List of icons to be used, depending on player setting type
		public List<string> Titles;                                         //!< Titles for UI display(optional, else string values are also used as titles)
		public List<string> StringValues;                                   //!< String values for string based PlayerSettingOptions
		public List<KeyCode> KeyCodeFilterList;                             //!< Filter list for keycodes
		public bool KeyCodeFilterListIsWhitelist;                           //!< Set to true if you want the filter list to be whitelist instead of blacklist
		public bool IsLocalized = true;                                     //!< Should usually be set to true; only set to false if you want to display languages (see TestMain language setting)
		public UnityAction<PlayerSetting> OnChanged = null;                 //!< Optional callback. To react to player setting changes, you may either use this or the global event UiEventDefinitions.EvPlayerSettingChanged
		public bool IsSaveable = true;                                      //!< Is the option saved in player prefs? Obviously usually true, but can be set to false for cheats etc.
		public object CustomData = null;                                    //!< Optional custom data to hand over to your handler
		public Func<float, string> ValueToStringFn = null;                  //!< Optional slider text conversion. If left null, no text is displayed.
		public GameObject CustomPrefab = null;                              //!< Custom prefab to be used in Ui
		public EKeyPolicy KeyPolicy = EKeyPolicy.KeyWithModifiers;			//!< Determines, if modifiers are allowed for this key

		public static PlayerSettingOptions NoMouseKeys =>
			new()
			{
				KeyCodeFilterList = KeyCodeNoMouseList
			};

		public static PlayerSettingOptions OnlyMouseKeys =>
			new()
			{
				KeyCodeFilterList = KeyCodeNoMouseList,
				KeyCodeFilterListIsWhitelist = true,
			};
	}
}