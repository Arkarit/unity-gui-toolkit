using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiTessellator : BaseMeshEffectTMP
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
					UiModifierUtil.Tessellate(_vh, m_sizeHorizontal, LARGE_SIZE);
					break;
				case EMode.Vertical:
					UiModifierUtil.Tessellate(_vh, LARGE_SIZE, m_sizeVertical);
					break;
				case EMode.Both:
					UiModifierUtil.Tessellate(_vh, m_sizeHorizontal, m_sizeVertical);
					break;
			}
		}

		protected override bool ChangesTopology { get {return true;} }

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiTessellator))]
	public class UiTessellatorEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			UiTessellator thisUiTesselator = (UiTessellator)target;

			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_mode"));
			serializedObject.ApplyModifiedProperties();

			switch(thisUiTesselator.Mode)
			{
				case UiTessellator.EMode.None:
					break;
				case UiTessellator.EMode.Horizontal:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeHorizontal"), new GUIContent("Size"));
					break;
				case UiTessellator.EMode.Vertical:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeVertical"), new GUIContent("Size"));
					break;
				case UiTessellator.EMode.Both:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeHorizontal"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("m_sizeVertical"));
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif

}
