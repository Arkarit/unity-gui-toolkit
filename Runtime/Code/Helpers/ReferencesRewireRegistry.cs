using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
    [ExecuteAlways]
    public class ReferencesRewireRegistry : MonoBehaviour
    {
#if UNITY_EDITOR
	    public const string RegistryName = "__ReferencesRewireRegistry__";
	    
		[Serializable]
		public class Entry
		{
			public UnityEngine.Object Owner;		// Component in the same scene or prefab stage
			public string PropertyPath;				// SerializedProperty path on owner
			public GameObject TargetGameObject;		// GO that had old type (will hold new type)
			public Type OldType;
			public Type NewType;
		}

		public List<Entry> Entries = new ();
		
		// Finds or creates (hidden) a registry GameObject in the given scene
		public static ReferencesRewireRegistry GetOrCreate( Scene scene )
		{
			ReferencesRewireRegistry result = Get(scene);
			
			if (result == null)
			{
				var registryGameObject = new GameObject(RegistryName);
				registryGameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
				SceneManager.MoveGameObjectToScene(registryGameObject, scene);
				result = registryGameObject.AddComponent<ReferencesRewireRegistry>();
				Undo.RegisterCreatedObjectUndo(registryGameObject, "Create TMP Rewire Registry");
			}
			
			return result;
		}

		public static ReferencesRewireRegistry Get( Scene scene )
		{
			foreach (var root in scene.GetRootGameObjects())
			{
				if (root.name == RegistryName)
				{
					return root.GetComponent<ReferencesRewireRegistry>();
				}
			}
			
			return null;
		}

#else
        private void Awake() => this.gameObject.SafeDestroy();
#endif
    }
}