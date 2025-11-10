using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public class LocaProviderList
	{
		public const string RESOURCES_SUB_PATH = "LocaJson/";
		private const string PATH = RESOURCES_SUB_PATH + "_locaProviders";
		private const string EDITOR_PATH = "Assets/Resources/" + PATH + ".json";

		public List<string> Paths = new();

		public static LocaProviderList Load()
		{
			var text = Resources.Load<TextAsset>(PATH);
			if (text == null)
				return null;
			
			string json = text.text;
			return JsonUtility.FromJson<LocaProviderList>(json);
		}

#if UNITY_EDITOR
		public void Save()
		{
			EditorFileUtility.EnsureUnityFolderExists("Assets/Resources/" + RESOURCES_SUB_PATH);

			string json = JsonUtility.ToJson(this, true);
			File.WriteAllText(EDITOR_PATH, json, new UTF8Encoding(false));
		}
#endif
	}
}