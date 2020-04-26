using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[RequireComponent(typeof(Button))]
	public class UiButton : UiButtonBase
	{
		[Tooltip("Simple wiggle animation (optional)")]
		public UiSimpleAnimation m_simpleWiggleAnimation;

		private Button m_button;

		public Button Button
		{
			get
			{
				InitIfNecessary();
				return m_button;
			}
		}

		public Button.ButtonClickedEvent OnClick => Button.onClick;

		public void Wiggle()
		{
			if (m_simpleWiggleAnimation)
				m_simpleWiggleAnimation.Play();
		}

		protected override void OnEnabledChanged(bool _enabled)
		{
			base.OnEnabledChanged(_enabled);
			InitIfNecessary();
			m_button.interactable = _enabled;
		}

		protected override void Init()
		{
			base.Init();

			m_button = GetComponent<Button>();
		}

	}

	#if UNITY_EDITOR
	[CustomEditor(typeof(UiButton))]
	public class UiButtonEditor : UiButtonBaseEditor
	{
		protected SerializedProperty m_simpleWiggleAnimationProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_simpleWiggleAnimationProp = serializedObject.FindProperty("m_simpleWiggleAnimation");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			UiButton thisButton = (UiButton)target;

			EditorGUILayout.PropertyField(m_simpleWiggleAnimationProp);

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif

}