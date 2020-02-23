using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{

	[CustomEditor(typeof(UiSimpleAnimationBase))]
	public class UiSimpleAnimationBaseEditor : Editor, IEditorUpdateable
	{
		protected SerializedProperty m_durationProp;
		protected SerializedProperty m_delayProp;
		protected SerializedProperty m_backwardsPlayableProp;
		protected SerializedProperty m_gotoStartOnBackwardsProp;
		protected SerializedProperty m_autoStartProp;
		protected SerializedProperty m_setOnStartProp;
		protected SerializedProperty m_numberOfLoopsProp;
		protected SerializedProperty m_slaveAnimationsProp;
		protected SerializedProperty m_setLoopsForSlavesProp;

		private readonly List<UiSimpleAnimationBase> m_animationsToUpdate = new List<UiSimpleAnimationBase>();

		public virtual void OnEnable()
		{
			m_durationProp = serializedObject.FindProperty("m_duration");
			m_delayProp = serializedObject.FindProperty("m_delay");
			m_backwardsPlayableProp = serializedObject.FindProperty("m_backwardsPlayable");
			m_gotoStartOnBackwardsProp = serializedObject.FindProperty("m_gotoStartOnBackwards");
			m_autoStartProp = serializedObject.FindProperty("m_autoStart");
			m_setOnStartProp = serializedObject.FindProperty("m_setOnStart");
			m_numberOfLoopsProp = serializedObject.FindProperty("m_numberOfLoops");
			m_slaveAnimationsProp = serializedObject.FindProperty("m_slaveAnimations");
			m_setLoopsForSlavesProp = serializedObject.FindProperty("m_setLoopsForSlaves");
		}

		public virtual void OnDisable()
		{
			Stop();
		}

		public virtual void EditSubClass() { }

		public override void OnInspectorGUI()
		{
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;

			GUILayout.Label("Timing:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_durationProp);
			EditorGUILayout.PropertyField(m_delayProp);

			EditorGUILayout.PropertyField(m_backwardsPlayableProp);
			EditorGUI.BeginDisabledGroup(m_backwardsPlayableProp.boolValue);
			EditorGUILayout.PropertyField(m_gotoStartOnBackwardsProp);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.PropertyField(m_numberOfLoopsProp);
			EditorGUILayout.PropertyField(m_autoStartProp);
			EditorGUI.BeginDisabledGroup(m_autoStartProp.boolValue);
			EditorGUILayout.PropertyField(m_setOnStartProp);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			GUILayout.Label("Slave animations:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_slaveAnimationsProp, true);
			EditorGUILayout.PropertyField(m_setLoopsForSlavesProp);
			EditorGUILayout.Space();

			EditSubClass();

			EditorGUILayout.Space();

			DisplayTestFields();

			serializedObject.ApplyModifiedProperties();

			#if DEBUG_SIMPLE_ANIMATION
				EditorGUILayout.Space(50);
				GUILayout.Label("Default Inspector:", EditorStyles.boldLabel);
				DrawDefaultInspector();
				serializedObject.ApplyModifiedProperties();
			#endif

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
		}

		private void Play(bool _backwards = false)
		{
			Stop();
			CollectAnimations();
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;
			thisUiSimpleAnimationBase.Play(_backwards);
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
				if (animation.Running)
					return false;

			return true;
		}
	}
}