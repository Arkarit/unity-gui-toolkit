using System.Collections.Generic;
using GuiToolkit.Debugging;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.Tools
{
	public static class DebugTools
	{
		private const string BaseMenuEntry = "Assets/Debug/";

		private const string DumpOverridesName = "Dump Overrides";
		private const string DumpOverridesEntry = BaseMenuEntry + DumpOverridesName;
		private const int DumpOverridesOrder = -400;

		[MenuItem(DumpOverridesEntry, false, DumpOverridesOrder)]
		private static void DumpOverrides()
		{
			var prefabVariants = PrefabVariants;
			if (prefabVariants == null || prefabVariants.Count == 0)
				return;

			foreach (var prefabVariant in prefabVariants)
			{
				Debug.Log(DebugUtility.DumpOverridesString(prefabVariant, prefabVariant.GetPath(1)));
			}
		}

		[MenuItem(DumpOverridesEntry, true)]
		private static bool DumpOverridesValidation() => ValidatePrefabVariantsSelected;

		private static GameObject[] GameObjects
		{
			get
			{
				if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
					return Selection.gameObjects;
				return new[] { Selection.activeObject as GameObject };
			}
		}

		private static List<GameObject> PrefabVariants
		{
			get
			{
				var result = new List<GameObject>();
				var gameObjects = GameObjects;
				if (gameObjects == null ||  gameObjects.Length == 0) 
					return result;

				foreach (var gameObject in gameObjects)
				{
					if (gameObject != null && PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
						result.Add(gameObject);
				}

				return result;
			}
		}

		private static bool ValidatePrefabVariantsSelected => PrefabVariants != null && PrefabVariants.Count > 0;
		private static bool ValidateGameObjectsSelected => GameObjects != null && GameObjects.Length > 0;

	}
}