using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistort : UiDistortBase
	{

		[SerializeField]
		protected bool m_absoluteValues;

		public override bool IsAbsolute
		{
			get => m_absoluteValues;
			set
			{
				if (m_absoluteValues == value)
					return;
				ChangeAbsRel(value);
			}
		}

		private void ChangeAbsRel( bool _isAbsolute )
		{
			Rect bounds = Bounding;
			Vector2 fac = new Vector2( _isAbsolute ? bounds.width : 1.0f / bounds.width, _isAbsolute ? bounds.height : 1.0f / bounds.height);
			m_bottomLeft *= fac;
			m_topLeft *= fac;
			m_topRight *= fac;
			m_bottomRight *= fac;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiDistort))]
	public class UiDistortEditor : UiDistortEditorBase
	{
		protected SerializedProperty m_absoluteValuesProp;
		protected SerializedProperty m_topLeftProp;
		protected SerializedProperty m_topRightProp;
		protected SerializedProperty m_bottomLeftProp;
		protected SerializedProperty m_bottomRightProp;

		protected override bool HasMirror { get { return true; } }

		public override void OnEnable()
		{
			base.OnEnable();
			m_absoluteValuesProp = serializedObject.FindProperty("m_absoluteValues");
			m_topLeftProp = serializedObject.FindProperty("m_topLeft");
			m_topRightProp = serializedObject.FindProperty("m_topRight");
			m_bottomLeftProp = serializedObject.FindProperty("m_bottomLeft");
			m_bottomRightProp = serializedObject.FindProperty("m_bottomRight");
		}

		protected override void Edit( UiDistortBase _thisUiDistort )
		{
			bool isAbsolute = m_absoluteValuesProp.boolValue;
			EditorGUILayout.PropertyField(m_absoluteValuesProp);
			if (isAbsolute != m_absoluteValuesProp.boolValue)
				ChangeAbsRel(_thisUiDistort, m_absoluteValuesProp.boolValue);

			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_topLeftProp);
			EditorGUILayout.PropertyField(m_topRightProp);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_bottomLeftProp);
			EditorGUILayout.PropertyField(m_bottomRightProp);
			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = oldLabelWidth;
		}

		private void ChangeAbsRel( UiDistortBase _uiDistort, bool _isAbsolute )
		{
			Rect bounds = _uiDistort.Bounding;
			Vector2 fac = new Vector2( _isAbsolute ? bounds.width : 1.0f / bounds.width, _isAbsolute ? bounds.height : 1.0f / bounds.height);
			m_bottomLeftProp.vector2Value *= fac;
			m_topLeftProp.vector2Value *= fac;
			m_topRightProp.vector2Value *= fac;
			m_bottomRightProp.vector2Value *= fac;
		}

		protected override void Edit2( UiDistortBase _thisUiDistort )
		{
			if (GUILayout.Button("Reset"))
			{
				m_bottomLeftProp.vector2Value = 
				m_topLeftProp.vector2Value = 
				m_topRightProp.vector2Value = 
				m_bottomRightProp.vector2Value = Vector2.zero;
			}
		}

		protected virtual void OnSceneGUI()
		{
			UiDistortBase thisUiDistort = (UiDistortBase)target;
			if (thisUiDistort == null || !thisUiDistort.IsActive())
				return;

			RectTransform rt = (RectTransform) thisUiDistort.transform;

			// Avoid div/0
			if (rt.rect.size.x == 0 || rt.rect.size.y == 0)
				return;

			Rect bounding = thisUiDistort.Bounding;
			Vector3[] corners = bounding.GetWorldCorners(rt);

            Handles.color = Constants.HANDLE_COLOR;

			SerializedProperty blprop = m_bottomLeftProp;
			SerializedProperty tlprop = m_topLeftProp;
			SerializedProperty trprop = m_topRightProp;
			SerializedProperty brprop = m_bottomRightProp;

			EAxis2DFlags mirrorAxes = (EAxis2DFlags) m_mirrorAxisFlagsProp.intValue;
			bool mirrorHorizontal = mirrorAxes.IsFlagSet(EAxis2DFlags.Horizontal);
			bool mirrorVertical = mirrorAxes.IsFlagSet(EAxis2DFlags.Vertical);

			if (mirrorHorizontal)
			{
				UiMathUtility.Swap(ref tlprop, ref trprop);
				UiMathUtility.Swap(ref blprop, ref brprop);
			}

			if (mirrorVertical)
			{
				UiMathUtility.Swap(ref tlprop, ref blprop);
				UiMathUtility.Swap(ref trprop, ref brprop);
			}

			bool isAbsolute = m_absoluteValuesProp.boolValue;
			Vector2 size = isAbsolute ? Vector2.one : bounding.size;

			bool hasChanged = false;
			hasChanged |= EditorUiUtility.DoHandle( blprop, corners[0], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );
			hasChanged |= EditorUiUtility.DoHandle( tlprop, corners[1], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );
			hasChanged |= EditorUiUtility.DoHandle( trprop, corners[2], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );
			hasChanged |= EditorUiUtility.DoHandle( brprop, corners[3], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorGeneralUtility.SetDirty(target);
			}
		}
	}
#endif



}
