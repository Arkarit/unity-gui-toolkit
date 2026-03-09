using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public class LocaProviderEntry
	{
		public string Path;
		public string TypeName = "GuiToolkit.LocaExcelBridge";
	}

	[Serializable]
	public class LocaProviderList
	{
		public const string RESOURCES_SUB_PATH = "LocaJson/";
		private const string PATH = RESOURCES_SUB_PATH + "_locaProviders";
		private const string EDITOR_PATH = "Assets/Resources/" + PATH + ".json";

		// Migration field: old JSON stored bare path strings here.
		// On load, entries are converted to Providers and this list stays empty.
		public List<string> Paths = new();

		public List<LocaProviderEntry> Providers = new();

		public static LocaProviderList Load()
		{
			var text = Resources.Load<TextAsset>(PATH);
			if (text == null)
				return null;

			string json = text.text;
			var result = JsonUtility.FromJson<LocaProviderList>(json);

			// Migrate old string-only entries to LocaProviderEntry
			if (result.Providers.Count == 0 && result.Paths.Count > 0)
			{
				foreach (var path in result.Paths)
					result.Providers.Add(new LocaProviderEntry { Path = path });
				result.Paths.Clear();
			}

			return result;
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