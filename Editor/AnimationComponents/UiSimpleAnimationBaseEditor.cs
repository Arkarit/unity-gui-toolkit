using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit.Editor
{

	[CustomEditor(typeof(UiSimpleAnimationBase))]
	public class UiSimpleAnimationBaseEditor : UnityEditor.Editor, IEditorUpdateable
	{
		protected SerializedProperty m_durationProp;
		protected SerializedProperty m_delayProp;
		protected SerializedProperty m_animationSpeedProp;
		protected SerializedProperty m_setSpeedForSlavesProp;
		protected SerializedProperty m_backwardsPlayableProp;
		protected SerializedProperty m_backwardsAnimationProp;
		protected SerializedProperty m_gotoStartOnBackwardsProp;
		protected SerializedProperty m_autoStartProp;
		protected SerializedProperty m_autoOnEnableProp;
		protected SerializedProperty m_setOnStartProp;
		protected SerializedProperty m_numberOfLoopsProp;
		protected SerializedProperty m_finishInstantOnResolutionChangeProp;
		protected SerializedProperty m_slaveAnimationsProp;
		protected SerializedProperty m_setLoopsForSlavesProp;
		protected SerializedProperty m_supportViewAnimationsProp;
		
		private static bool s_drawDefaultInspector;


#if DEBUG_SIMPLE_ANIMATION && !DEBUG_SIMPLE_ANIMATION_ALL
		protected SerializedProperty m_debugProp;
#endif

		private readonly List<UiSimpleAnimationBase> m_animationsToUpdate = new List<UiSimpleAnimationBase>();

		public virtual void OnEnable()
		{
			m_durationProp = serializedObject.FindProperty("m_duration");
			m_delayProp = serializedObject.FindProperty("m_delay");
			m_animationSpeedProp = serializedObject.FindProperty("m_animationSpeed");
			m_setSpeedForSlavesProp = serializedObject.FindProperty("m_setSpeedForSlaves");
			m_backwardsPlayableProp = serializedObject.FindProperty("m_backwardsPlayable");
			m_backwardsAnimationProp = serializedObject.FindProperty("m_backwardsAnimation");
			m_gotoStartOnBackwardsProp = serializedObject.FindProperty("m_gotoStartOnBackwards");
			m_autoStartProp = serializedObject.FindProperty("m_autoStart");
			m_autoOnEnableProp = serializedObject.FindProperty("m_autoOnEnable");
			m_setOnStartProp = serializedObject.FindProperty("m_setOnStart");
			m_numberOfLoopsProp = serializedObject.FindProperty("m_numberOfLoops");
			m_finishInstantOnResolutionChangeProp = serializedObject.FindProperty("m_finishInstantOnResolutionChange");
			m_slaveAnimationsProp = serializedObject.FindProperty("m_slaveAnimations");
			m_setLoopsForSlavesProp = serializedObject.FindProperty("m_setLoopsForSlaves");
			m_supportViewAnimationsProp = serializedObject.FindProperty("m_supportViewAnimations");
#if DEBUG_SIMPLE_ANIMATION && !DEBUG_SIMPLE_ANIMATION_ALL
			m_debugProp = serializedObject.FindProperty("m_debug");
#endif
		}

		public virtual void OnDisable()
		{
			Stop();
		}

		public virtual bool DisplayDurationProp => true;
		public virtual bool DisplaySlaveAnimations => true;


		public virtual void EditSubClass() { }

		public override void OnInspectorGUI()
		{
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;

			DisplayViewConnection();

			GUILayout.Label("Timing:", EditorStyles.boldLabel);
			if (DisplayDurationProp)
				EditorGUILayout.PropertyField(m_durationProp);

			EditorGUILayout.PropertyField(m_delayProp);
			EditorGUILayout.PropertyField(m_animationSpeedProp);

			EditorGUILayout.Space();
			
			EditorGUILayout.PropertyField(m_backwardsPlayableProp);
			EditorGUI.BeginDisabledGroup(m_backwardsPlayableProp.boolValue);
			EditorGUILayout.PropertyField(m_gotoStartOnBackwardsProp);
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!m_backwardsPlayableProp.boolValue);
			EditorGUILayout.PropertyField(m_backwardsAnimationProp);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.PropertyField(m_numberOfLoopsProp);
			EditorGUILayout.PropertyField(m_finishInstantOnResolutionChangeProp);
			EditorGUILayout.PropertyField(m_autoStartProp);
			EditorGUILayout.PropertyField(m_autoOnEnableProp);
			EditorGUI.BeginDisabledGroup(m_autoStartProp.boolValue || m_autoOnEnableProp.boolValue);
			EditorGUILayout.PropertyField(m_setOnStartProp);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			if (DisplaySlaveAnimations)
			{
				GUILayout.Label("Slave animations:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_slaveAnimationsProp, true);
				EditorGUILayout.PropertyField(m_setLoopsForSlavesProp);
				EditorGUILayout.PropertyField(m_setSpeedForSlavesProp);
				EditorGUILayout.Space();
			}

			EditSubClass();

#if DEBUG_SIMPLE_ANIMATION && !DEBUG_SIMPLE_ANIMATION_ALL
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(m_debugProp);
#endif

			EditorGUILayout.Space();

			DisplayTestFields();

			serializedObject.ApplyModifiedProperties();
			
			s_drawDefaultInspector = GUILayout.Toggle(s_drawDefaultInspector, "Draw Default Inspector");
			if (!s_drawDefaultInspector)
				return;

			EditorGUILayout.Space(50);
			GUILayout.Label("Default Inspector:", EditorStyles.boldLabel);
			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();

		}

		private void DisplayViewConnection()
		{
			GUILayout.Label("UIView / UIPanel connection:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_supportViewAnimationsProp, new GUIContent("Support UIView and UIPanel"));
			EditorGUILayout.Space();
		}

		private void DisplayTestFields()
		{
			GUILayout.Label("Test:", EditorStyles.boldLabel);
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Forwards"))
			{
				Play();
			}
			if (GUILayout.Button("Backwards"))
			{
				Play(true);
			}
			if (GUILayout.Button("Stop"))
			{
				Stop();
			}
			if (GUILayout.Button("To Begin"))
			{
				Reset(false);
			}
			if (GUILayout.Button("To End"))
			{
				Reset(true);
			}

			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();

			EditorUpdater.TimeScale = EditorGUILayout.Slider("Test Speed", EditorUpdater.TimeScale, 0.001f, 5);
			EditorGUI.BeginDisabledGroup(EditorUpdater.TimeScale == 1);
			if (GUILayout.Button("Apply"))
			{
				if (EditorUtility.DisplayDialog("Apply?", "Apply time scale to whole animation?", "OK", "Cancel" ))
					ApplyTimeScale();
			}
			EditorGUI.EndDisabledGroup();

			GUILayout.EndHorizontal();

		}

		private void ApplyTimeScale()
		{
			Stop();
			CollectAnimations();
			float scale = 1.0f / EditorUpdater.TimeScale;
			foreach (var animation in m_animationsToUpdate)
			{
				Undo.RecordObject(animation, "Apply Time Scale");
				animation.Duration *= scale;
				animation.Delay *= scale;
			}
			m_animationsToUpdate.Clear();
			EditorUpdater.TimeScale = 1;
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;
			EditorSceneManager.MarkSceneDirty(thisUiSimpleAnimationBase.gameObject.scene);
		}


		private void Play(bool _backwards = false)
		{
			Stop();
			CollectAnimations();
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;
			thisUiSimpleAnimationBase.EditorPlay(_backwards);
			EditorUpdater.StartUpdating(this);
		}

		private void Stop()
		{
			foreach( var animation in m_animationsToUpdate)
				animation.Stop();
			m_animationsToUpdate.Clear();
			EditorUpdater.StopUpdating(this);
		}

		private void Reset(bool _toEnd)
		{
			Stop();
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;
			thisUiSimpleAnimationBase.Reset(_toEnd);
		}

		private void CollectAnimations()
		{
			Debug.Assert(m_animationsToUpdate.Count == 0);
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;
			CollectAnimationsRecursive(thisUiSimpleAnimationBase);
		}

		private void CollectAnimationsRecursive( UiSimpleAnimationBase uiSimpleAnimationBase )
		{
			m_animationsToUpdate.Add(uiSimpleAnimationBase);
			foreach(var animation in uiSimpleAnimationBase.SlaveAnimations)
				CollectAnimationsRecursive(animation);
		}

		public void UpdateInEditor( float _deltaTime )
		{
			foreach(var animation in m_animationsToUpdate)
				animation.UpdateInEditor(_deltaTime);
		}

		public bool RemoveFromEditorUpdate()
		{
			foreach(var animation in m_animationsToUpdate)
				if (animation.IsPlaying)
					return false;

			return true;
		}

		public UpdateCondition editorUpdateCondition => UpdateCondition.IsNotPlaying;
	}
}
