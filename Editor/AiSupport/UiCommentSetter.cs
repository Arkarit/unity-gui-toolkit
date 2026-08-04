using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Sets (idempotently) a <see cref="UiComment"/> "flavor" description on a prefab root — the note humans
	/// read in the Inspector and, for palette prefabs, the description harvested into the screen-authoring
	/// catalog. Mirrors <see cref="UiStandardElementTagger"/>: a batch is sorted base-before-variant via
	/// <see cref="EditorAssetUtility.SortByPrefabHierarchyAssetPaths"/> so a variant is written only after its
	/// base is final — a variant then stores its own text as an override on the inherited component instead of
	/// a duplicate, and the round-trip stays override-clean.
	///
	/// One engine, two users: the library's own bulk nachrüsten and the MCP set_ui_comment tool a client AI
	/// uses to describe its own prefab variants (so they gain palette descriptions).
	/// </summary>
	public static class UiCommentSetter
	{
		public struct CommentRequest
		{
			/// <summary>Project-relative prefab path.</summary>
			public string PrefabPath;

			/// <summary>The flavor description to store on the root's <see cref="UiComment"/>.</summary>
			public string Comment;
		}

		public struct CommentResult
		{
			public string PrefabPath;
			public bool Ok;
			public string Message;
		}

		/// <summary>Sets the root comment on each prefab, base-before-variant. One result per prefab.</summary>
		public static List<CommentResult> Set( IReadOnlyList<CommentRequest> _requests )
		{
			var results = new List<CommentResult>();
			if (_requests == null || _requests.Count == 0)
				return results;

			// Deduplicate by path (last wins), then order base-before-variant.
			var byPath = new Dictionary<string, CommentRequest>();
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
				UiLog.LogWarning($"[UiComment] Could not sort by prefab hierarchy ({e.Message}); writing in request order.");
			}

			foreach (var path in orderedPaths)
				results.Add(Apply(byPath[path]));

			AssetDatabase.SaveAssets();
			return results;
		}

		private static CommentResult Apply( CommentRequest _request )
		{
			var result = new CommentResult { PrefabPath = _request.PrefabPath };

			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(_request.PrefabPath);
			if (asset == null)
			{
				result.Message = "No prefab at path.";
				UiLog.LogError($"[UiComment] {_request.PrefabPath}: {result.Message}");
				return result;
			}

			GameObject root = PrefabUtility.LoadPrefabContents(_request.PrefabPath);
			try
			{
				var comment = root.GetComponent<UiComment>();
				if (comment == null)
					comment = root.AddComponent<UiComment>();

				var so = new SerializedObject(comment);
				var prop = so.FindProperty("m_comment");
				if (prop == null)
				{
					result.Message = "UiComment has no serialized 'm_comment' (editor-only field missing?).";
					UiLog.LogError($"[UiComment] {_request.PrefabPath}: {result.Message}");
					return result;
				}
				prop.stringValue = _request.Comment ?? "";
				so.ApplyModifiedPropertiesWithoutUndo();

				PrefabUtility.SaveAsPrefabAsset(root, _request.PrefabPath, out bool success);
				result.Ok = success;
				result.Message = success ? "Comment set." : "SaveAsPrefabAsset reported failure.";
				if (!success)
					UiLog.LogError($"[UiComment] {_request.PrefabPath}: {result.Message}");
			}
			catch (Exception e)
			{
				result.Ok = false;
				result.Message = e.Message;
				UiLog.LogError($"[UiComment] {_request.PrefabPath}: {e}");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}

			return result;
		}
	}
}
