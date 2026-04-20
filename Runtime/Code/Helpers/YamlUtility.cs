#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using System.Collections.Generic;
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
		/// <remarks>
		/// <c>AssetDatabase.TryGetGUIDAndLocalFileIdentifier</c> is unreliable for scene objects.
		/// This method falls back to reading the in-memory <c>m_LocalIdentfierInFile</c> field via
		/// reflection, which is the standard workaround used by many Unity editor tools.
		/// </remarks>
		public static bool TryGetLocalFileId(UnityEngine.Object obj, out long localId)
		{
			// Official API — reliable for asset objects (prefabs, ScriptableObjects, etc.).
			if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out _, out localId) && localId != 0)
				return true;

			// Fallback via reflection — works for scene objects where the official API returns 0.
			// Sets inspectorMode to Debug to expose the hidden m_LocalIdentfierInFile property.
			try
			{
				var so = new SerializedObject(obj);
				var inspectorModeProp = typeof(SerializedObject)
					.GetProperty("inspectorMode", BindingFlags.NonPublic | BindingFlags.Instance);
				inspectorModeProp?.SetValue(so, InspectorMode.Debug);
				var idProp = so.FindProperty("m_LocalIdentfierInFile");
				if (idProp != null)
				{
					localId = idProp.longValue;
					return localId != 0;
				}
			}
			catch (Exception ex)
			{
				UiLog.LogWarning($"[YamlUtility] TryGetLocalFileId reflection fallback failed: {ex.Message}");
			}

			localId = 0;
			return false;
		}

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
			string result = PatchYaml(yaml, localFileId, oldScriptGuid, newScriptGuid);
			if (result == null)
			{
				UiLog.LogError($"YamlUtility: Could not patch block &{localFileId} (script guid '{oldScriptGuid}') in {assetPath}");
				return false;
			}

			File.WriteAllText(fullPath, result);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			return true;
		}

		/// <summary>
		/// Replaces the <c>m_Script</c> GUID inside a MonoBehaviour YAML block identified by its
		/// local file ID (YAML anchor). Pure string operation — no file I/O.
		/// </summary>
		/// <param name="yaml">The full YAML text to search.</param>
		/// <param name="localFileId">YAML anchor value of the target MonoBehaviour block.</param>
		/// <param name="oldScriptGuid">GUID string to replace (must match exactly).</param>
		/// <param name="newScriptGuid">Replacement GUID string.</param>
		/// <returns>
		/// Modified YAML string if successful; <c>null</c> if the block was not found or the
		/// expected script GUID was not present in the block.
		/// </returns>
		internal static string PatchYaml(string yaml, long localFileId,
		                                 string oldScriptGuid, string newScriptGuid)
		{
			string blockMarker = $"--- !u!{MonoBehaviourClassId} &{localFileId}";
			int blockStart = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
			if (blockStart < 0)
				return null;

			// Find the end of this block (start of the next YAML document separator).
			int searchFrom = blockStart + blockMarker.Length;
			int nextBlock = yaml.IndexOf("\n---", searchFrom, StringComparison.Ordinal);
			int blockEnd = nextBlock < 0 ? yaml.Length : nextBlock;

			// Include ", type:" in the token to ensure we only touch the m_Script line and not
			// any other guid values that may appear elsewhere in the block.
			string oldToken = $"guid: {oldScriptGuid}, type:";
			string newToken = $"guid: {newScriptGuid}, type:";

			string block = yaml.Substring(blockStart, blockEnd - blockStart);
			int tokenIdx = block.IndexOf(oldToken, StringComparison.Ordinal);
			if (tokenIdx < 0)
				return null;

			string modifiedBlock = block.Substring(0, tokenIdx) + newToken +
			                       block.Substring(tokenIdx + oldToken.Length);
			return yaml.Substring(0, blockStart) + modifiedBlock + yaml.Substring(blockEnd);
		}

		/// <summary>
		/// Replaces an entire MonoBehaviour block in Unity YAML, identified by its local file ID.
		/// The separator line (<c>--- !u!114 &amp;{localFileId}</c>) is preserved; everything after
		/// it up to the next separator is replaced with <paramref name="newBlockContent"/>.
		/// </summary>
		/// <param name="yaml">The full YAML text.</param>
		/// <param name="localFileId">YAML anchor of the block to replace.</param>
		/// <param name="newBlockContent">
		/// Replacement content starting with <c>MonoBehaviour:\n</c> (i.e. everything AFTER the
		/// <c>--- !u!114 &amp;ID\n</c> header line). Must end without a trailing newline.
		/// </param>
		/// <returns>Modified YAML string, or <c>null</c> if the block was not found.</returns>
		public static string ReplaceMonoBehaviourBlock(string yaml, long localFileId, string newBlockContent)
		{
			string blockMarker = $"--- !u!{MonoBehaviourClassId} &{localFileId}";
			int blockStart = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
			if (blockStart < 0)
				return null;

			int searchFrom = blockStart + blockMarker.Length;
			int nextBlock  = yaml.IndexOf("\n---", searchFrom, StringComparison.Ordinal);
			int blockEnd   = nextBlock < 0 ? yaml.Length : nextBlock;

			// Advance past the header line to find where content begins.
			int afterHeader = yaml.IndexOf('\n', blockStart);
			if (afterHeader < 0 || afterHeader >= blockEnd)
				return null;
			afterHeader++; // move past the '\n'

			return yaml.Substring(0, afterHeader) + newBlockContent + yaml.Substring(blockEnd);
		}

		/// <summary>
		/// Removes an entire MonoBehaviour block from Unity YAML, identified by its local file ID.
		/// The separator line and all block content are removed. The newline immediately preceding
		/// the separator (if any) is also consumed to avoid leaving stray blank lines.
		/// </summary>
		/// <param name="yaml">The full YAML text.</param>
		/// <param name="localFileId">YAML anchor of the block to remove.</param>
		/// <returns>Modified YAML string, or <c>null</c> if the block was not found.</returns>
		public static string RemoveMonoBehaviourBlock(string yaml, long localFileId)
		{
			string blockMarker = $"--- !u!{MonoBehaviourClassId} &{localFileId}";
			int blockStart = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
			if (blockStart < 0)
				return null;

			int searchFrom = blockStart + blockMarker.Length;
			int nextBlock  = yaml.IndexOf("\n---", searchFrom, StringComparison.Ordinal);
			int blockEnd   = nextBlock < 0 ? yaml.Length : nextBlock;

			// Also consume the newline that precedes the separator so we don't leave a blank line.
			int removeFrom = (blockStart > 0 && yaml[blockStart - 1] == '\n') ? blockStart - 1 : blockStart;

			return yaml.Substring(0, removeFrom) + yaml.Substring(blockEnd);
		}

		/// <summary>
		/// Replaces the <c>m_Script</c> GUID of a single MonoBehaviour block and removes specific
		/// named fields from that block, all in a single file pass.
		/// </summary>
		/// <param name="assetPath">Unity-relative path, e.g. <c>"Assets/Scenes/MyScene.unity"</c>.</param>
		/// <param name="localFileId">Local file ID from <see cref="TryGetLocalFileId"/>.</param>
		/// <param name="oldScriptGuid">GUID of the script to replace.</param>
		/// <param name="newScriptGuid">GUID of the replacement script.</param>
		/// <param name="fieldNamesToStrip">
		/// YAML field names (without leading spaces or colon) to remove from the block,
		/// e.g. <c>"m_group"</c>, <c>"m_locaKey"</c>. Matches the exact indented form
		/// <c>"  fieldName: ..."</c>.
		/// </param>
		/// <returns><c>true</c> if the replacement was written successfully.</returns>
		public static bool SwapComponentScriptAndStripFields(string assetPath, long localFileId,
		                                                     string oldScriptGuid, string newScriptGuid,
		                                                     params string[] fieldNamesToStrip)
		{
			string fullPath = AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				UiLog.LogError($"YamlUtility: File not found: {fullPath}");
				return false;
			}

			string yaml = File.ReadAllText(fullPath);
			string result = PatchYaml(yaml, localFileId, oldScriptGuid, newScriptGuid);
			if (result == null)
			{
				UiLog.LogError($"YamlUtility: Could not patch block &{localFileId} (script guid '{oldScriptGuid}') in {assetPath}");
				return false;
			}

			if (fieldNamesToStrip != null && fieldNamesToStrip.Length > 0)
				result = StripFieldsFromBlock(result, localFileId, fieldNamesToStrip);

			File.WriteAllText(fullPath, result);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			return true;
		}

		/// <summary>
		/// Removes specific named fields from a MonoBehaviour block in YAML text.
		/// Matches lines of the form <c>"  fieldName: ..."</c> (two-space indent, as Unity serializes).
		/// </summary>
		/// <param name="yaml">The full YAML text.</param>
		/// <param name="localFileId">YAML anchor of the target block.</param>
		/// <param name="fieldNames">Field names to remove (without leading spaces or colon).</param>
		/// <returns>Modified YAML string; the original string if the block is not found.</returns>
		internal static string StripFieldsFromBlock(string yaml, long localFileId, IReadOnlyList<string> fieldNames)
		{
			string blockMarker = $"--- !u!{MonoBehaviourClassId} &{localFileId}";
			int blockStart = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
			if (blockStart < 0)
				return yaml;

			int searchFrom = blockStart + blockMarker.Length;
			int nextBlock  = yaml.IndexOf("\n---", searchFrom, StringComparison.Ordinal);
			int blockEnd   = nextBlock < 0 ? yaml.Length : nextBlock;

			string before = yaml.Substring(0, blockStart);
			string block  = yaml.Substring(blockStart, blockEnd - blockStart);
			string after  = yaml.Substring(blockEnd);

			foreach (string fieldName in fieldNames)
			{
				string lineStart = "\n  " + fieldName + ":";
				int idx = 0;
				while (idx < block.Length)
				{
					int pos = block.IndexOf(lineStart, idx, StringComparison.Ordinal);
					if (pos < 0)
						break;

					int lineEnd = block.IndexOf('\n', pos + 1);
					if (lineEnd < 0)
						lineEnd = block.Length;

					block = block.Substring(0, pos) + block.Substring(lineEnd);
				}
			}

			return before + block + after;
		}

		/// <summary>
		/// Converts a Unity-relative asset path to an absolute file-system path.
		/// </summary>
		public static string AssetPathToFullPath(string assetPath)
			=> Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
	}
}
#endif
