#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class LocaSaver
	{
		static LocaSaver()
		{
			EditorSceneManager.sceneSaving += OnSceneSaving;
		}

		private static void OnSceneSaving( Scene _, string __ )
		{
			UiLocaClientBase[] clients = Resources.FindObjectsOfTypeAll<UiLocaClientBase>();
			
			UiMain.LocaManager.ReadKeyData();

			foreach (var client in clients)
				UiMain.LocaManager.AddKey(client.Key);

			UiMain.LocaManager.WriteKeyData();
		}
	}
}

#endif