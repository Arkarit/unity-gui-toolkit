using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiRadialLayoutGroup : LayoutGroup
	{
		public enum Mode
		{
			FixedOverallAngle,
			FixedElementAngle,
			PerElementAngle,
		}

		[SerializeField]							protected Mode m_mode;
		[SerializeField][Range(-360f, 360f)]		protected float m_angle0;
		[SerializeField][Range(-360f, 360f)]		protected float m_angle1;
		[SerializeField][Range(-360f, 360f)]		protected float m_angleOffset;
		[SerializeField]							protected float m_radius;
		[SerializeField]							protected float m_zIncrement = 0;
		[SerializeField]							protected float m_xFactor = 1;
		[SerializeField]							protected bool m_rotateElements = false;
		[SerializeField][Range(-360f, 360f)]		protected float m_elementAngleOffset;
		[SerializeField][HideInInspector]			protected bool m_useZIncrement;
		[SerializeField][HideInInspector]			protected bool m_childRotationChanged;

		protected override void OnEnable()
		{
			base.OnEnable();
			CalculateRadial();
		}

		public override void SetLayoutHorizontal() {}
		public override void SetLayoutVertical() {}

		public override void CalculateLayoutInputVertical()
		{
			CalculateRadial();
		}

		public override void CalculateLayoutInputHorizontal()
		{
			CalculateRadial();
		}

		public new void SetDirty()
		{
			base.SetDirty();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			m_useZIncrement = !Mathf.Approximately(m_zIncrement, 0);
			CalculateRadial();
		}
#endif

		private int ChildCount
		{
			get
			{
				int result = 0;
				foreach (Transform child in transform)
				{
					if (child.gameObject.activeSelf)
						result++;
				}
				return result;
			}
		}

		private List<float> ElementAngles
		{
			get
			{
				List <float> result = new List<float>();
				foreach (Transform child in transform)
				{
					if (child.gameObject.activeSelf)
					{
						UiRadialLayoutElement layoutElement = child.GetComponent<UiRadialLayoutElement>();
						result.Add(layoutElement != null ? layoutElement.Angle : 0);
					}
				}
				return result;
			}
		}

		private void CalculateRadial()
		{
			m_Tracker.Clear();

			int childCount = ChildCount;
			if (childCount == 0)
				return;

			float topAngleOffset = 0, angleIncrement = 0;
			List<float> angleIncrements = null;

			switch(m_mode)
			{
				case Mode.FixedOverallAngle:
					topAngleOffset = -m_angle0 - 90;
					angleIncrement = ((m_angle1 - m_angle0)) / (childCount - 1);
					break;
				case Mode.PerElementAngle:
					angleIncrements = ElementAngles;
					float sum = 0;
					for (int i = 0; i<angleIncrements.Count; i++)
						sum += angleIncrements[i];
					topAngleOffset = sum * 0.5f - 90.0f;
					break;
				default:
				case Mode.FixedElementAngle:
					topAngleOffset = m_angle0 * (childCount - 1) * 0.5f - 90.0f;
					angleIncrement = m_angle0;
					break;
			}

			float runningAngle = m_angleOffset - topAngleOffset;

			float z = 0;
			if (m_useZIncrement && childCount > 1)
			{
				z = - m_zIncrement * childCount / 2;
			}

			int angleIncrementsIdx = 0;
			for (int i = 0; i < transform.childCount; i++)
			{
				RectTransform child = (RectTransform)transform.GetChild(i);
				if (!child.gameObject.activeSelf)
					continue;

				if (child != null)
				{
					m_Tracker.Add( this, child, GetDrivenTransformProperties() );

					float angle = runningAngle;
					if (m_mode == Mode.PerElementAngle)
						angle += angleIncrements[angleIncrementsIdx] * 0.5f;

					Vector3 vPos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * m_xFactor, Mathf.Sin(angle * Mathf.Deg2Rad), 0);
					child.localPosition = vPos * m_radius;

					if (m_useZIncrement)
					{
						child.localPosition += new Vector3(0,0, z);
						z += m_zIncrement;
					}

					if (m_rotateElements)
					{
						child.localRotation = Quaternion.AngleAxis(angle + m_elementAngleOffset, Vector3.forward);
					}
					else if (m_childRotationChanged)
					{
						child.localRotation = Quaternion.identity;
					}

					if (m_mode == Mode.PerElementAngle)
						runningAngle += angleIncrements[angleIncrementsIdx++];
					else
						runningAngle += angleIncrement;
				}
			}

			m_childRotationChanged = false;
		}

		private DrivenTransformProperties GetDrivenTransformProperties()
		{
			DrivenTransformProperties result = DrivenTransformProperties.AnchoredPosition;

			if (m_useZIncrement)
				result |= DrivenTransformProperties.AnchoredPositionZ;

			if (m_rotateElements)
				result |= DrivenTransformProperties.Rotation;

			return result;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiRadialLayoutGroup))]
	public class UiRadialLayoutGroupEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_modeProp;
		protected SerializedProperty m_angle0Prop;
		protected SerializedProperty m_angle1Prop;
		protected SerializedProperty m_angleOffsetProp;
		protected SerializedProperty m_radiusProp;
		protected SerializedProperty m_zIncrementProp;
		protected SerializedProperty m_xFactorProp;
		protected SerializedProperty m_rotateElementsProp;
		protected SerializedProperty m_elementAngleOffsetProp;
		protected SerializedProperty m_childRotationChangedProp;

		static private bool m_toolsVisible;

		public virtual void OnEnable()
		{
			m_modeProp = serializedObject.FindProperty("m_mode");
			m_angle0Prop = serializedObject.FindProperty("m_angle0");
			m_angle1Prop = serializedObject.FindProperty("m_angle1");
			m_angleOffsetProp = serializedObject.FindProperty("m_angleOffset");
			m_radiusProp = serializedObject.FindProperty("m_radius");
			m_zIncrementProp = serializedObject.FindProperty("m_zIncrement");
			m_xFactorProp = serializedObject.FindProperty("m_xFactor");
			m_rotateElementsProp = serializedObject.FindProperty("m_rotateElements");
			m_elementAngleOffsetProp = serializedObject.FindProperty("m_elementAngleOffset");
			m_childRotationChangedProp = serializedObject.FindProperty("m_childRotationChanged");
		}

		public override void OnInspectorGUI()
		{
			UiRadialLayoutGroup thisUiRadialLayoutGroup = (UiRadialLayoutGroup)target;

			EditorGUILayout.PropertyField(m_modeProp);
			UiRadialLayoutGroup.Mode mode = (UiRadialLayoutGroup.Mode) m_modeProp.intValue;
			switch( mode )
			{
				case UiRadialLayoutGroup.Mode.FixedOverallAngle:
					EditorGUILayout.PropertyField(m_angle0Prop, new GUIContent("Angle left"));
					EditorGUILayout.PropertyField(m_angle1Prop, new GUIContent("Angle right"));
					break;
				case UiRadialLayoutGroup.Mode.FixedElementAngle:
					EditorGUILayout.PropertyField(m_angle0Prop, new GUIContent("Angle between elements"));
					break;
			}

			EditorGUILayout.PropertyField(m_angleOffsetProp);
			EditorGUILayout.PropertyField(m_radiusProp);
			EditorGUILayout.PropertyField(m_zIncrementProp);
			EditorGUILayout.PropertyField(m_xFactorProp);
			bool rotateElementsBefore = m_rotateElementsProp.boolValue;
			EditorGUILayout.PropertyField(m_rotateElementsProp);
			bool rotateElementsAfter = m_rotateElementsProp.boolValue;
			m_childRotationChangedProp.boolValue = rotateElementsBefore != rotateElementsAfter;

			EditorGUILayout.PropertyField(m_elementAngleOffsetProp);

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}