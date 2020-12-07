using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiRadialLayoutGroup : LayoutGroup
	{
		[SerializeField]
		private float m_radius;

		[SerializeField]
		private float m_zIncrement = 0;

		[SerializeField]
		private bool m_rotateElements = false;

		[SerializeField]
		[Range(-360f, 360f)]
		private float m_rotationAngleOffset;

		[SerializeField]
		[Range(0f, 360f)]
		private float m_minAngle;

		[SerializeField]
		[Range(0f, 360f)]
		private float m_maxAngle;

		[SerializeField]
		[Range(0f, 360f)]
		private float m_startAngle;

		[SerializeField]
		[HideInInspector]
		private bool m_useZIncrement;

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

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			m_useZIncrement = !Mathf.Approximately(m_zIncrement, 0);
			CalculateRadial();
		}
#endif

		private void CalculateRadial()
		{
			m_Tracker.Clear();
			if (transform.childCount == 0)
				return;

			float offsetAngle = ((m_maxAngle - m_minAngle)) / (transform.childCount - 1);
			float angle = m_startAngle;

			int childCount = transform.childCount;
			float z = 0;
			if (m_useZIncrement && childCount > 1)
			{
				z = - m_zIncrement * childCount / 2;
			}

			for (int i = 0; i < transform.childCount; i++)
			{
				RectTransform child = (RectTransform)transform.GetChild(i);
				if (child != null)
				{
					m_Tracker.Add( this, child, GetDrivenTransformProperties() );
					Vector3 vPos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
					child.localPosition = vPos * m_radius;

					if (m_useZIncrement)
					{
						child.localPosition += new Vector3(0,0, z);
						z += m_zIncrement;
					}

					if (m_rotateElements)
					{
						child.localRotation = Quaternion.AngleAxis(angle + m_rotationAngleOffset, Vector3.forward);
					}
					angle += offsetAngle;
				}
			}
		}

		private DrivenTransformProperties GetDrivenTransformProperties()
		{
			DrivenTransformProperties result =
				  DrivenTransformProperties.Anchors
				| DrivenTransformProperties.AnchoredPosition
				| DrivenTransformProperties.Pivot;

			if (m_useZIncrement)
				result |= DrivenTransformProperties.AnchoredPositionZ;

			if (m_rotateElements)
				result |= DrivenTransformProperties.Rotation;

			return result;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiRadialLayoutGroup))]
	public class UiRadialLayoutGroupEditor : Editor
	{
		protected SerializedProperty m_radiusProp;
		protected SerializedProperty m_zIncrementProp;
		protected SerializedProperty m_rotateElementsProp;
		protected SerializedProperty m_rotationAngleOffsetProp;

		static private bool m_toolsVisible;

		public virtual void OnEnable()
		{
			m_radiusProp = serializedObject.FindProperty("m_radius");
			m_zIncrementProp = serializedObject.FindProperty("m_zIncrement");
			m_rotateElementsProp = serializedObject.FindProperty("m_rotateElements");
			m_rotationAngleOffsetProp = serializedObject.FindProperty("m_rotationAngleOffset");
		}

		public override void OnInspectorGUI()
		{
			UiRadialLayoutGroup thisUiRadialLayoutGroup = (UiRadialLayoutGroup)target;

			EditorGUILayout.PropertyField(m_radiusProp);
			EditorGUILayout.PropertyField(m_zIncrementProp);
			EditorGUILayout.PropertyField(m_rotateElementsProp);
			EditorGUILayout.PropertyField(m_rotationAngleOffsetProp);

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}