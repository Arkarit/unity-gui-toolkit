using System.Collections;
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
	public class UiThing : MonoBehaviour, IEventSystemHandler
	{
		private static int s_layer = -1;
		[SerializeField] private bool m_enabled = true;

		/// Override and return false here if you don't want to receive events when currently not active.
		protected virtual bool ReceiveEventsWhenDisabled => true;

		/// Override and return true here if you need the OnLanguageChanged() callback
		protected virtual bool NeedsLanguageChangeCallback => false;

		protected virtual bool IsEnableable => false;

		/// Override to add your event listeners.
		protected virtual void AddEventListeners() {}

		/// Override to remove your event listeners.
		protected virtual void RemoveEventListeners() {}

		private bool m_eventListenersAdded = false;

		public RectTransform RectTransform => transform as RectTransform;

		public bool Enabled
		{
			get
			{
				return m_enabled;
			}
			set
			{
				if (m_enabled == value)
					return;
				m_enabled = value;
				OnEnabledChanged(m_enabled);

				UiThing[] childComponents = GetComponentsInChildren<UiThing>();

				// We can no call 'Enabled' recursively - otherwise every called child would call recursively too
				foreach (var childComponent in childComponents)
				{
					if (!childComponent.IsEnableable)
						continue;
					if (childComponent.m_enabled != value)
					{
						childComponent.m_enabled = value;
						childComponent.OnEnabledChanged(m_enabled);
					}
				}
			}
		}

		protected virtual void OnEnabledChanged(bool _enabled) {}

		protected virtual void OnLanguageChanged( string _languageId ){}


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

		/// Translation functions.
		/// Note that the convenient but weird "_" name is standard in gettext/po/pot environment, so don't blame me :-P
		protected string _(string _s)
		{
			return gettext(_s);
		}
		protected string gettext(string _s)
		{
			return LocaManager.Instance.Translate(_s);
		}
		protected string _n(string _singular, string _plural, int _n)
		{
			return ngettext(_singular, _plural, _n);
		}
		protected string ngettext(string _singular, string _plural, int _n)
		{
			return LocaManager.Instance.Translate(_singular, _plural, _n);
		}
		/// Not translated, only for POT creation
		protected static string __(string _s)
		{
			return _s;
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
		}

		/// Installs event listeners, if not ReceiveEventsWhenDisabled
		protected virtual void OnEnable()
		{
			if (NeedsLanguageChangeCallback)
				UiEvents.OnLanguageChanged.AddListener(OnLanguageChanged);

			if (!ReceiveEventsWhenDisabled && !m_eventListenersAdded)
			{
				AddEventListeners();
				m_eventListenersAdded = true;
			}
		}

		/// Removes event listeners, if not ReceiveEventsWhenDisabled
		protected virtual void OnDisable()
		{
			if (NeedsLanguageChangeCallback)
				UiEvents.OnLanguageChanged.RemoveListener(OnLanguageChanged);

			if (!ReceiveEventsWhenDisabled && m_eventListenersAdded)
			{
				RemoveEventListeners();
				m_eventListenersAdded = false;
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
