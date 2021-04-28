using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTab : UiToggle
	{
		protected UiScrollRect m_uiScrollRect;

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
			if (_isActive && UiScrollRect != null)
				UiScrollRect.EnsureTabVisibility(this);
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
