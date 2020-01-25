using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistortByVelocity : UiDistortBase, IEditorUpdateable
	{


		[SerializeField]
		[Range(0,1)]
		private float m_velocityDampen = 0.9f;

		private float m_velocity;
		private float m_velocityChange;
		private Vector2 m_lastPosition;

#if UNITY_EDITOR
		public bool m_updating;

		public bool RemoveFromEditorUpdate()
		{
			return !m_updating;
		}

		public void UpdateInEditor( float _deltaTime )
		{
			Update(_deltaTime);
		}
#endif

		protected virtual void Update()
		{
			Update(Time.deltaTime);
		}

		private void Update( float _deltaTime )
		{
			Debug.Log($"m_velocity; {m_velocity} m_velocityChange: {m_velocityChange}");
		}

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiDistortByVelocity))]
	public class UiDistortByVelocityEditor : UiDistortEditorBase
	{
		private static bool s_updateInEditor = false;

		protected override void Edit( UiDistortBase _thisUiDistort )
		{
			UiDistortByVelocity thisUiDistortByVelocity = (UiDistortByVelocity) _thisUiDistort;

			bool oldUpdateInEditor = s_updateInEditor;
			s_updateInEditor = GUILayout.Toggle(s_updateInEditor, "Update when not running");
			if (oldUpdateInEditor != s_updateInEditor)
			{
				if (s_updateInEditor)
				{
					thisUiDistortByVelocity.m_updating = true;
					EditorUpdater.StartUpdating(thisUiDistortByVelocity);
				}
				else
				{
					thisUiDistortByVelocity.m_updating = false;
				}
			}

			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;

// 			EditorGUILayout.BeginHorizontal();
// 			EditorGUILayout.PropertyField(m_topLeftProp);
// 			EditorGUILayout.PropertyField(m_topRightProp);
// 			EditorGUILayout.EndHorizontal();
// 
// 			EditorGUILayout.BeginHorizontal();
// 			EditorGUILayout.PropertyField(m_bottomLeftProp);
// 			EditorGUILayout.PropertyField(m_bottomRightProp);
// 			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = oldLabelWidth;
		}
	}
#endif



}