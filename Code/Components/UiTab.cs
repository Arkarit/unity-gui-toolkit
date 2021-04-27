using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTab : UiToggle
	{
		protected UiTabScrollRect m_uiTabScrollRect;

		public UiTabScrollRect TabScrollRect
		{
			get
			{
				if (m_uiTabScrollRect == null)
					m_uiTabScrollRect = GetComponentInParent<UiTabScrollRect>();
				return m_uiTabScrollRect;
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
			if (_isActive && TabScrollRect != null)
				EnsureVisible();
		}

		private void EnsureVisible()
		{
			TabScrollRect.ScrollToChild(RectTransform);
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiTab))]
	public class UiTabEditor : UiToggleEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}
#endif
}
