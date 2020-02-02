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

		private bool DoHandle( SerializedProperty _serProp, Vector3 _rectPoint, Vector2 _rectSize, bool _mirrorHorizontal, bool _mirrorVertical )
		{
			Vector3 normPoint = _serProp.vector2Value;
			normPoint.x *= _mirrorHorizontal ? -1 : 1;
			normPoint.y *= _mirrorVertical ? -1 : 1;

			Vector3 point = _rectPoint + Vector3.Scale(normPoint, _rectSize);
			Handles.DotHandleCap(0, point, Quaternion.identity, 5, EventType.Repaint);
			Vector3 oldLeft = point;
			Vector3 newLeft = Handles.FreeMoveHandle(oldLeft, Quaternion.identity, 5, Vector3.zero, Handles.DotHandleCap);
			if (oldLeft != newLeft)
			{
				Vector2 val = (newLeft - _rectPoint) / _rectSize;
				val.x *= _mirrorHorizontal ? -1 : 1;
				val.y *= _mirrorVertical ? -1 : 1;
				_serProp.vector2Value = val;
				return true;
			}
			return false;
		}

		public void OnSceneGUI()
		{
			UiDistortBase thisUiDistort = (UiDistortBase)target;
			RectTransform rt = (RectTransform) thisUiDistort.transform;

			Rect rect = rt.GetWorldRect2D();
			rect = rect.Absolute();
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

			bool hasChanged = DoHandle( blprop, rect.BottomLeft3(), rect.size, mirrorHorizontal, mirrorVertical);
			hasChanged |= DoHandle( tlprop, rect.TopLeft3(), rect.size, mirrorHorizontal, mirrorVertical );
			hasChanged |= DoHandle( trprop, rect.TopRight3(), rect.size, mirrorHorizontal, mirrorVertical );
			hasChanged |= DoHandle( brprop, rect.BottomRight3(), rect.size, mirrorHorizontal, mirrorVertical );

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}

		}

	}
#endif



}