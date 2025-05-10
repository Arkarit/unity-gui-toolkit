using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	/// \brief Basic UI class
	///
	/// Basic UI class which defines virtual methods for the most common
	/// Unity functions Awake(), OnEnable(), OnDisable() and OnDestroy().
	/// \attention Be sure to always call the base class methods if you inherit from UiThing
	/// 
	/// Additionally, it offers event handling plus some convenience functions.

	[RequireComponent(typeof(RectTransform))]
	public class UiThing : LocaMonoBehaviour, IEventSystemHandler, IEnableableInHierarchy
	{
		private static int s_layer = -1;
		[HideInInspector] // Only editable in custom editors
		[SerializeField] private bool m_enabledInHierarchy = true;

		/// Override and return false here if you don't want to receive events when currently not active.
		protected virtual bool ReceiveEventsWhenDisabled => true;

		/// Override and return true here if you need the OnLanguageChanged() callback
		protected virtual bool NeedsLanguageChangeCallback => false;

		/// Override and return true here if you need the OnScreenOrientation() callback
		protected virtual bool NeedsOnScreenOrientationCallback => false;

		#region IEnableableInHierarchy
		/// Override and return true here if the component is hierarchically enableable
		public virtual bool IsEnableableInHierarchy => false;

		bool IEnableableInHierarchy.StoreEnabledInHierarchy 
		{ 
			get => m_enabledInHierarchy; 
			set => m_enabledInHierarchy = value; 
		}

		public bool EnabledInHierarchy
		{ 
			get => EnableableInHierarchyUtility.GetEnabledInHierarchy(this);
			set => EnableableInHierarchyUtility.SetEnabledInHierarchy(this, value); 
		}

		#endregion

		/// Override to add your event listeners.
		protected virtual void AddEventListeners() {}

		/// Override to remove your event listeners.
		protected virtual void RemoveEventListeners() {}

		protected readonly List<(UiButton button, UnityAction action)> m_buttonListeners = new();

		private bool m_eventListenersAdded = false;
		private bool m_isAwake = false;

		public RectTransform RectTransform => transform as RectTransform;

		IEnableableInHierarchy[] IEnableableInHierarchy.Children => GetComponentsInChildren<IEnableableInHierarchy>();
		public virtual void OnEnabledInHierarchyChanged(bool _enabled) {}

		protected virtual void OnLanguageChanged( string _languageId ){}
		protected virtual void OnScreenOrientationChanged( EScreenOrientation _oldScreenOrientation, EScreenOrientation _newScreenOrientation ){}


		/// Call this in Awake(), before calling base.Awake()!
		protected void AddOnEnableButtonListeners(params (UiButton button, UnityAction action)[] _listeners)
		{
			if (m_isAwake)
			{
				Debug.LogError("Call this in Awake(), before calling base.Awake() please!");
				return;
			}

			m_buttonListeners.AddRange(_listeners);
		}

		/// \brief Install Event handlers on disabled objects
		/// 
		/// Unity unfortunately has NO reliable "OnCreate" callback:<BR>
		/// Constructors should not be used according to Unity, and Awake() is only called on active game objects.<BR>
		/// So if you e.g. want to install event handlers on components, which are disabled on creation (e.g. to enable them on event),
		/// you're lost.<BR>
		/// In such cases, you have to call InitEvents() manually.
		public void InitEvents()
		{
			// When we are active, leave it up to the common Awake() etc.
			if (gameObject.activeInHierarchy && enabled)
				return;

			// only when we receive events while disabled, we initialize the events.
			if (ReceiveEventsWhenDisabled && !m_eventListenersAdded)
			{
				AddEventListeners();
				m_eventListenersAdded = true;
			}
		}

		protected virtual void Start() {}

		/// Installs event listeners, if ReceiveEventsWhenDisabled
		/// Also ensure that the game object is always in UI layer
		protected virtual void Awake()
		{
			if (s_layer == -1)
				s_layer = LayerMask.NameToLayer("UI");

			gameObject.layer = s_layer;

			if (ReceiveEventsWhenDisabled && !m_eventListenersAdded)
			{
				AddEventListeners();
				m_eventListenersAdded = true;
			}

			m_isAwake = true;
		}

		/// Installs event listeners, if not ReceiveEventsWhenDisabled
		protected virtual void OnEnable()
		{
			if (NeedsLanguageChangeCallback)
				UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);

			if (NeedsOnScreenOrientationCallback)
				UiEventDefinitions.EvScreenOrientationChange.AddListener(OnScreenOrientationChanged);

			if (!ReceiveEventsWhenDisabled && !m_eventListenersAdded)
			{
				AddEventListeners();
				m_eventListenersAdded = true;
			}

			foreach (var buttonListener in m_buttonListeners)
			{
				if (buttonListener.button == null || buttonListener.action == null)
					continue;

				buttonListener.button.OnClick.AddListener(buttonListener.action);
			}
		}

		/// Removes event listeners, if not ReceiveEventsWhenDisabled
		protected virtual void OnDisable()
		{
			if (NeedsLanguageChangeCallback)
				UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);

			if (NeedsOnScreenOrientationCallback)
				UiEventDefinitions.EvScreenOrientationChange.RemoveListener(OnScreenOrientationChanged);

			if (!ReceiveEventsWhenDisabled && m_eventListenersAdded)
			{
				RemoveEventListeners();
				m_eventListenersAdded = false;
			}

			foreach (var buttonListener in m_buttonListeners)
			{
				if (buttonListener.button == null || buttonListener.action == null)
					continue;

				buttonListener.button.OnClick.RemoveListener(buttonListener.action);
			}
		}

		/// Removes event listeners, if ReceiveEventsWhenDisabled
		protected virtual void OnDestroy()
		{
			if (ReceiveEventsWhenDisabled && m_eventListenersAdded)
			{
				RemoveEventListeners();
				m_eventListenersAdded = false;
			}
		}
	}
}
