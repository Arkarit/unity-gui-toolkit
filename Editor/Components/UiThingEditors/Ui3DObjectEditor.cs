using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// \addtogroup Editor Code
	/// Ui3DObjectEditor can have several circumstances, under which it is technically impossible
	/// to work. This editor's purpose is to show some warning if these issues occur.
	[CustomEditor(typeof(Ui3DObject))]
	public class Ui3DObjectEditor : UiThingEditor
	{
		protected SerializedProperty m_zSizeProp;
		protected SerializedProperty m_zSizeFactorProp;

		private static readonly HashSet<string> m_excludedProperties = new()
		{
			"m_zSize",
			"m_zSizeFactor"
		};

		protected override HashSet<string> excludedProperties => m_excludedProperties;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_zSizeProp = serializedObject.FindProperty("m_zSize");
			m_zSizeFactorProp = serializedObject.FindProperty("m_zSizeFactor");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			Ui3DObject thisUi3DObject = (Ui3DObject)target;
			GameObject go = thisUi3DObject.gameObject;
			MaterialCloner materialCloner = go.GetComponent<MaterialCloner>();

			if (materialCloner == null)
				return;

			if (EditorGameObjectUtility.InfoBoxIfPrefab(go))
				return;

			bool error = false;
			Material material = materialCloner.ClonedMaterial;

			if (!material.HasProperty(Ui3DObject.s_propOffset) || !material.HasProperty(Ui3DObject.s_propScale))
			{
				error = true;
				EditorGUILayout.HelpBox("Ui3DObject needs a material with _Offset and _Scale property (for scaling the mesh) support to work.\n" + 
					"You can assign UI_3D.mat (which supports this feature) to the MaterialCloner on this game object.\n" + 
					"Or, you can examine Ui3D.shader how it's done. ", MessageType.Warning);
			}

			if (materialCloner.IsSharedMaterial)
			{
				if (!material.enableInstancing)
				{
					error = true;
					EditorGUILayout.HelpBox("If 'Share Material between instances' is selected in the MaterialCloner script on this game object," + 
						"Ui3DObject needs a material, which has 'GPU Instancing' enabled. Otherwise scaling will not work properly.", MessageType.Warning);
				}
			}

			if (error)
				return;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_zSizeProp);
			EditorGUILayout.PropertyField(m_zSizeFactorProp);

			if (EditorGUI.EndChangeCheck())
			{
				thisUi3DObject.SetDirty();
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}