using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// <summary>
	/// Reusable base class for dropdown-style UI components backed by <see cref="UiPopup"/>.
	///
	/// Responsibilities:
	/// - Manages open/close state via an explicit <see cref="UiButton"/> toggle.
	/// - Populates the popup via the virtual <see cref="PopulatePopup"/> method.
	/// - Fires <see cref="EvOnDropdownValueChanged"/> and calls the virtual
	///   <see cref="OnDropdownValueChanged"/> when the user picks an item.
	/// - Detects open/close transitions and fires <see cref="EvOnStatusChanged"/> and
	///   calls the virtual <see cref="OnStatusChanged"/>.
	/// - Plays an optional <see cref="UiSimpleAnimationBase"/> forwards on open and
	///   backwards on close.
	/// - Pauses an optional <see cref="UiSimpleAnimationMouseOver"/> while the popup is
	///   open so the UiPopup ClickCatcher cannot reset the hover animation.
	///
	/// Derive from this class and override the virtual methods as needed.
	/// </summary>
	public class UiDropdown : UiThing
	{
		[Tooltip("Button that toggles the popup open and closed.")]
		[SerializeField] protected UiButton m_toggleButton;

		[Tooltip("Prefab for the popup menu shown when the dropdown opens.")]
		[SerializeField] private UiPopup m_popupMenuPrefab;

		[Tooltip("Label that shows the currently selected item. Optional.")]
		[SerializeField][Optional] protected TextMeshProUGUI m_selectedLabel;

		[Tooltip("Optional animation played forwards when the dropdown opens and backwards when it closes.")]
		[SerializeField][Optional] protected UiSimpleAnimationBase m_statusAnimation;

		[Tooltip("Optional hover animation. Paused while the popup is open so the UiPopup ClickCatcher " +
		         "cannot fire pointer-exit events that would reset the hover state.")]
		[SerializeField][Optional] private UiSimpleAnimationMouseOver m_hoverAnimation;

		[Tooltip("Maximum popup height in canvas pixels. If the content is taller a vertical scrollbar appears. Values <= 0 use the popup prefab's own default.")]
		[SerializeField] private float m_maxPopupHeight = 600f;

		[Tooltip("Fine-positioning offset applied to the popup relative to the anchor (canvas pixels).")]
		[SerializeField] private Vector2 m_offset = Vector2.zero;

		public CEvent<int> EvOnDropdownValueChanged = new();
		public CEvent<bool> EvOnStatusChanged = new();

		private string[] m_presetStringItems;

		/// <summary>
		/// Optional string items to inject into the popup without subclassing.
		/// When set, <see cref="UpdateLabel"/> automatically uses these for the selected-item label.
		/// </summary>
		public string[] PresetStringItems
		{
			get => m_presetStringItems;
			set => m_presetStringItems = value;
		}

		/// <summary>
		/// Gets or sets the selected index.
		/// The setter is <b>silent</b>: it updates the internal index and refreshes the label
		/// but does <b>not</b> fire <see cref="EvOnDropdownValueChanged"/>.
		/// </summary>
		public int SelectedIndex
		{
			get => m_selectedIndex;
			set { m_selectedIndex = value; UpdateLabel(); }
		}

		/// <summary>Maximum popup height in canvas pixels. If the content is taller a vertical scrollbar appears.</summary>
		public float MaxPopupHeight
		{
			get => m_maxPopupHeight;
			set => m_maxPopupHeight = value;
		}

		/// <summary>Fine-positioning offset applied to the popup relative to the anchor (canvas pixels).</summary>
		public Vector2 Offset
		{
			get => m_offset;
			set => m_offset = value;
		}

		protected int m_selectedIndex = -1;
		private bool m_isOpen;
		private UiPopup m_activePopup;

		protected override void Awake()
		{
			AddOnEnableButtonListeners((m_toggleButton, OnToggleButtonClicked));
			base.Awake();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (m_isOpen)
				ClosePopup();
		}

		private void OnValidate()
		{
			if (m_activePopup != null)
				m_activePopup.UpdateOffset(m_offset);
		}

		/// <summary>Override to fill the popup with items via <paramref name="options"/>.</summary>
		protected virtual void PopulatePopup( UiPopup.Options options )
		{
			if (m_presetStringItems != null)
				options.StringItems = m_presetStringItems;
		}

		/// <summary>Override to refresh the selected-item label text.</summary>
		protected virtual void UpdateLabel()
		{
			if (m_selectedLabel == null || m_presetStringItems == null)
				return;
			if (m_selectedIndex >= 0 && m_selectedIndex < m_presetStringItems.Length)
				m_selectedLabel.text = m_presetStringItems[m_selectedIndex];
		}

		/// <summary>
		/// Called when the user selects an item. Base implementation fires
		/// <see cref="EvOnDropdownValueChanged"/> and calls <see cref="UpdateLabel"/>.
		/// </summary>
		protected virtual void OnDropdownValueChanged( int _index )
		{
			m_selectedIndex = _index;
			EvOnDropdownValueChanged.Invoke(_index);
			UpdateLabel();
		}

		/// <summary>
		/// Called when the popup opens (<paramref name="_isOpen"/>=true) or closes (=false).
		/// Also fires <see cref="EvOnStatusChanged"/> and plays <see cref="m_statusAnimation"/>.
		/// </summary>
		protected virtual void OnStatusChanged( bool _isOpen )
		{
			EvOnStatusChanged.Invoke(_isOpen);
			if (m_statusAnimation != null)
				m_statusAnimation.Play(!_isOpen); // false=forwards on open, true=backwards on close

			if (m_hoverAnimation != null)
			{
				if (_isOpen)
				{
					// Pause mouse-over detection and force hover state visible while popup is open.
					m_hoverAnimation.PauseMouseOverAnimation = true;
					m_hoverAnimation.Play(false);
				}
				else
				{
					// Popup closed: reset hover to off-state and re-enable mouse-over detection.
					m_hoverAnimation.Reset();
					m_hoverAnimation.PauseMouseOverAnimation = false;
				}
			}
		}

		/// <summary>
		/// Apply a visual selection indicator to a spawned popup item.
		/// Default implementation tints the item's <see cref="TextMeshProUGUI"/> yellow when selected.
		/// </summary>
		protected virtual void ApplyItemSelectionVisual( GameObject item, bool selected )
		{
			if (item == null)
				return;

			var tmp = item.GetComponentInChildren<TextMeshProUGUI>();
			if (tmp != null)
				tmp.color = selected ? Color.yellow : Color.white;
		}

		private void OnToggleButtonClicked()
		{
			// Suppress clicks that land during scroll-rect inertia (user tapping to stop momentum).
			// Velocity is captured at PointerDown time (before any deceleration that occurs between
			// PointerDown and PointerClick), making the check far more reliable than querying
			// velocity at click time.
			if (m_toggleButton != null && m_toggleButton.ScrollVelocitySqrMagAtPointerDown > 2500f)
				return;

			if (m_isOpen)
				ClosePopup();
			else
				OpenPopup();
		}

		private void OpenPopup()
		{
			if (m_activePopup != null || m_popupMenuPrefab == null)
				return;

			var options = new UiPopup.Options
			{
				AnchorElement = (RectTransform)transform,
				CloseOnItemClick = true,
				AllowOutsideTap = true,
				MaxHeight = m_maxPopupHeight,
				Offset = m_offset,
			};

			// Let subclasses (and this class) fill items and wire callbacks before we show.
			options.OnItemClicked = (go, index) => OnDropdownValueChanged(index);
			options.OnClose = OnPopupClosed;

			PopulatePopup(options);

			// Wrap OnItemAdded to also apply selection visual.
			var subclassOnItemAdded = options.OnItemAdded;
			options.OnItemAdded = (go, index) =>
			{
				subclassOnItemAdded?.Invoke(go, index);
				ApplyItemSelectionVisual(go, index == m_selectedIndex);
			};

			m_activePopup = UiMain.Instance.ShowPopupMenu(options);
			m_isOpen = true;
			OnStatusChanged(true);
		}

		private void ClosePopup()
		{
			if (!m_isOpen)
				return;

			if (m_activePopup != null)
			{
				m_activePopup.Hide();
				m_activePopup = null;
			}

			m_isOpen = false;
			OnStatusChanged(false);
		}

		private void OnPopupClosed()
		{
			m_activePopup = null;
			if (m_isOpen)
			{
				m_isOpen = false;
				OnStatusChanged(false);
			}
		}
	}
}

