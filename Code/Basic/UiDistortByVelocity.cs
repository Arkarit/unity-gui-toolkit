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

		[SerializeField]
		private AnimationCurve m_frontX;

		[SerializeField]
		private AnimationCurve m_frontY;

		[SerializeField]
		private AnimationCurve m_backX;

		[SerializeField]
		private AnimationCurve m_backY;

		[SerializeField]
		private float  m_maxVelocity = 500	;

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

		protected override void Prepare( Rect _bounding )
		{
			if (m_frontX == null || m_frontY == null || m_backX == null || m_backY == null || m_maxVelocity == 0)
				return;

			float normVelocity = m_velocityChange / m_maxVelocity;
			float normVelocitySgn = Mathf.Sign(normVelocity);
			normVelocity = Mathf.Min(1, Mathf.Abs(normVelocity));
			m_topLeft = m_topRight = m_bottomLeft = m_bottomRight = Vector2.zero;
			float velFrontX = m_frontX.Evaluate(normVelocity);
			float velFrontY = m_frontY.Evaluate(normVelocity);
			float velBackX = m_backX.Evaluate(normVelocity);
			float velBackY = m_backY.Evaluate(normVelocity);

			if (m_direction == EDirection.Horizontal)
			{
				if (normVelocitySgn > 0)
				{
					m_topRight.x = -velFrontX;
					m_bottomRight.x = -velFrontX;
					m_topRight.y = velFrontY;
					m_bottomRight.y = -velFrontY;
				}
			}
		}

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
			SetDirty();

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
		protected SerializedProperty m_frontXProp;
		protected SerializedProperty m_frontYProp;
		protected SerializedProperty m_backXProp;
		protected SerializedProperty m_backYProp;
		protected SerializedProperty m_maxVelocityProp;

		private static bool s_updateInEditor = false;


		public override void OnEnable()
		{
			m_directionProp = serializedObject.FindProperty("m_direction");
			m_velocityDampenProp = serializedObject.FindProperty("m_velocityDampen");
			m_frontXProp = serializedObject.FindProperty("m_frontX");
			m_frontYProp = serializedObject.FindProperty("m_frontY");
			m_backXProp = serializedObject.FindProperty("m_backX");
			m_backYProp = serializedObject.FindProperty("m_backY");
			m_maxVelocityProp = serializedObject.FindProperty("m_maxVelocity");
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
 			EditorGUILayout.PropertyField(m_frontXProp);
 			EditorGUILayout.PropertyField(m_frontYProp);
 			EditorGUILayout.PropertyField(m_backXProp);
 			EditorGUILayout.PropertyField(m_backYProp);
 			EditorGUILayout.PropertyField(m_maxVelocityProp);

			EditorGUIUtility.labelWidth = oldLabelWidth;
		}
	}
#endif



}