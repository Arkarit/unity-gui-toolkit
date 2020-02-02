using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistort : UiDistortBase
	{
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiDistort))]
	public class UiDistortEditor : UiDistortEditorBase
	{
		protected override bool HasMirror { get { return true; } }

		protected override void Edit( UiDistortBase thisUiDistort )
		{
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

		private bool DoHandle( ref SerializedProperty _serProp, Vector3 _rectPoint, Vector2 _rectSize )
		{
			Vector3 normPoint = _serProp.vector2Value;
			Vector3 point = _rectPoint + Vector3.Scale(normPoint, _rectSize);
			Handles.DotHandleCap(0, point, Quaternion.identity, 5, EventType.Repaint);
			Vector3 oldLeft = point;
			Vector3 newLeft = Handles.FreeMoveHandle(oldLeft, Quaternion.identity, 5, Vector3.zero, Handles.DotHandleCap);
			if (oldLeft != newLeft)
			{
				Vector2 val = (newLeft - _rectPoint) / _rectSize;
				_serProp.vector2Value = val;
				return true;
			}
			return false;
		}

		public void OnSceneGUI()
		{
			UiDistortBase thisUiDistort = (UiDistortBase)target;
			RectTransform rt = (RectTransform) thisUiDistort.transform;

			Vector3 tl = m_topLeftProp.vector2Value;
			Vector3 tr = m_topRightProp.vector2Value;
			Vector3 br = m_bottomRightProp.vector2Value;

			Rect rect = rt.GetWorldRect2D();
			rect = rect.Absolute();
            Handles.color = Color.yellow;

			bool hasChanged = DoHandle( ref m_bottomLeftProp, rect.BottomLeft3(), rect.size);
			hasChanged |= DoHandle( ref m_topLeftProp, rect.TopLeft3(), rect.size );
			hasChanged |= DoHandle( ref m_topRightProp, rect.TopRight3(), rect.size );
			hasChanged |= DoHandle( ref m_bottomRightProp, rect.BottomRight3(), rect.size );

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}

		}

	}
#endif



}