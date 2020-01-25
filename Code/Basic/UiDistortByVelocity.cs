using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiDistortByVelocity : UiDistortBase, IEditorUpdateable
	{
		[SerializeField]
		private EDirection m_direction = EDirection.Horizontal;

		[SerializeField]
		[Range(0,1)]
		private float m_velocityDampen = 0.9f;

		private float m_velocityChange;

		private float m_lastVelocity;
		private Vector2 m_lastPosition;

#if UNITY_EDITOR
		[System.NonSerialized]
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
			if (Application.isPlaying)
				Update(Time.deltaTime);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_lastPosition = m_rectTransform.anchoredPosition;
			m_lastVelocity = 0;
		}

		private void Update( float _deltaTime )
		{
#if UNITY_EDITOR
			if (ShouldSkipUpdate())
				return;
#endif
			if (_deltaTime == 0)
				return;

			m_velocityChange *= m_velocityDampen;

			Vector2 movementV2 = m_rectTransform.anchoredPosition - m_lastPosition;
			m_lastPosition = m_rectTransform.anchoredPosition;

			float movement = m_direction == EDirection.Horizontal ? movementV2.x : movementV2.y;

			float velocity = movement / _deltaTime;
			m_velocityChange -= velocity - m_lastVelocity;
			m_lastVelocity = velocity;

if (Mathf.Abs(m_velocityChange) > 0.0001)
Debug.Log($"m_lastVelocity: {m_lastVelocity} m_velocityChange: {m_velocityChange}");
		}

#if UNITY_EDITOR
		private bool ShouldSkipUpdate()
		{
			if (Application.isPlaying)
				return false;
			if (m_updating)
				return false;
			return true;
		}
#endif

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiDistortByVelocity))]
	public class UiDistortByVelocityEditor : UiDistortEditorBase
	{
		protected SerializedProperty m_directionProp;
		protected SerializedProperty m_velocityDampenProp;

		private static bool s_updateInEditor = false;


		public override void OnEnable()
		{
			m_directionProp = serializedObject.FindProperty("m_direction");
			m_velocityDampenProp = serializedObject.FindProperty("m_velocityDampen");
		}

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

 			EditorGUILayout.PropertyField(m_directionProp);
 			EditorGUILayout.PropertyField(m_velocityDampenProp);

			EditorGUIUtility.labelWidth = oldLabelWidth;
		}
	}
#endif



}