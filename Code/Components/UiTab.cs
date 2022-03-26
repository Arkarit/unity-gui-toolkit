using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTab : UiToggle
	{
		[SerializeField] protected bool m_ensureVisibilityInScrollRect;

		protected UiScrollRect m_uiScrollRect;

		protected override bool NeedsOnScreenOrientationCallback => m_ensureVisibilityInScrollRect;

		public UiScrollRect UiScrollRect
		{
			get
			{
				if (m_uiScrollRect == null)
					m_uiScrollRect = GetComponentInParent<UiScrollRect>();
				return m_uiScrollRect;
			}
		}

		/// Override to add your event listeners.
		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			OnValueChanged.AddListener(OnToggleChanged);
		}

		/// Override to remove your event listeners.
		protected override void RemoveEventListeners()
		{
			base.RemoveEventListeners();
			OnValueChanged.RemoveListener(OnToggleChanged);
		}

		private void OnToggleChanged( bool _isActive )
		{
			if (_isActive && UiScrollRect != null && m_ensureVisibilityInScrollRect)
				UiScrollRect.EnsureVisible(RectTransform);
		}

		protected override void OnScreenOrientationChanged( EScreenOrientation _oldScreenOrientation, EScreenOrientation _newScreenOrientation )
		{
			base.OnScreenOrientationChanged(_oldScreenOrientation, _newScreenOrientation);
			if (Toggle.isOn && UiScrollRect != null)
				UiScrollRect.EnsureVisible(RectTransform, false, true);
		}

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiTab))]
	public class UiTabEditor : UiToggleEditor
	{
		protected SerializedProperty m_ensureVisibilityInScrollRectProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_ensureVisibilityInScrollRectProp = serializedObject.FindProperty("m_ensureVisibilityInScrollRect");
		}

		public override void OnInspectorGUI()
		{
			EditorGUILayout.PropertyField(m_ensureVisibilityInScrollRectProp);
			base.OnInspectorGUI();
		}
	}
#endif
}
