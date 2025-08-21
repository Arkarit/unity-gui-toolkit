using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Editor
{
    [ExecuteAlways]
    public class UITextTMP_RewireRegistry : MonoBehaviour
    {
#if UNITY_EDITOR
		[Serializable]
		public class Entry
		{
			public UnityEngine.Object owner;   // Component in the same scene or prefab stage
			public string propertyPath;        // SerializedProperty path on owner
			public GameObject targetGO;        // GO that had old Text (will hold TMP)
		}

		public List<Entry> entries = new List<Entry>();
#else
        private void Awake() => this.gameObject.SafeDestroy();
#endif
    }
}