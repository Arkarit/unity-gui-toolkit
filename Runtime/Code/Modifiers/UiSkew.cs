using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiSkew : UiDistortBase
	{
		[Range(-89,89)][SerializeField] protected float m_angleHorizontal;
		[Range(-89,89)][SerializeField] protected float m_angleVertical;
		[SerializeField] protected CornerFlags m_usedCorners = CornerFlags.BottomLeft | CornerFlags.BottomRight | CornerFlags.TopLeft | CornerFlags.TopRight;

		private Rect m_lastBounding = Rect.zero;
		private float m_lastAngleHorizontal;
		private float m_lastAngleVertical;

		public virtual Vector2 Angles
		{
			get => new Vector2(m_angleHorizontal, m_angleVertical);
			set
			{
				if (m_angleHorizontal == value.x && m_angleVertical == value.y)
					return;
				
				m_angleHorizontal = value.x;
				m_angleVertical = value.y;
				CalcOffsetsIfNecessary();
				SetDirty();
			}
		}
		
		public override Vector2 TopLeft
		{
			get
			{
				CalcOffsetsIfNecessary();
				return m_topLeft;
			}
			set => throw new ArgumentException($"This setter can not be used. Please use Angles getter/setter instead");
		}
		
		public override Vector2 TopRight
		{
			get
			{
				CalcOffsetsIfNecessary();
				return m_topRight;
			}
			set => throw new ArgumentException($"This setter can not be used. Please use Angles getter/setter instead");
		}
		
		public override Vector2 BottomLeft
		{
			get
			{
				CalcOffsetsIfNecessary();
				return m_bottomLeft;
			}
			set => throw new ArgumentException($"This setter can not be used. Please use Angles getter/setter instead");
		}
		
		public override Vector2 BottomRight
		{
			get
			{
				CalcOffsetsIfNecessary();
				return m_bottomRight;
			}
			set => throw new ArgumentException($"This setter can not be used. Please use Angles getter/setter instead");
		}

		public override bool IsAbsolute => true;

		private void CalcOffsetsIfNecessary()
		{
			if (m_lastBounding == Bounding && 
			    Mathf.Approximately(m_lastAngleHorizontal, m_angleHorizontal) && 
			    Mathf.Approximately(m_lastAngleVertical, m_angleVertical))
				return;

			m_lastBounding = Bounding;
			m_lastAngleHorizontal = m_angleHorizontal;
			m_lastAngleVertical = m_angleVertical;

			var hor = Calc(m_lastBounding.height, m_angleHorizontal);
			m_topLeft.x = Used(CornerFlags.TopLeft) ? hor : 0;
			m_topRight.x = Used(CornerFlags.TopRight) ? hor : 0;
			m_bottomLeft.x = Used(CornerFlags.BottomLeft) ? -hor : 0;
			m_bottomRight.x = Used(CornerFlags.BottomRight) ? -hor : 0;
			var vert = Calc(m_lastBounding.width, m_angleVertical);
			m_topLeft.y = Used(CornerFlags.TopLeft) ? -vert : 0;
			m_topRight.y = Used(CornerFlags.TopRight) ? vert : 0;
			m_bottomLeft.y = Used(CornerFlags.BottomLeft) ? -vert : 0;
			m_bottomRight.y = Used(CornerFlags.BottomRight) ? vert : 0;
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			m_lastBounding = Rect.zero;
		}

		private bool Used(CornerFlags _flag) => (m_usedCorners & _flag) != 0;
		
		// https://en.wikipedia.org/wiki/Law_of_tangents
		private float Calc(float _b, float _alpha) => _b / Mathf.Tan((90-_alpha) * Mathf.Deg2Rad) * .5f;
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiSkew))]
	public class UISkewEditor : UiDistortEditorBase
	{
		private SerializedProperty m_angleHorizontalProp;
		private SerializedProperty m_angleVerticalProp;
		private SerializedProperty m_usedCornersProp;
		protected override bool HasMirror => true;

		public override void OnEnable()
		{
			base.OnEnable();
			m_angleHorizontalProp = serializedObject.FindProperty("m_angleHorizontal");
			m_angleVerticalProp = serializedObject.FindProperty("m_angleVertical");
			m_usedCornersProp = serializedObject.FindProperty("m_usedCorners");
		}

		protected override void Edit( UiDistortBase _thisUiDistort )
		{
			EditorGUILayout.PropertyField(m_angleHorizontalProp);
			EditorGUILayout.PropertyField(m_angleVerticalProp);
			EditorGUILayout.PropertyField(m_usedCornersProp);
		}
	}
#endif

}
