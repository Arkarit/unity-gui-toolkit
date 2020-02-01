using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiSkew : UiDistortBase
	{
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiSkew))]
	public class UiSkewEditor : UiDistortEditorBase
	{
		protected override bool HasMirror { get { return true; } }

		protected override void Edit( UiDistortBase thisUiDistort )
		{
			float skewHorizontal = m_topLeftProp.vector2Value.x;
			float skewVertical = m_topLeftProp.vector2Value.y;
			skewHorizontal = EditorGUILayout.FloatField("Horizontal", skewHorizontal);
			skewVertical = EditorGUILayout.FloatField("Vertical", skewVertical);

			SetValue(m_topLeftProp, skewHorizontal, skewVertical);
			SetValue(m_topRightProp, skewHorizontal, -skewVertical);
			SetValue(m_bottomLeftProp, -skewHorizontal, skewVertical);
			SetValue(m_bottomRightProp, -skewHorizontal, -skewVertical);
		}

		private void SetValue(SerializedProperty _prop, float _x, float _y)
		{
			Vector2 vec = new Vector2(_x, _y);
			_prop.vector2Value = vec;
		}

	}
#endif

}
