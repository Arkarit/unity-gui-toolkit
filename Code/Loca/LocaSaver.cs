#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class LocaSaver
	{
		static LocaSaver()
		{
			EditorSceneManager.sceneSaving += OnSceneSaving;
		}

		public static void SaveKeys( Scene _scene )
		{
			List<ILocaClient> clients = UiEditorUtility.FindObjectsOfType<ILocaClient>(_scene);

			UiMain.LocaManager.ReadKeyData();

			foreach (var client in clients)
			{
				if (client.UsesMultipleLocaKeys)
				{
					var keys = client.LocaKeys;
					foreach (var key in keys)
						UiMain.LocaManager.AddKey(client.LocaGroup, key);
				}
				else
				{
					UiMain.LocaManager.AddKey(client.LocaGroup, client.LocaKey);
				}
			}

			UiMain.LocaManager.WriteKeyData();
		}

		private static void OnSceneSaving( Scene _scene, string __ )
		{
			SaveKeys(_scene);
		}
	}

	public class OnAssetSave : UnityEditor.AssetModificationProcessor
	{
		static string[] OnWillSaveAssets(string[] _paths)
		{
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage == null)
				return _paths;

			LocaSaver.SaveKeys(prefabStage.scene);
			return _paths;
		}
	}

}

#endif