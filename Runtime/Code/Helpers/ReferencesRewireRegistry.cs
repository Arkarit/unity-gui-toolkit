using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Editor
{
    [ExecuteAlways]
    public class ReferencesRewireRegistry : MonoBehaviour
    {
#if UNITY_EDITOR
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
#else
        private void Awake() => this.gameObject.SafeDestroy();
#endif
    }
}