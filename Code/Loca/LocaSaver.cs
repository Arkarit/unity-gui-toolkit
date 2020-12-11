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
			{
				if (client.UsesMultipleLocaKeys)
				{
					var keys = client.LocaKeys;
					foreach (var key in keys)
						UiMain.LocaManager.AddKey(key);
				}
				else
				{
					UiMain.LocaManager.AddKey(client.LocaKey);
				}
			}

			UiMain.LocaManager.WriteKeyData();
		}
	}
}

#endif