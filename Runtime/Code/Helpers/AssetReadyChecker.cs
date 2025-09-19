#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Editor-only helper to check if all ScriptableObjects are fully imported.
	/// Single public method by design, for use from AssetReadyGate.
	/// </summary>
	public static class AssetReadyChecker
	{
		/// <summary>
		/// Returns true if the editor is calm (no compile/update) and
		/// all ScriptableObjects under the given folders are fully imported.
		/// Defaults to "Assets" to avoid scanning Packages.
		/// </summary>
		public static bool AllScriptableObjectsReady
		{
			get
			{
				if (Application.isPlaying)
					return true;
				
				// Editor must be calm first.
				if (EditorApplication.isCompiling || EditorApplication.isUpdating)
					return false;

				string[] folders = new[] { "Assets", "Packages" };

				// GUID-level search is safe even while importer churns.
				string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", folders);

				for (int i = 0; i < guids.Length; i++)
				{
					string guid = guids[i];
					string path = AssetDatabase.GUIDToAssetPath(guid);

					// If path cannot be resolved (and editor is calm), treat as missing, not pending.
					if (string.IsNullOrEmpty(path))
					{
						continue;
					}

					// Folders are not assets to wait for.
					if (AssetDatabase.IsValidFolder(path))
					{
						continue;
					}

					// If file is not on disk (and editor is calm), treat as missing, not pending.
					if (!File.Exists(path))
					{
						continue;
					}

					// Importer is done when the main type is known.
					Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
					if (mainType == null)
					{
						// Still pending.
						return false;
					}
				}

				// All checked paths are ready (or irrelevant).
				return true;
			}
		}
	}
}
#endif
