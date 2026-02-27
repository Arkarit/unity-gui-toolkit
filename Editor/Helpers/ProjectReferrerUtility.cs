using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Utilities for finding and rewiring references to Unity Objects across the project.
	/// Uses EditorAssetUtility for consistent asset iteration.
	/// </summary>
	public static class ProjectReferrerUtility
	{
		/// <summary>
		/// Collects all references to a target object in the current scene/prefab context.
		/// This is faster than CollectReferrersInProject as it only scans the active context.
		/// </summary>
		/// <param name="_target">The Unity Object to find references to.</param>
		/// <returns>List of (owner, propertyPath) tuples pointing to the target.</returns>
		public static List<(UnityEngine.Object owner, string propertyPath)> CollectReferrersInCurrentContext( UnityEngine.Object _target )
		{
			var result = new List<(UnityEngine.Object owner, string propertyPath)>();

			if (!_target)
				return result;

			var scene = SceneManager.GetActiveScene();

			// Force serialization if scene has unsaved changes
			if (scene.IsValid() && scene.isDirty)
			{
				// Save assets to ensure all SerializedObject data is up-to-date
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			}

			var allComponents = EditorAssetUtility.FindObjectsInCurrentEditedPrefabOrScene<MonoBehaviour>();
			if (allComponents == null || allComponents.Length == 0)
				return result;

			foreach (var comp in allComponents)
			{
				if (!comp)
					continue;

				ScanSerializedObjectForTarget(comp, _target, result);
			}

			return result;
		}

		/// <summary>
		/// Collects all references to a target object across the current context, ScriptableObjects and Prefabs in the project.
		/// This is more comprehensive but slower than CollectReferrersInCurrentContext.
		/// </summary>
		/// <param name="_target">The Unity Object to find references to.</param>
		/// <returns>List of (owner, propertyPath) tuples pointing to the target.</returns>
		public static List<(UnityEngine.Object owner, string propertyPath)> CollectReferrersInProject( UnityEngine.Object _target )
		{
			var result = new List<(UnityEngine.Object owner, string propertyPath)>();

			if (!_target)
				return result;

			UiLog.LogInternal($"CollectReferrersInProject for {_target.GetType().Name}: saving active scenes");
			EditorSceneManager.SaveOpenScenes();
			AssetDatabase.SaveAssets();

			// Scan current context first (most common case for component replacement)
			UiLog.LogInternal($"CollectReferrersInProject for {_target.GetType().Name}: scan current context");
			var contextRefs = CollectReferrersInCurrentContext(_target);
			if (contextRefs != null && contextRefs.Count > 0)
				result.AddRange(contextRefs);

			UiLog.LogInternal($"CollectReferrersInProject for {_target.GetType().Name}: scan scriptable objects");
			CollectReferrersInAllScriptableObjects(_target, result);
			UiLog.LogInternal($"CollectReferrersInProject for {_target.GetType().Name}: scan prefabs");
			CollectReferrersInAllPrefabs(_target, result);
			UiLog.LogInternal($"CollectReferrersInProject for {_target.GetType().Name}: scan scenes");
			CollectReferrersInAllScenesOnDisk(_target, result);
			UiLog.LogInternal($"CollectReferrersInProject for {_target.GetType().Name}: found {result.Count} referrers");

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
				so.Update();

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
				UiLog.LogInternal($"Scanning prefab for '{comp.GetType().Name}': '{assetPath}'");
				ScanSerializedObjectForTarget(comp, _target, _out);
				return true;
			});
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
			});
		}

		private static void ScanSerializedObjectForTarget(
			UnityEngine.Object _owner,
			UnityEngine.Object _target,
			List<(UnityEngine.Object owner, string propertyPath)> _out )
		{
			if (!_owner || !_target)
				return;

			var so = new SerializedObject(_owner);
			so.Update();

			var it = so.GetIterator();

			while (it.Next(true)) // Include hidden properties
			{

				if (it.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				var refValue = it.objectReferenceValue;
				if (!refValue)
					continue;

				if (ReferenceEquals(refValue, _target))
				{
					_out.Add((_owner, it.propertyPath));
					continue;
				}

				// Check if they are logically the same (e.g., prefab asset vs. instance)
				var refSource = PrefabUtility.GetCorrespondingObjectFromSource(refValue);
				var targetSource = PrefabUtility.GetCorrespondingObjectFromSource(_target);

				var root = PrefabUtility.GetNearestPrefabInstanceRoot(_target);
				if (!root)
					continue;

				bool found = false;
				var components = root.GetComponentsInChildren(_target.GetType());
				foreach (var component in components)
				{
					if (ReferenceEquals(refValue, component))
					{
						found = true;
						break;
					}
				}

				if (!found && (!refSource || !targetSource || !ReferenceEquals(refSource, targetSource)))
					continue;

				_out.Add((_owner, it.propertyPath));
			}
		}
	}
}
