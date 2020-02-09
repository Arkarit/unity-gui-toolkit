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
		private const float HANDLE_SIZE = 0.08f;
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

		private bool DoHandle( SerializedProperty _serProp, Vector3 _rectPoint, Vector2 _rectSize, bool _mirrorHorizontal, bool _mirrorVertical, RectTransform _rt )
		{
			Vector3 normPoint = _serProp.vector2Value;
			normPoint.x *= _mirrorHorizontal ? -1 : 1;
			normPoint.y *= _mirrorVertical ? -1 : 1;

			Vector3 offset = _rt.TransformVector(Vector3.Scale(normPoint, _rectSize));
			Vector3 point = _rectPoint + offset;

			float handleSize = HANDLE_SIZE * HandleUtility.GetHandleSize(Vector3.zero);

			Handles.DotHandleCap(0, point, Quaternion.identity, handleSize, EventType.Repaint);
			Vector3 oldLeft = point;
			Vector3 newLeft = Handles.FreeMoveHandle(oldLeft, Quaternion.identity, handleSize, Vector3.zero, Handles.DotHandleCap);
			if (oldLeft != newLeft)
			{
				// Move offset in screen space
				Vector3 moveOffset = _rt.TransformVector(newLeft-oldLeft);

				// Bring it to the space of the rect transform
				moveOffset = _rt.InverseTransformVector(moveOffset);

				// apply mirror
				moveOffset.x *= _mirrorHorizontal ? -1 : 1;
				moveOffset.y *= _mirrorVertical ? -1 : 1;

				_serProp.vector2Value += moveOffset.Xy() / _rectSize;

				return true;
			}
			return false;
		}

		public void OnSceneGUI()
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
            Handles.color = Color.yellow;

			SerializedProperty blprop = m_bottomLeftProp;
			SerializedProperty tlprop = m_topLeftProp;
			SerializedProperty trprop = m_topRightProp;
			SerializedProperty brprop = m_bottomRightProp;

			EDirection mirrorDirection = (EDirection) m_mirrorDirectionProp.intValue;
			bool mirrorHorizontal = mirrorDirection.IsFlagSet(EDirection.Horizontal);
			bool mirrorVertical = mirrorDirection.IsFlagSet(EDirection.Vertical);

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
			hasChanged |= DoHandle( blprop, corners[0], size, mirrorHorizontal, mirrorVertical, rt );
			hasChanged |= DoHandle( tlprop, corners[1], size, mirrorHorizontal, mirrorVertical, rt );
			hasChanged |= DoHandle( trprop, corners[2], size, mirrorHorizontal, mirrorVertical, rt );
			hasChanged |= DoHandle( brprop, corners[3], size, mirrorHorizontal, mirrorVertical, rt );

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}

		}

	}
#endif



}