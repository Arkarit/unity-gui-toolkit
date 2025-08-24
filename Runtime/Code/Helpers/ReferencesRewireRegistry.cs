using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Holds serialized reference rewiring information for Text-to-TMP migration.
	/// Automatically created in the current scene or prefab stage during migration.
	/// </summary>
	[ExecuteAlways]
	public class ReferencesRewireRegistry : MonoBehaviour
	{
#if UNITY_EDITOR
		/// <summary>
		/// Name of the hidden GameObject that stores the registry in a scene.
		/// </summary>
		public const string RegistryName = "__ReferencesRewireRegistry__";

		/// <summary>
		/// A single rewiring entry that describes how to update a reference
		/// from an old component (e.g. UnityEngine.UI.Text) to a new one (e.g. TextMeshProUGUI).
		/// </summary>
		[Serializable]
		public class Entry
		{
			/// <summary>
			/// Object (usually a MonoBehaviour) that holds the serialized reference.
			/// </summary>
			public UnityEngine.Object Owner;

			/// <summary>
			/// SerializedProperty path on the Owner object pointing to the old reference.
			/// </summary>
			public string PropertyPath;

			/// <summary>
			/// GameObject that contains the old component and will receive the new one.
			/// </summary>
			public GameObject TargetGameObject;

			/// <summary>
			/// Type of the original (old) component.
			/// </summary>
			public Type OldType;

			/// <summary>
			/// Type of the new component that replaces the old one.
			/// </summary>
			public Type NewType;
		}

		/// <summary>
		/// List of all rewiring entries to be processed.
		/// </summary>
		public List<Entry> Entries = new();

		/// <summary>
		/// Returns the registry in the given scene, or creates a new hidden one if none exists.
		/// </summary>
		/// <param name="_scene">Scene to search or insert the registry into.</param>
		/// <returns>Existing or newly created registry instance.</returns>
		public static ReferencesRewireRegistry GetOrCreate( Scene _scene )
		{
			ReferencesRewireRegistry result = Get(_scene);

			if (result == null)
			{
				var registryGameObject = new GameObject(RegistryName);
				registryGameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
				SceneManager.MoveGameObjectToScene(registryGameObject, _scene);
				result = registryGameObject.AddComponent<ReferencesRewireRegistry>();
				Undo.RegisterCreatedObjectUndo(registryGameObject, "Create TMP Rewire Registry");
			}

			return result;
		}

		/// <summary>
		/// Returns the registry from the given scene, or null if none exists.
		/// </summary>
		/// <param name="_scene">Scene to search for the registry.</param>
		/// <returns>The registry if found, otherwise null.</returns>
		public static ReferencesRewireRegistry Get( Scene _scene )
		{
			foreach (var root in _scene.GetRootGameObjects())
			{
				if (root.name == RegistryName)
				{
					return root.GetComponent<ReferencesRewireRegistry>();
				}
			}

			return null;
		}

		/// <summary>
		/// Returns true if the scene contains a registry with rewiring entries.
		/// </summary>
		/// <param name="_scene">Scene to check.</param>
		/// <returns>True if registry exists and has entries, otherwise false.</returns>
		public static bool HasRegistryWithEntries( Scene _scene )
		{
			var instance = Get(_scene);
			return instance != null && instance.Entries != null && instance.Entries.Count > 0;
		}

		/// <summary>
		/// Attempts to retrieve a registry with entries from the given scene.
		/// </summary>
		/// <param name="_scene">Scene to search for the registry.</param>
		/// <param name="_reg">Output parameter receiving the registry if found.</param>
		/// <returns>True if found and contains entries, false otherwise.</returns>
		public static bool TryGetRegistryWithEntries( Scene _scene, out ReferencesRewireRegistry _reg )
		{
			_reg = Get(_scene);
			return _reg != null && _reg.Entries != null && _reg.Entries.Count > 0;
		}
#else
        /// <summary>
        /// Destroys the registry when running in a non-editor context.
        /// </summary>
        private void Awake() => this.gameObject.SafeDestroy();
#endif
	}
}
