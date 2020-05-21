using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class CreateTestUiObjects : MonoBehaviour
	{
		public TestUiObject m_prefab;
		public int m_numInstances;

#if UNITY_EDITOR
#if TEST_UI
		public void Spawn()
		{
			for (int i=0; i<m_numInstances; i++)
			{
				GameObject go = PrefabUtility.InstantiatePrefab(m_prefab.gameObject) as GameObject;
				TestUiObject obj = go.GetComponent<TestUiObject>();
				obj.transform.SetParent(transform, false);
				go.name = go.name + "_" + i.ToString("D2");
				obj.Init(i, m_numInstances);
			}
		}
#endif
#endif
	}

#if UNITY_EDITOR
#if TEST_UI
	[CustomEditor(typeof(CreateTestUiObjects))]
	public class CreateTestUiObjectsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			if (GUILayout.Button("Create"))
			{
				CreateTestUiObjects thisCreateTestUiObjects = target as CreateTestUiObjects;
				thisCreateTestUiObjects.Spawn();
			}
		}
	}
#endif
#endif

}
