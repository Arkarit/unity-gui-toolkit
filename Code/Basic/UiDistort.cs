using UnityEngine;
using System;

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
		protected override bool IsAbsolute { get { return m_absoluteValues; } }
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiDistort))]
	public class UiDistortEditor : UiDistortEditorBase
	{
		protected SerializedProperty m_absoluteValuesProp;

		protected override bool HasMirror { get { return true; } }

		public override void OnEnable()
		{
			base.OnEnable();
			m_absoluteValuesProp = serializedObject.FindProperty("m_absoluteValues");
		}

		protected override void Edit( UiDistortBase thisUiDistort )
		{
			bool isAbsolute = m_absoluteValuesProp.boolValue;
			EditorGUILayout.PropertyField(m_absoluteValuesProp);
			if (isAbsolute != m_absoluteValuesProp.boolValue)
				ChangeAbsRel(thisUiDistort, m_absoluteValuesProp.boolValue);

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

		protected override void Edit2( UiDistortBase thisUiDistort )
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
			if (!thisUiDistort.IsActive())
				return;

			RectTransform rt = (RectTransform) thisUiDistort.transform;

			// Avoid div/0
			if (rt.rect.size.x == 0 || rt.rect.size.y == 0)
				return;

			Rect bounding = thisUiDistort.Bounding;
			Vector2[] corners = thisUiDistort.Bounding.GetWorldCorners2D(rt);
            Handles.color = Constants.HANDLE_COLOR;

			SerializedProperty blprop = m_bottomLeftProp;
			SerializedProperty tlprop = m_topLeftProp;
			SerializedProperty trprop = m_topRightProp;
			SerializedProperty brprop = m_bottomRightProp;

			EDirectionFlags mirrorDirection = (EDirectionFlags) m_mirrorDirectionProp.intValue;
			bool mirrorHorizontal = mirrorDirection.IsFlagSet(EDirectionFlags.Horizontal);
			bool mirrorVertical = mirrorDirection.IsFlagSet(EDirectionFlags.Vertical);

			if (mirrorHorizontal)
			{
				UiMath.Swap(ref tlprop, ref trprop);
				UiMath.Swap(ref blprop, ref brprop);
			}

			if (mirrorVertical)
			{
				UiMath.Swap(ref tlprop, ref blprop);
				UiMath.Swap(ref trprop, ref brprop);
			}

			bool isAbsolute = m_absoluteValuesProp.boolValue;
			Vector2 size = isAbsolute ? bounding.size / rt.rect.size : bounding.size;

			bool hasChanged = false;
			hasChanged |= UiEditorUtility.DoHandle( blprop, corners[0], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );
			hasChanged |= UiEditorUtility.DoHandle( tlprop, corners[1], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );
			hasChanged |= UiEditorUtility.DoHandle( trprop, corners[2], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );
			hasChanged |= UiEditorUtility.DoHandle( brprop, corners[3], size, rt, mirrorHorizontal, mirrorVertical, Constants.HANDLE_SIZE );

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}

		}

	}
#endif



}