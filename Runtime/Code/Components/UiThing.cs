using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	/// <summary>
	/// Basic UI class providing a dependable Unity lifecycle scaffold and
	/// opt-in event subscriptions. Inherits LocaMonoBehaviour so gettext helpers
	/// are available to derived classes.
	///
	/// Responsibilities:
	/// - Defines virtual lifecycle hooks (Awake, OnEnable, OnDisable, OnDestroy, Start).
	/// - Provides an opt-in pattern for receiving events while disabled.
	/// - Integrates optional UiEventDefinitions callbacks (language, resolution).
	/// - Centralizes UI-layer assignment and requires a RectTransform.
	/// - Adds a small convenience mechanism for wiring UiButton click listeners.
	///
	/// Contract:
	/// - If you override lifecycle methods, always call the base implementation.
	/// - If you need to receive events while the component is disabled, override
	///   ReceiveEventsWhenDisabled and return true.
	/// - If you need language or resolution callbacks, override the corresponding
	///   Needs... properties and return true.
	/// </summary>
	[RequireComponent(typeof(RectTransform))]
	public class UiThing : LocaMonoBehaviour, IEventSystemHandler, IEnableableInHierarchy
	{
		// Cached UI layer index. Lazily resolved once per domain reload.
		private static int s_layer = -1;

		[HideInInspector] // Only editable via custom inspectors or helpers.
		[SerializeField] private bool m_enabledInHierarchy = true;

		/// <summary>
		/// If true, event listeners are installed in Awake even when the component
		/// is disabled, allowing it to react to external events while inactive.
		/// Default: true.
		/// </summary>
		protected virtual bool ReceiveEventsWhenDisabled => true;

		/// <summary>
		/// If true, subscribes to UiEventDefinitions.EvLanguageChanged on enable.
		/// </summary>
		protected virtual bool NeedsLanguageChangeCallback => false;

		/// <summary>
		/// If true, subscribes to UiEventDefinitions.EvScreenresolutionChange on enable.
		/// </summary>
		protected virtual bool NeedsOnScreenResolutionChangedCallback => false;

		#region IEnableableInHierarchy

		/// <summary>
		/// If true, this component participates in hierarchical enable/disable
		/// logic managed by EnableableInHierarchyUtility.
		/// </summary>
		public virtual bool IsEnableableInHierarchy => false;

		/// <summary>
		/// Storage for the effective enabled state used by IEnableableInHierarchy.
		/// This is the backing field that the utility reads and writes.
		/// </summary>
		bool IEnableableInHierarchy.StoreEnabledInHierarchy
		{
			get => m_enabledInHierarchy;
			set => m_enabledInHierarchy = value;
		}

		/// <summary>
		/// Gets or sets the effective enabled state in the hierarchy as computed
		/// by EnableableInHierarchyUtility.
		/// </summary>
		public bool EnabledInHierarchy
		{
			get => EnableableInHierarchyUtility.GetEnabledInHierarchy(this);
			set => EnableableInHierarchyUtility.SetEnabledInHierarchy(this, value);
		}

		/// <summary>
		/// Enumerates children that also implement IEnableableInHierarchy.
		/// </summary>
		IEnableableInHierarchy[] IEnableableInHierarchy.Children => GetComponentsInChildren<IEnableableInHierarchy>();

		/// <summary>
		/// Called when the effective hierarchical enabled state changes.
		/// Override to react to changes (e.g., show/hide view content).
		/// </summary>
		public virtual void OnEnabledInHierarchyChanged( bool _enabled ) { }

		#endregion

		/// <summary>
		/// Override to add your custom event listeners (e.g., bus subscriptions).
		/// This is invoked according to ReceiveEventsWhenDisabled and lifecycle.
		/// </summary>
		protected virtual void AddEventListeners() { }

		/// <summary>
		/// Override to remove your custom event listeners.
		/// Must be symmetrical to AddEventListeners to prevent leaks.
		/// </summary>
		protected virtual void RemoveEventListeners() { }

		/// <summary>
		/// Convenience container for UiButton -> UnityAction pairs to be wired
		/// automatically on enable and unwired on disable.
		/// </summary>
		protected readonly List<(UiButton button, UnityAction action)> m_buttonListeners = new();

		private bool m_eventListenersAdded = false;
		private bool m_isAwake = false;
		private RectTransform m_rectTransform = null;

		/// <summary>
		/// Convenience accessor for the required RectTransform.
		/// </summary>
		public RectTransform RectTransform
		{
			get
			{
				if (m_rectTransform == null)
					m_rectTransform = transform as RectTransform;

				return m_rectTransform;
			}
		}

		/// <summary>
		/// Optional callback when application language changes.
		/// Only active if NeedsLanguageChangeCallback returns true.
		/// </summary>
		protected virtual void OnLanguageChanged( string _languageId ) { }

		/// <summary>
		/// Optional callback when screen resolution changes.
		/// Only active if NeedsOnScreenResolutionCallback returns true.
		/// </summary>
		protected virtual void OnScreenResolutionChanged( ScreenResolution _oldScreenResolution, ScreenResolution _newScreenResolution ) { }

		/// <summary>
		/// Registers UiButton click listeners to be installed later in OnEnable.
		/// Call this in your Awake() BEFORE base.Awake().
		/// </summary>
		/// <param name="_listeners">Pairs of (UiButton, UnityAction). UiButton is optional (can be null)</param>
		protected void AddOnEnableButtonListeners( params (UiButton button, UnityAction action)[] _listeners )
		{
			if (m_isAwake)
			{
				UiLog.LogError("Call this in Awake(), before calling base.Awake() please!");
				return;
			}

			m_buttonListeners.AddRange(_listeners);
		}

		/// <summary>
		/// Initializes event listeners even when the object is inactive.
		/// Use this if you need to receive events while disabled and the
		/// standard Awake() path is not guaranteed to run.
		///
		/// Unity specifics:
		/// - Constructors should not be used for initialization.
		/// - Awake() is called only on active objects at load time.
		/// </summary>
		public void InitEvents()
		{
			// If we are active, rely on regular lifecycle.
			if (gameObject.activeInHierarchy && enabled)
				return;

			// Only initialize when allowed to receive while disabled.
			if (ReceiveEventsWhenDisabled && !m_eventListenersAdded)
			{
				AddEventListeners();
				m_eventListenersAdded = true;
			}
		}

		/// <summary>
		/// Optional Unity Start hook for derived classes.
		/// </summary>
		protected virtual void Start() { }

		/// <summary>
		/// Sets the UI layer and installs listeners if ReceiveEventsWhenDisabled.
		/// This is the earliest safe point for engine-side setup.
		/// </summary>
		protected virtual void Awake()
		{
			if (s_layer == -1)
				s_layer = LayerMask.NameToLayer("UI");

			if (s_layer == -1)
			{
				UiLog.LogError($"Standard 'UI' Layer not present - please check your project setup. Falling back to Layer {gameObject.layer} (as set in Game Object)");
				s_layer = gameObject.layer;
			}
			else
			{
				gameObject.layer = s_layer;
			}

			if (ReceiveEventsWhenDisabled && !m_eventListenersAdded)
			{
				AddEventListeners();
				m_eventListenersAdded = true;
			}

			m_isAwake = true;
		}

		/// <summary>
		/// Subscribes optional UiEventDefinitions callbacks and installs
		/// listeners if we do not receive while disabled. Also wires UiButton clicks.
		/// </summary>
		protected virtual void OnEnable()
		{
			if (NeedsLanguageChangeCallback)
				UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);

			if (NeedsOnScreenResolutionChangedCallback)
				UiEventDefinitions.EvScreenResolutionChange.AddListener(OnScreenResolutionChanged);

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

		/// <summary>
		/// Unsubscribes optional UiEventDefinitions callbacks, removes listeners
		/// if we only receive while enabled, and unwires UiButton clicks.
		/// </summary>
		protected virtual void OnDisable()
		{
			if (NeedsLanguageChangeCallback)
				UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);

			if (NeedsOnScreenResolutionChangedCallback)
				UiEventDefinitions.EvScreenResolutionChange.RemoveListener(OnScreenResolutionChanged);

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

		/// <summary>
		/// Final cleanup for the receive-while-disabled case.
		/// </summary>
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
