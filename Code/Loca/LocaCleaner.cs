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
//			Debug.Log($"{_component.gameObject.scene.name}:{_component.gameObject.name}");
			UiMain.LocaManager.AddKey(_component.Key);
		}
	}
}
#endif