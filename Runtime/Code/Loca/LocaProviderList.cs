using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Represents a single localization provider entry in the provider registry.
	/// Stores the Resources path and type name needed to dynamically load an <see cref="ILocaProvider"/>.
	/// </summary>
	[Serializable]
	public class LocaProviderEntry
	{
		/// <summary>
		/// The Resources-relative path to the provider asset (without file extension).
		/// Example: "LocaJson/MyExcelBridge"
		/// </summary>
		public string Path;
		
		/// <summary>
		/// The fully-qualified type name of the provider (e.g., "GuiToolkit.LocaExcelBridge").
		/// Used to cast the loaded asset to <see cref="ILocaProvider"/>.
		/// </summary>
		public string TypeName = "GuiToolkit.LocaExcelBridge";
	}

	/// <summary>
	/// Persistent registry of <see cref="ILocaProvider"/> assets to load at runtime.
	/// Stored as a JSON file in Resources and loaded by <see cref="LocaManagerDefaultImpl"/>.
	/// The Loca processor tool auto-generates this list by scanning for provider ScriptableObjects.
	/// </summary>
	[Serializable]
	public class LocaProviderList
	{
		/// <summary>Resources subdirectory where provider JSON and related data are stored.</summary>
		public const string RESOURCES_SUB_PATH = "LocaJson/";
		private const string PATH = RESOURCES_SUB_PATH + "_locaProviders";
		private const string EDITOR_PATH = "Assets/Resources/" + PATH + ".json";

		/// <summary>
		/// (Migration field) Old JSON format stored bare path strings here.
		/// On load, entries are converted to <see cref="Providers"/> and this list stays empty.
		/// </summary>
		public List<string> Paths = new();

		/// <summary>
		/// The list of provider entries. Each entry describes one <see cref="ILocaProvider"/> to load.
		/// </summary>
		public List<LocaProviderEntry> Providers = new();

		/// <summary>
		/// Loads the provider list from Resources.
		/// Automatically migrates old string-based entries to <see cref="LocaProviderEntry"/> format.
		/// </summary>
		/// <returns>The loaded <see cref="LocaProviderList"/>, or null if not found.</returns>
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
		/// <summary>
		/// (Editor-only) Saves the provider list to the JSON file in Resources.
		/// Ensures the directory exists before writing.
		/// </summary>
		public void Save()
		{
			EditorFileUtility.EnsureUnityFolderExists("Assets/Resources/" + RESOURCES_SUB_PATH);

			string json = JsonUtility.ToJson(this, true);
			File.WriteAllText(EDITOR_PATH, json, new UTF8Encoding(false));
		}
#endif
	}
}