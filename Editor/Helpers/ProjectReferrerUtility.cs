using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class ProjectReferrerUtility
	{
		public static List<(UnityEngine.Object owner, string propertyPath)> CollectReferrersInProject( UnityEngine.Object _target )
		{
			var result = new List<(UnityEngine.Object owner, string propertyPath)>();

			if (!_target)
				return result;

			CollectReferrersInAllScriptableObjects(_target, result);
			CollectReferrersInAllPrefabs(_target, result);

			// Optional (expensive / intrusive): scenes on disk
			//CollectReferrersInAllScenesOnDisk(_target, result);

			return result;
		}

		public static void RewireReferrers
		(
			List<(UnityEngine.Object owner, string propertyPath)> _referrers,
			UnityEngine.Object _newTarget
		)
		{
			if (_referrers == null || _referrers.Count == 0)
				return;

			for (int i = 0; i < _referrers.Count; i++)
			{
				var (owner, path) = _referrers[i];
				if (!owner)
					continue;

				var so = new SerializedObject(owner);
				var sp = so.FindProperty(path);
				if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (sp.objectReferenceValue != _newTarget)
				{
					Undo.RecordObject(owner, "Rewire component reference");
					sp.objectReferenceValue = _newTarget;
					so.ApplyModifiedProperties();
					EditorUtility.SetDirty(owner);
				}
			}
		}

		private static void CollectReferrersInAllScriptableObjects( UnityEngine.Object _target, List<(UnityEngine.Object owner, string propertyPath)> _out )
		{
			var guids = AssetDatabase.FindAssets("t:ScriptableObject");

			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				var soAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
				if (!soAsset)
					continue;

				ScanSerializedObjectForTarget(soAsset, _target, _out);
			}
		}

		private static void CollectReferrersInAllPrefabs( UnityEngine.Object _target, List<(UnityEngine.Object owner, string propertyPath)> _out )
		{
			var guids = AssetDatabase.FindAssets("t:Prefab");

			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				if (string.IsNullOrEmpty(path))
					continue;

				// Load a temporary editable prefab instance to get real Component owners.
				var root = PrefabUtility.LoadPrefabContents(path);
				if (!root)
					continue;

				try
				{
					var comps = root.GetComponentsInChildren<Component>(true);
					for (int c = 0; c < comps.Length; c++)
					{
						var owner = comps[c];
						if (!owner)
							continue;

						ScanSerializedObjectForTarget(owner, _target, _out);
					}
				}
				finally
				{
					PrefabUtility.UnloadPrefabContents(root);
				}
			}
		}

		private static void ScanSerializedObjectForTarget
		(
			UnityEngine.Object _owner,
			UnityEngine.Object _target,
			List<(UnityEngine.Object owner, string propertyPath)> _out
		)
		{
			var so = new SerializedObject(_owner);
			var it = so.GetIterator();
			bool enterChildren = true;

			while (it.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (it.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (it.objectReferenceValue != _target)
					continue;

				_out.Add((_owner, it.propertyPath));
			}
		}

	}
}
