#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class LocaSaver
	{
		static LocaSaver()
		{
			EditorSceneManager.sceneSaving += OnSceneSaving;
		}

		private static void OnSceneSaving( Scene _scene, string __ )
		{
			List<ILocaClient> clients = UiEditorUtility.FindObjectsOfType<ILocaClient>(_scene);
			
			UiMain.LocaManager.ReadKeyData();

			foreach (var client in clients)
				UiMain.LocaManager.AddKey(client.Key);

			UiMain.LocaManager.WriteKeyData();
		}
	}
}

#endif