using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiTesselator : BaseMeshEffect
	{
		private const float LARGE_SIZE = 1000000;

		public enum EMode
		{
			None,
			Horizontal,
			Vertical,
			Both,
		}

		[SerializeField]
		private EMode m_mode = EMode.Horizontal;

		[SerializeField]
		[Range(5,2000)]
		private float m_sizeHorizontal = 50.0f;

		[SerializeField]
		[Range(5,2000)]
		private float m_sizeVertical = 50.0f;

		public EMode Mode { get { return m_mode; } }

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

			switch( m_mode )
			{
				default:
				case EMode.None:
					break;
				case EMode.Horizontal:
					UiTesselationUtil.Tesselate(_vh, m_sizeHorizontal, LARGE_SIZE);
					break;
				case EMode.Vertical:
					UiTesselationUtil.Tesselate(_vh, LARGE_SIZE, m_sizeVertical);
					break;
				case EMode.Both:
					UiTesselationUtil.Tesselate(_vh, m_sizeHorizontal, m_sizeVertical);
					break;
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiTesselator))]
	public class UiStateMachineEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			UiTesselator thisUiTesselator = (UiTesselator)target;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_mode"));
			serializedObject.ApplyModifiedProperties();

			switch(thisUiTesselator.Mode)
			{
				case UiTesselator.EMode.None:
					break;
				case UiTesselator.EMode.Horizontal:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeHorizontal"), new GUIContent("Size"));
					break;
				case UiTesselator.EMode.Vertical:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeVertical"), new GUIContent("Size"));
					break;
				case UiTesselator.EMode.Both:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeHorizontal"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeVertical"));
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif

}
