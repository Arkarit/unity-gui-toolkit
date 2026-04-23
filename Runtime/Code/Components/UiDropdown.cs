using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	/// <summary>
	/// Reusable base class for TMP_Dropdown-based components.
	///
	/// Responsibilities:
	/// - Populates the dropdown via the virtual <see cref="PopulateDropdown"/>.
	/// - Ensures the popup Canvas renders on top of all other Override-Sorting canvases
	///   (TMP_Dropdown 3.0.7 hardcodes sortingOrder=30000; we bump it to <see cref="short.MaxValue"/>).
	/// - Fires <see cref="EvOnDropdownValueChanged"/> and calls the virtual
	///   <see cref="OnDropdownValueChanged"/> when the user picks an item.
	/// - Detects open/close transitions and fires <see cref="EvOnStatusChanged"/> and
	///   calls the virtual <see cref="OnStatusChanged"/>.
	/// - Plays an optional <see cref="UiSimpleAnimationBase"/> forwards on open and
	///   backwards on close.
	///
	/// Derive from this class and override the virtual methods as needed.
	/// </summary>
	[RequireComponent(typeof(TMP_Dropdown))]
	public class UiDropdown : UiThing, IPointerClickHandler
	{
		[Tooltip("Optional animation played forwards when the dropdown opens and backwards when it closes.")]
		[SerializeField][Optional] protected UiSimpleAnimationBase m_statusAnimation;

		[Tooltip("Optional hover animation. When the popup is open the blocker canvas swallows pointer-exit " +
		         "events, which would otherwise reset the hover animation. Assign it here so UiDropdown can " +
		         "keep it visible while the popup is open and reset it cleanly when it closes.")]
		[SerializeField][Optional] private UiSimpleAnimationMouseOver m_hoverAnimation;

		public CEvent<int> EvOnDropdownValueChanged = new();
		public CEvent<bool> EvOnStatusChanged = new();

		protected TMP_Dropdown m_dropdown;
		private bool m_isOpen;

		protected override void Awake()
		{
			base.Awake();
			m_dropdown = GetComponent<TMP_Dropdown>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			PopulateDropdown();
			m_dropdown.onValueChanged.AddListener(OnDropdownValueChangedInternal);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_dropdown.onValueChanged.RemoveListener(OnDropdownValueChangedInternal);
			if (m_isOpen)
			{
				m_isOpen = false;
				OnStatusChanged(false);
			}
		}

		/// <summary>
		/// Override to fill the dropdown with options. Called in OnEnable.
		/// </summary>
		protected virtual void PopulateDropdown() { }

		/// <summary>
		/// Called when the user selects an item. Also fires <see cref="EvOnDropdownValueChanged"/>.
		/// </summary>
		protected virtual void OnDropdownValueChanged( int _index )
		{
			EvOnDropdownValueChanged.Invoke(_index);
		}

		/// <summary>
		/// Called when the popup opens (_isOpen=true) or closes (_isOpen=false).
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
					// The blocker canvas created by TMP_Dropdown swallows pointer-exit events, so
					// OnPointerExit already fired and played the animation backwards. Pause the
					// mouse-over detection and force the animation forwards (hover visible).
					m_hoverAnimation.PauseMouseOverAnimation = true;
					m_hoverAnimation.Play(false);
				}
				else
				{
					// Popup closed: reset the hover animation to the off-state and re-enable
					// mouse-over detection so normal hover works again.
					m_hoverAnimation.Reset();
					m_hoverAnimation.PauseMouseOverAnimation = false;
				}
			}
		}

		/// <summary>
		/// TMP_Dropdown.OnPointerClick (earlier in component order) already called Show() by the
		/// time this runs. We bump the popup Canvas sortingOrder to the maximum value so it always
		/// renders above any Override-Sorting Canvas in the scene, then detect the open/close transition.
		/// </summary>
		public void OnPointerClick( PointerEventData _ )
		{
			Transform popup = m_dropdown.transform.Find("Dropdown List");
			bool isNowOpen = popup != null;

			if (isNowOpen)
				EnsurePopupOnTop(popup);

			if (isNowOpen == m_isOpen)
				return;

			m_isOpen = isNowOpen;
			OnStatusChanged(isNowOpen);

			if (isNowOpen)
			{
				var watcher = popup.gameObject.AddComponent<DropdownListWatcher>();
				watcher.Init(OnDropdownClosed);
			}
		}

		protected void OnDropdownValueChangedInternal( int _index )
		{
			OnDropdownValueChanged(_index);
		}

		private void OnDropdownClosed()
		{
			if (!m_isOpen)
				return;

			m_isOpen = false;
			OnStatusChanged(false);
		}

		private static void EnsurePopupOnTop( Transform _popup )
		{
			Canvas canvas = _popup.GetComponent<Canvas>();
			if (canvas != null)
				canvas.sortingOrder = short.MaxValue;
		}

		/// <summary>
		/// Monitors the "Dropdown List" GameObject and notifies the parent <see cref="UiDropdown"/>
		/// when it is destroyed (i.e. the dropdown closes).
		/// </summary>
		private class DropdownListWatcher : MonoBehaviour
		{
			private Action m_onDestroyed;

			public void Init( Action _onDestroyed ) => m_onDestroyed = _onDestroyed;

			private void OnDestroy() => m_onDestroyed?.Invoke();
		}
	}
}
