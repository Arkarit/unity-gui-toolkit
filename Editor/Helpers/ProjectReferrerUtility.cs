using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Utilities for finding and rewiring references to Unity Objects across the project.
	/// Uses EditorAssetUtility for consistent asset iteration.
	/// </summary>
	public static class ProjectReferrerUtility
	{
		/// <summary>
		/// Collects all references to a target object across ScriptableObjects and Prefabs in the project.
		/// </summary>
		/// <param name="_target">The Unity Object to find references to.</param>
		/// <returns>List of (owner, propertyPath) tuples pointing to the target.</returns>
		public static List<(UnityEngine.Object owner, string propertyPath)> CollectReferrersInProject( UnityEngine.Object _target )
		{
			var result = new List<(UnityEngine.Object owner, string propertyPath)>();

			if (!_target)
				return result;

			CollectReferrersInAllScriptableObjects(_target, result);
			CollectReferrersInAllPrefabs(_target, result);

			// Optional (expensive / intrusive): scenes on disk
			// Uncommenting this will scan ALL scenes in the project, which loads/unloads them
			// CollectReferrersInAllScenesOnDisk(_target, result);

			return result;
		}

		/// <summary>
		/// Rewires all collected references to point to a new target object.
		/// </summary>
		/// <param name="_referrers">List of (owner, propertyPath) collected earlier.</param>
		/// <param name="_newTarget">The new Unity Object to rewire references to.</param>
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

		/// <summary>
		/// Collects references in all ScriptableObject assets.
		/// </summary>
		private static void CollectReferrersInAllScriptableObjects
		(
			UnityEngine.Object _target,
			List<(UnityEngine.Object owner, string propertyPath)> _out
		)
		{
			EditorAssetUtility.FindAllScriptableObjects<ScriptableObject>(soAsset =>
			{
				if (!soAsset)
					return;

				ScanSerializedObjectForTarget(soAsset, _target, _out);
			});
		}

		/// <summary>
		/// Collects references in all Prefab assets by loading their contents.
		/// </summary>
		private static void CollectReferrersInAllPrefabs
		(
			UnityEngine.Object _target,
			List<(UnityEngine.Object owner, string propertyPath)> _out
		)
		{
			EditorAssetUtility.FindAllComponentsInAllPrefabs<Component>(( comp, prefabAsset, assetPath ) =>
			{
				if (!comp)
					return true;

				ScanSerializedObjectForTarget(comp, _target, _out);
				return true;
			}, new EditorAssetUtility.AssetSearchOptions { ShowProgressBar = false });
		}

		/// <summary>
		/// Optional: Collects references in all Scene assets (expensive - loads each scene).
		/// Uncomment the call in CollectReferrersInProject if needed.
		/// </summary>
		private static void CollectReferrersInAllScenesOnDisk
		(
			UnityEngine.Object _target,
			List<(UnityEngine.Object owner, string propertyPath)> _out
		)
		{
			EditorAssetUtility.FindAllComponentsInAllScenes<Component>(( comp, scene, scenePath ) =>
			{
				if (!comp)
					return true;

				ScanSerializedObjectForTarget(comp, _target, _out);
				return true;
			}, new EditorAssetUtility.AssetSearchOptions { ShowProgressBar = false });
		}

		/// <summary>
		/// Scans all serialized properties of an owner object to find references to the target.
		/// </summary>
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
