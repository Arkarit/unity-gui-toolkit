#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiFFD : UiDistort
	{
		[SerializeField]
		[Range(3,6)]
		protected int m_pointsHorizontal = 3;

		[SerializeField]
		[Range(3,6)]
		protected int m_pointsVertical = 3;

		[SerializeField]
		[Range(0,1)]
		protected float m_subPointsStrength = 0.5f;

		[SerializeField]
		protected Vector2[] m_points;
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiFFD))]
	public class UiFFDEditor : UiDistortEditor
	{
		protected override bool HasMirror { get { return false; } }

		protected SerializedProperty m_pointsHorizontalProp;
		protected SerializedProperty m_pointsVerticalProp;
		protected SerializedProperty m_subPointsStrengthProp;
		protected SerializedProperty m_pointsProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_pointsHorizontalProp = serializedObject.FindProperty("m_pointsHorizontal");
			m_pointsVerticalProp = serializedObject.FindProperty("m_pointsVertical");
			m_subPointsStrengthProp = serializedObject.FindProperty("m_subPointsStrength");
			m_pointsProp = serializedObject.FindProperty("m_points");
		}

		protected override void Edit( UiDistortBase _thisUiDistortBase )
		{

		}
		protected override void Edit2( UiDistortBase thisUiDistort )
		{
		}
	}
#endif
}
