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

		public void OnSceneGUI()
		{
			UiDistortBase thisUiDistort = (UiDistortBase)target;
			RectTransform rt = (RectTransform) thisUiDistort.transform;

			Vector2 bl = m_bottomLeftProp.vector2Value;
			Vector2 tl = m_topLeftProp.vector2Value;
			Vector2 tr = m_topRightProp.vector2Value;
			Vector2 br = m_bottomRightProp.vector2Value;

			Rect rect = rt.GetWorldRect2D();
			rect = rect.Absolute();
//Debug.Log($"rect:{rect} rect.BottomLeft():{rect.BottomLeft()} rect.TopLeft():{rect.TopLeft()}");
            Handles.color = Color.yellow;
			Vector3 pbl = rect.BottomLeft() + bl * rect.size;

            Handles.DotHandleCap( 0, pbl, Quaternion.identity, 5, EventType.Repaint );
            Vector3 oldLeft = pbl;
            Vector3 newLeft = Handles.FreeMoveHandle(oldLeft, Quaternion.identity, 5, Vector3.zero, Handles.DotHandleCap);
            bool hasChanged = false;
            if (oldLeft != newLeft)
            {
Debug.Log($"oldLeft:{oldLeft} newLeft:{newLeft}");
				Vector2 val = (pbl - rect.BottomLeft3()) / rect.size;
				m_bottomLeftProp.vector2Value = val;
				hasChanged = true;
			}

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}


		}

	}
#endif



}