#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Utilities for directly manipulating Unity YAML asset files (scenes and prefabs).
	/// Enables swapping a component's script type in-place, preserving all serialized field
	/// values and keeping all external references to the component intact.
	/// All methods are editor-only.
	/// </summary>
	public static class YamlUtility
	{
		// Unity YAML class ID for MonoBehaviour.
		private const string MonoBehaviourClassId = "114";

		/// <summary>
		/// Returns the Unity-relative asset path of the scene or prefab currently being edited
		/// that contains <paramref name="go"/>. Returns <c>null</c> if the object is not yet
		/// saved (new unsaved scene or brand-new GameObject that has never been part of a saved asset).
		/// </summary>
		public static string GetEditedAssetPath(GameObject go)
		{
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
				return prefabStage.assetPath;

			var scene = EditorSceneManager.GetActiveScene();
			return string.IsNullOrEmpty(scene.path) ? null : scene.path;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="go"/> is part of a prefab instance in a scene
		/// (as opposed to being a plain scene object or a prefab being edited directly in Prefab Mode).
		/// Components on prefab instances cannot be patched via YAML manipulation because their
		/// serialized data lives in the source prefab asset, not in the scene file.
		/// </summary>
		public static bool IsPartOfPrefabInstance(GameObject go)
			=> PrefabUtility.IsPartOfPrefabInstance(go);

		/// <summary>
		/// Gets the local file identifier of a Unity object within its containing scene or prefab.
		/// This corresponds to the YAML anchor (<c>&amp;localId</c>) that heads the object's block.
		/// </summary>
		public static bool TryGetLocalFileId(UnityEngine.Object obj, out long localId)
			=> AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out localId);

		/// <summary>
		/// Returns the MonoScript asset GUID for the script attached to a live MonoBehaviour instance.
		/// Prefer this over <see cref="FindMonoScriptGuid"/> when you hold a reference to the component.
		/// </summary>
		public static string GetMonoScriptGuid(MonoBehaviour component)
		{
			var script = MonoScript.FromMonoBehaviour(component);
			if (script == null)
				return null;
			string path = AssetDatabase.GetAssetPath(script);
			return string.IsNullOrEmpty(path) ? null : AssetDatabase.AssetPathToGUID(path);
		}

		/// <summary>
		/// Finds the MonoScript asset GUID for a given C# type by searching all MonoScript assets.
		/// Use when you do not have a live component reference.
		/// </summary>
		public static string FindMonoScriptGuid(Type type)
		{
			string[] guids = AssetDatabase.FindAssets($"t:MonoScript {type.Name}");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (script != null && script.GetClass() == type)
					return guid;
			}
			return null;
		}

		/// <summary>
		/// Saves the currently active scene, or the prefab open in Prefab Mode.
		/// Returns <c>true</c> on success.
		/// </summary>
		public static bool SaveCurrentSceneOrPrefab()
		{
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath, out bool success);
				return success;
			}

			return EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
		}

		/// <summary>
		/// Replaces the <c>m_Script</c> GUID of a single MonoBehaviour block inside a Unity YAML
		/// asset file, identified by the component's local file ID (YAML anchor). All serialized
		/// field values and all external references to the component remain intact.
		/// The asset is reimported after the file is written.
		/// </summary>
		/// <param name="assetPath">Unity-relative path, e.g. <c>"Assets/Scenes/MyScene.unity"</c>.</param>
		/// <param name="localFileId">Local file ID from <see cref="TryGetLocalFileId"/>.</param>
		/// <param name="oldScriptGuid">GUID of the script to replace.</param>
		/// <param name="newScriptGuid">GUID of the replacement script.</param>
		/// <returns><c>true</c> if the replacement was written successfully.</returns>
		public static bool SwapComponentScript(string assetPath, long localFileId,
		                                       string oldScriptGuid, string newScriptGuid)
		{
			string fullPath = AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				UiLog.LogError($"YamlUtility: File not found: {fullPath}");
				return false;
			}

			string yaml = File.ReadAllText(fullPath);

			// Locate the MonoBehaviour block by its YAML anchor, e.g. "--- !u!114 &1234567890"
			string blockMarker = $"--- !u!{MonoBehaviourClassId} &{localFileId}";
			int blockStart = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
			if (blockStart < 0)
			{
				UiLog.LogError($"YamlUtility: YAML block &{localFileId} not found in {assetPath}");
				return false;
			}

			// Find the end of this block (start of the next YAML document)
			int searchFrom = blockStart + blockMarker.Length;
			int nextBlock = yaml.IndexOf("\n---", searchFrom, StringComparison.Ordinal);
			int blockEnd = nextBlock < 0 ? yaml.Length : nextBlock;

			// Build the token to match — include ", type:" so we only touch the m_Script line
			// and not any other guid that might appear elsewhere in the block.
			string oldToken = $"guid: {oldScriptGuid}, type:";
			string newToken = $"guid: {newScriptGuid}, type:";

			string block = yaml.Substring(blockStart, blockEnd - blockStart);
			int tokenIdx = block.IndexOf(oldToken, StringComparison.Ordinal);
			if (tokenIdx < 0)
			{
				UiLog.LogError($"YamlUtility: Expected m_Script guid '{oldScriptGuid}' not found in block &{localFileId} in {assetPath}");
				return false;
			}

			string modifiedBlock = block.Substring(0, tokenIdx) + newToken + block.Substring(tokenIdx + oldToken.Length);
			string result = yaml.Substring(0, blockStart) + modifiedBlock + yaml.Substring(blockEnd);

			File.WriteAllText(fullPath, result);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			return true;
		}

		/// <summary>
		/// Converts a Unity-relative asset path to an absolute file-system path.
		/// </summary>
		public static string AssetPathToFullPath(string assetPath)
			=> Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
	}
}
#endif
