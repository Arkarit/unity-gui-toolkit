using System.Collections.Generic;
using System;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[ExecuteAlways]
	internal class UITextTMP_RewireRegistry : MonoBehaviour
	{
		[Serializable]
		internal class Entry
		{
			public UnityEngine.Object owner;   // Component in the same scene or prefab stage
			public string propertyPath;        // SerializedProperty path on owner
			public GameObject targetGO;        // GO that had old Text (will hold TMP)
		}

		public List<Entry> entries = new List<Entry>();
	}
}
