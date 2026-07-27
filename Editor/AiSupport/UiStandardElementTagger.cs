using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Tags prefab roots with a <see cref="UiStandardElement"/> marker (the single source of truth for
	/// standard-element identity). Safe for batches that mix base prefabs and their variants: the batch is
	/// sorted base-before-variant via <see cref="EditorAssetUtility.SortByPrefabHierarchyAssetPaths"/> so a
	/// variant is only ever written after its base is already in its final tagged state (avoiding the
	/// override corruption that an unordered prefab round-trip causes).
	///
	/// The same engine powers both the library's own bulk tagging and the MCP tag tool a client AI drives
	/// when it authors and tags its own prefabs/variants.
	/// </summary>
	public static class UiStandardElementTagger
	{
		public struct TagRequest
		{
			/// <summary>Project-relative prefab path, e.g. "Assets/.../OkButton.prefab".</summary>
			public string PrefabPath;

			/// <summary>An <see cref="EStandardElement"/> name (toolkit built-in) or any custom id (client element).</summary>
			public string Key;

			/// <summary>Internal sub-part: still gets an identity, but hidden from the authoring vocabulary.</summary>
			public bool Internal;
		}

		public struct TagResult
		{
			public string PrefabPath;
			public string ResolvedKey;
			public bool Ok;
			public string Message;
		}

		/// <summary>
		/// Sets (idempotently) the marker on each requested prefab. Existing markers are updated in place.
		/// Returns one result per request, in the order they were processed (base before variant).
		/// </summary>
		public static List<TagResult> Tag( IReadOnlyList<TagRequest> _requests )
		{
			var results = new List<TagResult>();
			if (_requests == null || _requests.Count == 0)
				return results;

			// Deduplicate by path (last request wins) and order base-before-variant.
			var byPath = new Dictionary<string, TagRequest>();
			foreach (var r in _requests)
			{
				if (!string.IsNullOrEmpty(r.PrefabPath))
					byPath[r.PrefabPath] = r;
			}

			var orderedPaths = byPath.Keys.ToList();
			try
			{
				EditorAssetUtility.SortByPrefabHierarchyAssetPaths(orderedPaths);
			}
			catch (Exception e)
			{
				// A path that is not a prefab throws; fall back to the unsorted order and let each
				// per-prefab load surface the concrete error.
				UiLog.LogWarning($"[Tagger] Could not sort by prefab hierarchy ({e.Message}); tagging in request order.");
			}

			foreach (var path in orderedPaths)
				results.Add(Apply(byPath[path], remove: false));

			AssetDatabase.SaveAssets();
			return results;
		}

		/// <summary>Removes the marker from each prefab (no-op where none is present).</summary>
		public static List<TagResult> Untag( IReadOnlyList<string> _prefabPaths )
		{
			var results = new List<TagResult>();
			if (_prefabPaths == null)
				return results;

			foreach (var path in _prefabPaths.Where(p => !string.IsNullOrEmpty(p)).Distinct())
				results.Add(Apply(new TagRequest { PrefabPath = path }, remove: true));

			AssetDatabase.SaveAssets();
			return results;
		}

		private static TagResult Apply( TagRequest _request, bool remove )
		{
			var result = new TagResult { PrefabPath = _request.PrefabPath };

			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(_request.PrefabPath);
			if (asset == null)
			{
				result.Message = "No prefab at path.";
				UiLog.LogError($"[Tagger] {_request.PrefabPath}: {result.Message}");
				return result;
			}

			GameObject root = PrefabUtility.LoadPrefabContents(_request.PrefabPath);
			try
			{
				var marker = root.GetComponent<UiStandardElement>();

				if (remove)
				{
					if (marker != null)
						UnityEngine.Object.DestroyImmediate(marker, true);
					result.Ok = true;
					result.Message = marker != null ? "Marker removed." : "No marker present.";
				}
				else
				{
					if (marker == null)
						marker = root.AddComponent<UiStandardElement>();

					ResolveKey(_request.Key, out var element, out var customId);

					var so = new SerializedObject(marker);
					var elementProp = so.FindProperty("m_element");
					int idx = Array.IndexOf(elementProp.enumNames, element.ToString());
					elementProp.enumValueIndex = idx >= 0 ? idx : 0;
					so.FindProperty("m_customId").stringValue = customId;
					so.FindProperty("m_internal").boolValue = _request.Internal;
					so.ApplyModifiedPropertiesWithoutUndo();

					result.ResolvedKey = element == EStandardElement.Custom ? customId : element.ToString();
					result.Ok = true;
					result.Message = $"Tagged as {result.ResolvedKey}{(_request.Internal ? " (internal)" : "")}.";
				}

				PrefabUtility.SaveAsPrefabAsset(root, _request.PrefabPath, out bool success);
				if (!success)
				{
					result.Ok = false;
					result.Message = "SaveAsPrefabAsset reported failure.";
					UiLog.LogError($"[Tagger] {_request.PrefabPath}: {result.Message}");
				}
			}
			catch (Exception e)
			{
				result.Ok = false;
				result.Message = e.Message;
				UiLog.LogError($"[Tagger] {_request.PrefabPath}: {e}");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}

			return result;
		}

		/// <summary>
		/// A key that names an <see cref="EStandardElement"/> built-in resolves to that enum value; anything
		/// else (including an empty key) becomes a Custom element carrying the key as its custom id.
		/// </summary>
		private static void ResolveKey( string _key, out EStandardElement _element, out string _customId )
		{
			if (!string.IsNullOrEmpty(_key)
				&& Enum.TryParse<EStandardElement>(_key, false, out var parsed)
				&& parsed != EStandardElement.None
				&& parsed != EStandardElement.Custom)
			{
				_element = parsed;
				_customId = "";
				return;
			}

			_element = EStandardElement.Custom;
			_customId = _key ?? "";
		}

		[MenuItem(StringConstants.AI_TAG_STANDARD_ELEMENTS_TEST_MENU_NAME)]
		private static void TagTest()
		{
			// A minimal base + variant pair (both already valid enum values) to verify the round-trip is
			// diff-clean and the variant is written as an override after its base. Paths are resolved by
			// search so this works regardless of where the toolkit is mounted. Inspect the git diff of the
			// two prefabs afterwards.
			var requests = new List<TagRequest>();
			AddIfFound(requests, "OkButton", EStandardElement.OkButton);
			AddIfFound(requests, "StandardButton", EStandardElement.StandardButton);

			if (requests.Count == 0)
			{
				UiLog.LogError("[Tagger] Test aborted: could not locate the toolkit's StandardButton/OkButton prefabs.");
				return;
			}

			var results = Tag(requests);
			foreach (var r in results)
				UiLog.Log($"[Tagger] {r.PrefabPath} -> Ok={r.Ok}: {r.Message}");
		}

		private static void AddIfFound( List<TagRequest> _requests, string _prefabName, EStandardElement _element )
		{
			string suffix = $"/StandardElements/Buttons/{_prefabName}.prefab";
			foreach (var guid in AssetDatabase.FindAssets($"{_prefabName} t:Prefab"))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
				if (path.EndsWith(suffix))
				{
					_requests.Add(new TagRequest { PrefabPath = path, Key = _element.ToString() });
					return;
				}
			}
			UiLog.LogWarning($"[Tagger] Test: could not find '{_prefabName}' under .../StandardElements/Buttons/.");
		}
	}
}
