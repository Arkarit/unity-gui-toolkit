using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Allow-list of raw UGUI / Unity building blocks that should be authorable as screen-node
	/// <c>type</c>s, in addition to the toolkit's own <c>Ui*</c> components. The catalog generator only
	/// picks up types whose name starts with "Ui" (see <see cref="UiScreenCatalogGenerator"/>), so plain
	/// UGUI types (Image, ScrollRect, LayoutElement, CanvasGroup, ...) would otherwise be invisible to
	/// the authoring AI even though the baker can build them.
	///
	/// The palette holds <em>composite</em> building blocks (whole prefabs); this list holds the
	/// <em>atomic</em> ones (single raw components).
	///
	/// Defaults ship in code (<see cref="DefaultEntries"/>), so the generator works with no asset present.
	/// Create an asset via <c>Assets → Create → Gui Toolkit → AI → Authorable Unity Types Config</c> to
	/// edit the list (it is pre-filled with the defaults on creation); the generator then uses the asset's
	/// entries verbatim. Only the first such asset in the project is used.
	/// </summary>
	[CreateAssetMenu(fileName = "UiAuthorableUnityTypesConfig",
		menuName = StringConstants.AI_HEADER + "Authorable Unity Types Config")]
	public class UiAuthorableUnityTypesConfig : ScriptableObject
	{
		[Serializable]
		public class Entry
		{
			[Tooltip("Full type name of the raw Unity/UGUI component (e.g. 'UnityEngine.UI.Image', " +
			         "'UnityEngine.CanvasGroup'). A short name (e.g. 'Image') also resolves.")]
			public string unityType = "";

			[Tooltip("Heuristic category for the catalog entry (Graphic/Input/Layout/Text/Misc/...). " +
			         "Empty = auto-classify.")]
			public string category = "";

			[Tooltip("Optional: name of a toolkit wrapper the author should usually prefer over this raw type " +
			         "(advisory only; the raw type stays authorable). Empty = no wrapper.")]
			public string prefer = "";

			[Tooltip("If true, this type is excluded from the catalog (a hard 'must use the wrapper' switch).")]
			public bool hidden;

			public Entry() { }

			public Entry( string _unityType, string _category, string _prefer = "" )
			{
				unityType = _unityType;
				category = _category;
				prefer = _prefer;
			}
		}

		[Tooltip("Raw Unity/UGUI types to expose as authorable element types. Pre-filled with the built-in " +
		         "defaults; edit freely (add client types, set 'hidden', add 'prefer' hints).")]
		public List<Entry> Entries = new();

		/// <summary>
		/// The built-in default allow-list. This is the single source of truth for "vorbefüllt": the generator
		/// falls back to it when no config asset exists, and a freshly created asset is seeded from it (Reset).
		/// </summary>
		public static List<Entry> DefaultEntries() => new()
		{
			// Graphics
			new("UnityEngine.UI.Image", "Graphic"),
			new("UnityEngine.UI.RawImage", "Graphic"),

			// Text (steer to the toolkit's localized text where possible)
			new("TMPro.TextMeshProUGUI", "Text", "UiLocalizedTextMeshProUGUI"),

			// Input (toolkit wrappers exist for the common ones)
			new("UnityEngine.UI.Button", "Input", "UiButton"),
			new("UnityEngine.UI.Toggle", "Input", "UiToggle"),
			new("UnityEngine.UI.Slider", "Input"),
			new("UnityEngine.UI.Scrollbar", "Input"),
			new("TMPro.TMP_Dropdown", "Input"),
			new("TMPro.TMP_InputField", "Input"),

			// Scroll / masking
			new("UnityEngine.UI.ScrollRect", "Layout", "UiScrollRect"),
			new("UnityEngine.UI.Mask", "Layout"),
			new("UnityEngine.UI.RectMask2D", "Layout"),

			// Layout driving
			new("UnityEngine.UI.LayoutElement", "Layout"),
			new("UnityEngine.UI.ContentSizeFitter", "Layout"),
			new("UnityEngine.UI.AspectRatioFitter", "Layout"),
			new("UnityEngine.UI.HorizontalLayoutGroup", "Layout", "UiHorizontalOrVerticalLayoutGroup"),
			new("UnityEngine.UI.VerticalLayoutGroup", "Layout", "UiHorizontalOrVerticalLayoutGroup"),
			new("UnityEngine.UI.GridLayoutGroup", "Layout"),

			// Misc — CanvasGroup: fade / interaction toggle of a whole subtree (properties, not fields).
			new("UnityEngine.CanvasGroup", "Misc"),
		};

		private void Reset()
		{
			// Seed a freshly created asset with the full default list so it appears pre-filled in the Inspector.
			Entries = DefaultEntries();
		}

		/// <summary>The effective entry list: the asset's if one exists, otherwise the built-in defaults.</summary>
		public static List<Entry> EffectiveEntries()
		{
			var config = FindFirst();
			return config != null && config.Entries != null && config.Entries.Count > 0
				? config.Entries
				: DefaultEntries();
		}

		/// <summary>Finds the first Unity-types config in the project, or null if none exists.</summary>
		public static UiAuthorableUnityTypesConfig FindFirst()
		{
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UiAuthorableUnityTypesConfig)}"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var config = AssetDatabase.LoadAssetAtPath<UiAuthorableUnityTypesConfig>(path);
				if (config != null)
					return config;
			}
			return null;
		}
	}
}
