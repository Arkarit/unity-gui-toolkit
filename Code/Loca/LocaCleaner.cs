#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public static class LocaCleaner
	{
		[MenuItem(StringConstants.LOCA_CLEANER_MENU_NAME)]
		public static void Clean()
		{
			UiMain.LocaManager.Clear();

			UiEditorUtility.FindAllComponentsInAllScenesAndPrefabs<ILocaClient>(FoundComponent);

			UiMain.LocaManager.WriteKeyData();
		}

		private static void FoundComponent( ILocaClient _component )
		{
			if (_component.UsesMultipleLocaKeys)
			{
				var keys = _component.LocaKeys;
				foreach (var key in keys)
					UiMain.LocaManager.AddKey(key);

				return;
			}

			UiMain.LocaManager.AddKey(_component.LocaKey);
		}
	}
}
#endif