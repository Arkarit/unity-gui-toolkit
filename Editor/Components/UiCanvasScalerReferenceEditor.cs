using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiCanvasScalerReference))]
	public class UiCanvasScalerReferenceEditor : CanvasScalerEditor
	{
		private SerializedProperty m_referenceProp;

		protected override void OnEnable()
		{
			m_referenceProp = serializedObject.FindProperty("m_reference");
			base.OnEnable();
		}

		private void SetProp( SerializedObject otherSerializedObject, ref SerializedProperty thisProp,
			ref SerializedProperty otherProp, string propName )
		{
			thisProp = serializedObject.FindProperty(propName);
			if (otherSerializedObject != null)
				otherProp = otherSerializedObject.FindProperty(propName);
		}

		public override void OnInspectorGUI()
		{
			UiCanvasScalerReference thisUiCSR = (UiCanvasScalerReference)target;
			bool isReferenceSet = thisUiCSR.Reference != null;

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_referenceProp);
			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space(10);

			if (isReferenceSet)
			{
				thisUiCSR.Reference.CopyTo((CanvasScaler)thisUiCSR);
				serializedObject.Update();
			}
			else
			{
				EditorGUILayout.HelpBox(
					"This component is intended to reference a\n" +
					"CanvasScaler prefab to have similar canvas scaler settings in all prefabs.\n" +
					"If no reference is set, it behaves like an common CanvasScaler."
					, MessageType.Warning);
			}

			EditorGUI.BeginDisabledGroup(isReferenceSet);
			base.OnInspectorGUI();
			EditorGUI.EndDisabledGroup();
		}
	}
}