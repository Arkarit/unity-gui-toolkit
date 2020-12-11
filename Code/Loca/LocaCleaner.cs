#if UNITY_EDITOR
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
			UiEditorUtility.FindAllComponentsInAllScenes<UiLocaClientBase>((client) => Debug.Log(client.gameObject.name) );
		}
	}
}
#endif