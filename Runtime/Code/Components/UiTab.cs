using UnityEngine;

namespace GuiToolkit
{
	public class UiTab : UiToggle
	{
		[SerializeField] protected bool m_ensureVisibilityInScrollRect;

		protected UiScrollRect m_uiScrollRect;

		protected override bool NeedsOnScreenResolutionChangedCallback => m_ensureVisibilityInScrollRect;

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
				UiScrollRect.EnsureChildVisibility(RectTransform);
		}

		protected override void OnScreenResolutionChanged( ScreenResolution _oldScreenResolution, ScreenResolution _newScreenResolution )
		{
			base.OnScreenResolutionChanged(_oldScreenResolution, _newScreenResolution);
			if (Toggle.isOn && UiScrollRect != null)
				UiScrollRect.EnsureChildVisibility(RectTransform, true);
		}
	}
}
