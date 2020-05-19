using System;
using UnityEngine;
using UnityEngine.Serialization;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Layout
{
	public enum SizePolicy
	{
		Fixed,
		Flexible,
		Master,
	}

	public enum AlignmentPolicy
	{
		Minimum,
		Center,
		Maximum,
	}

	[Serializable]
	public class UiLayoutElementTransformPolicy
	{
		public const int Unlimited = -1;

		[SerializeField]
		private float m_minimumSize;
		[SerializeField]
		[FormerlySerializedAs("m_sizeA")]
		private float m_preferredSize;
		[SerializeField]
		private float m_maximumSize;
		[SerializeField]
		private SizePolicy m_sizePolicy;
		[SerializeField]
		private AlignmentPolicy m_alignmentPolicy = AlignmentPolicy.Center;

		public float GetPreferredSize()
		{
			return m_preferredSize;
		}
	}

	[CustomPropertyDrawer(typeof(UiLayoutElementTransformPolicy))]
	public class UiLayoutElementTransformPolicyDrawer : PropertyDrawer
	{
		static readonly float s_lineHeight = EditorGUIUtility.singleLineHeight;

		private enum HorizontalAlignment
		{
			Left,
			Center,
			Right,
		}

		private enum VerticalAlignment
		{
			Top,
			Middle,
			Bottom,
		}

		private bool m_isHorizontal;
		private SerializedProperty m_sizePolicyProp;
		private SerializedProperty m_alignmentPolicyProp;
		private SerializedProperty m_minimumSizeProp;
		private SerializedProperty m_preferredSizeProp;
		private SerializedProperty m_maximumSizeProp;

		private bool ShowAlignmentPolicy
		{
			get
			{
				SizePolicy sizePolicy = (SizePolicy)(object) m_sizePolicyProp.intValue;
				return sizePolicy == SizePolicy.Fixed;
			}
		}

		private bool ShowMinAndMax
		{
			get
			{
				SizePolicy sizePolicy = (SizePolicy)(object) m_sizePolicyProp.intValue;
				return sizePolicy == SizePolicy.Flexible;
			}
		}

		public void InitProps(SerializedProperty _property)
		{
			m_isHorizontal = _property.name == "m_width";
			Debug.Assert(m_isHorizontal || _property.name == "m_height", "Please name your UiLayoutElementTransformPolicy member either m_width or m_height");

			m_sizePolicyProp = _property.FindPropertyRelative("m_sizePolicy");
			m_alignmentPolicyProp = _property.FindPropertyRelative("m_alignmentPolicy");
			m_minimumSizeProp = _property.FindPropertyRelative("m_minimumSize");
			m_preferredSizeProp = _property.FindPropertyRelative("m_preferredSize");
			m_maximumSizeProp = _property.FindPropertyRelative("m_maximumSize");
		}

		public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
		{
			InitProps(_property);
			_position.height = s_lineHeight;

			EditorGUI.PropertyField(_position, m_sizePolicyProp, new GUIContent( m_isHorizontal ? "Width Policy" : "Height Policy"));

			if (ShowAlignmentPolicy)
			{
				_position.y += s_lineHeight;

				AlignmentPolicy alignmentPolicy = (AlignmentPolicy) (object) m_alignmentPolicyProp.intValue;
				if (m_isHorizontal)
				{
					HorizontalAlignment alignment = (HorizontalAlignment) alignmentPolicy;
					alignmentPolicy = (AlignmentPolicy)  EditorGUI.EnumPopup(_position, new GUIContent("Horizontal Alignment"), alignment);
				}
				else
				{
					VerticalAlignment alignment = (VerticalAlignment) alignmentPolicy;
					alignmentPolicy = (AlignmentPolicy)  EditorGUI.EnumPopup(_position, new GUIContent("Vertical Alignment"), alignment);
				}

				m_alignmentPolicyProp.intValue = (int)(object) alignmentPolicy;
			}

			_position.y += s_lineHeight;
			if (ShowAlignmentPolicy)
				EditorGUI.PropertyField(_position, m_minimumSizeProp, new GUIContent( m_isHorizontal ? "Width" : "Height"));
			else
				EditorGUI.PropertyField(_position, m_minimumSizeProp, new GUIContent( m_isHorizontal ? "Preferred Width" : "Preferred Height"));

			if (ShowMinAndMax)
			{
				_position.y += s_lineHeight;
				EditorGUI.PropertyField(_position, m_minimumSizeProp, new GUIContent( m_isHorizontal ? "Min Width" : "Min Height"));
				_position.y += s_lineHeight;
				EditorGUI.PropertyField(_position, m_maximumSizeProp, new GUIContent( m_isHorizontal ? "Max Width" : "Min Height"));
			}
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
		{
			InitProps(_property);
			return 0
				+ s_lineHeight // Size Policy
				+ s_lineHeight * (ShowAlignmentPolicy ? 1 : 0) // Alignment policy
				+ s_lineHeight * (ShowMinAndMax ? 3 : 1) // sizes
				+ s_lineHeight / 2.0f // Space at end
				;
		}

	}

}