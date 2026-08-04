using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Optional override asset for the AI screen-authoring palette. The palette is auto-scanned from
	/// <c>.../Prefabs/StandardElements/</c> (see <see cref="UiScreenCatalogGenerator"/>); this asset
	/// only corrects or extends that automatic result:
	/// <list type="bullet">
	/// <item>add extra folders / individual prefabs the auto-scan misses (e.g. client widgets),</item>
	/// <item>hide entries that should not be authorable,</item>
	/// <item>fix a heuristic entry's description / category / slots.</item>
	/// </list>
	/// Create via <c>Assets → Create → Gui Toolkit → AI → Authorable Palette Config</c>. The generator
	/// picks up the first such asset it finds in the project; none is required.
	/// </summary>
	[CreateAssetMenu(fileName = "UiAuthorablePaletteConfig",
		menuName = StringConstants.MENU_HEADER + "AI/Authorable Palette Config")]
	public class UiAuthorablePaletteConfig : ScriptableObject
	{
		[Tooltip("Additional folders to scan for palette prefabs (in addition to the built-in StandardElements scan).")]
		public List<DefaultAsset> ExtraFolders = new();

		[Tooltip("Additional individual prefabs to include as palette entries.")]
		public List<GameObject> ExtraPrefabs = new();

		[Tooltip("Palette entry names to exclude entirely.")]
		public List<string> HiddenNames = new();

		[Tooltip("Per-entry corrections applied on top of the auto-scanned result (matched by name).")]
		public List<Override> Overrides = new();

		[Serializable]
		public class Override
		{
			[Tooltip("The auto-scanned palette entry name this override applies to.")]
			public string name = "";

			[Tooltip("If set, replaces the auto-derived category.")]
			public string category = "";

			[Tooltip("If set, replaces the auto-derived description.")]
			public string description = "";

			[Tooltip("If set (non-empty), replaces the auto-derived slots wholesale.")]
			public List<UiPaletteSlot> slots = new();
		}

		/// <summary>Project-relative folder paths from <see cref="ExtraFolders"/> (null/invalid entries dropped).</summary>
		public IEnumerable<string> ExtraFolderPaths()
		{
			foreach (var folder in ExtraFolders)
			{
				if (folder == null)
					continue;
				string path = AssetDatabase.GetAssetPath(folder);
				if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
					yield return path;
			}
		}

		/// <summary>Finds the first palette config in the project, or null if none exists.</summary>
		public static UiAuthorablePaletteConfig FindFirst()
		{
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UiAuthorablePaletteConfig)}"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var config = AssetDatabase.LoadAssetAtPath<UiAuthorablePaletteConfig>(path);
				if (config != null)
					return config;
			}
			return null;
		}

		public Override FindOverride( string _name )
		{
			foreach (var o in Overrides)
				if (o != null && o.name == _name)
					return o;
			return null;
		}

		public bool IsHidden( string _name ) => HiddenNames.Contains(_name);
	}
}
