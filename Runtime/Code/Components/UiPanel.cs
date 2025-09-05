using System;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// <summary>
	/// Interface for simple show/hide panel animations.
	/// Implement this on a MonoBehaviour attached to the same GameObject
	/// as UiPanel if you want animated transitions.
	/// </summary>
	public interface IShowHidePanelAnimation
	{
		/// <summary>
		/// Play the show/enter animation. Invoke the callback when finished.
		/// </summary>
		void ShowViewAnimation( UnityAction _onFinish = null );

		/// <summary>
		/// Play the hide/exit animation. Invoke the callback when finished.
		/// </summary>
		void HideViewAnimation( UnityAction _onFinish = null );

		/// <summary>
		/// Immediately stop the animation in a defined state.
		/// </summary>
		/// <param name="_visible">True to end in shown state, false to end hidden.</param>
		void StopViewAnimation( bool _visible );
	}

	/// <summary>
	/// Base panel view with show/hide lifecycle, optional animations, and events.
	///
	/// Features:
	/// - Show/Hide API with optional instant mode and completion callbacks.
	/// - Optional animation integration via IShowHidePanelAnimation found on the same GameObject.
	/// - Visibility events (begin/end show/hide) and destroyed event.
	/// - Optional auto-destroy or pool-return on hide.
	/// - Default scene visibility policy applied once at runtime.
	///
	/// Notes:
	/// - IsVisible and IsVisibleInHierarchy reflect GameObject active states.
	/// - The property Visible is the panel's intended visibility as managed by this class.
	/// - If you override lifecycle methods, always call base.
	/// </summary>
	public class UiPanel : UiThing, ISetDefaultSceneVisibility, IPoolable
	{
		/// <summary>
		/// Scene visibility policy that is applied once when the scene starts.
		/// </summary>
		[SerializeField] protected EDefaultSceneVisibility m_defaultSceneVisibility = EDefaultSceneVisibility.DontCare;

		/// <summary>
		/// Optional reference to a UiSimpleAnimationBase.
		/// If not assigned, a possible IShowHidePanelAnimation will be discovered on first use via GetComponents.
		/// </summary>
		[SerializeField][Optional] protected UiSimpleAnimationBase m_simpleShowHideAnimation = null;

		// --------------------------------------------------------------------
		// Events
		// --------------------------------------------------------------------

		/// <summary>Raised right before the show transition starts.</summary>
		public CEvent<UiPanel> EvOnBeginShow = new();

		/// <summary>Raised after the show transition completes (or instant show is done).</summary>
		public CEvent<UiPanel> EvOnEndShow = new();

		/// <summary>Raised right before the hide transition starts.</summary>
		public CEvent<UiPanel> EvOnBeginHide = new();

		/// <summary>Raised after the hide transition completes (or instant hide is done).</summary>
		public CEvent<UiPanel> EvOnEndHide = new();

		/// <summary>Raised when the panel is about to be destroyed (or returned to pool).</summary>
		public CEvent<UiPanel> EvOnDestroyed = new();

		// --------------------------------------------------------------------
		// Configuration toggles (override in subclasses)
		// --------------------------------------------------------------------

		/// <summary>
		/// If true, the panel destroys itself after hiding. If Poolable is true,
		/// it returns to the pool; otherwise the GameObject is destroyed.
		/// </summary>
		public virtual bool AutoDestroyOnHide => false;

		/// <summary>
		/// If true, the panel participates in pooling via UiPool.
		/// </summary>
		public virtual bool Poolable => false;

		/// <summary>
		/// If true, destroy-related controls are exposed in custom inspectors.
		/// </summary>
		public virtual bool ShowDestroyFieldsInInspector => false;

		// --------------------------------------------------------------------
		// Visibility state
		// --------------------------------------------------------------------

		/// <summary>
		/// True if the panel's GameObject is active (self, not hierarchy).
		/// </summary>
		public virtual bool IsVisible => gameObject.activeSelf;

		/// <summary>
		/// True if the panel is effectively active in the scene hierarchy.
		/// </summary>
		public virtual bool IsVisibleInHierarchy => gameObject.activeInHierarchy;

		/// <summary>
		/// Hook called when a show transition begins. Override for custom logic.
		/// </summary>
		public virtual void OnBeginShow() { }

		/// <summary>
		/// Hook called when a show transition ends. Override for custom logic.
		/// </summary>
		public virtual void OnEndShow() { }

		/// <summary>
		/// Hook called when a hide transition begins. Override for custom logic.
		/// </summary>
		public virtual void OnBeginHide() { }

		/// <summary>
		/// Hook called when a hide transition ends. Override for custom logic.
		/// </summary>
		public virtual void OnEndHide() { }

		protected IShowHidePanelAnimation m_showHideAnimation;
		private bool m_defaultSceneVisibilityApplied;
		private bool m_animationInitialized;
		private Action m_onShowHideFinishAction;

		/// <summary>
		/// Logical visibility flag managed by the panel API (SetVisible/Show/Hide).
		/// This may differ from activeSelf if the object was toggled externally.
		/// </summary>
		public bool Visible { get; private set; }

		/// <summary>
		/// Returns the concrete simple animation component if present.
		/// Null if m_showHideAnimation is not a UiSimpleAnimation.
		/// </summary>
		public UiSimpleAnimationBase SimpleShowHideAnimation
		{
			get
			{
				InitAnimationIfNecessary();
				if (m_showHideAnimation is UiSimpleAnimation)
					return (UiSimpleAnimation)m_showHideAnimation;
				return null;
			}
		}

		/// <summary>
		/// Unity Awake: initialize base and ensure an animation provider is cached if available.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();
			InitAnimationIfNecessary();
		}

		/// <summary>
		/// Called by pool when the instance is released back to the pool.
		/// Override to reset state.
		/// </summary>
		public virtual void OnPoolReleased() { }

		/// <summary>
		/// Called by pool when the instance is created or fetched.
		/// Override to initialize state.
		/// </summary>
		public virtual void OnPoolCreated() { }

		/// <summary>
		/// Show the panel. Optionally instant, with an optional on-finish callback.
		/// If no animation is available, forces instant mode.
		/// </summary>
		public virtual void Show( bool _instant = false, Action _onFinish = null )
		{
			if (SimpleShowHideAnimation == null)
				_instant = true;

			gameObject.SetActive(true);

			if (Visible && !_instant)
				return;

			OnBeginShow();
			EvOnBeginShow.Invoke(this);
			Visible = true;

			if (_instant)
			{
				if (SimpleShowHideAnimation != null)
					SimpleShowHideAnimation.StopViewAnimation(true);

				OnEndShow();
				EvOnEndShow.Invoke(this);
				_onFinish?.Invoke();
				m_onShowHideFinishAction = null;
				return;
			}

			PlayShowHideAnimation(true, _onFinish);
		}

		/// <summary>
		/// Hide the panel. Optionally instant, with an optional on-finish callback.
		/// If no animation is available, forces instant mode.
		/// </summary>
		public virtual void Hide( bool _instant = false, Action _onFinish = null )
		{
			if (SimpleShowHideAnimation == null)
				_instant = true;

			if (!Visible && !_instant)
				return;

			OnBeginHide();
			EvOnBeginHide.Invoke(this);
			Visible = false;

			if (_instant)
			{
				gameObject.SetActive(false);

				if (SimpleShowHideAnimation != null)
					SimpleShowHideAnimation.StopViewAnimation(false);

				OnEndHide();
				EvOnEndHide.Invoke(this);
				_onFinish?.Invoke();

				m_onShowHideFinishAction = null;
				DestroyIfNecessary();
				return;
			}

			PlayShowHideAnimation(false, _onFinish);
		}

		/// <summary>
		/// Convenience method to set visibility by boolean.
		/// </summary>
		public void SetVisible( bool _visible, bool _instant = false, Action _onFinish = null )
		{
			if (_visible)
				Show(_instant, _onFinish);
			else
				Hide(_instant, _onFinish);
		}

		/// <summary>
		/// Apply default scene visibility policy once at runtime.
		/// </summary>
		public void SetDefaultSceneVisibility()
		{
			if (Application.isPlaying && (!m_defaultSceneVisibilityApplied))
			{
				m_defaultSceneVisibilityApplied = true;

				switch (m_defaultSceneVisibility)
				{
					default:
					case EDefaultSceneVisibility.DontCare:
						break;

					case EDefaultSceneVisibility.Visible:
						gameObject.SetActive(true);
						break;

					case EDefaultSceneVisibility.Invisible:
						gameObject.SetActive(false);
						break;

					case EDefaultSceneVisibility.VisibleInDevBuild:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
						gameObject.SetActive(true);
#else
                            gameObject.SetActive(false);
#endif
						break;

					case EDefaultSceneVisibility.VisibleWhen_DEFAULT_SCENE_VISIBLE_defined:
#if DEFAULT_SCENE_VISIBLE
						gameObject.SetActive(true);
#else
                            gameObject.SetActive(false);
#endif
						break;
				}
			}
		}

		/// <summary>
		/// Ensure an animation provider is cached. If m_showHideAnimation is not
		/// assigned, scans the attached MonoBehaviours and picks the first one
		/// implementing IShowHidePanelAnimation.
		/// </summary>
		private void InitAnimationIfNecessary()
		{
			if (m_animationInitialized)
				return;

			m_animationInitialized = true;

			if (m_simpleShowHideAnimation != null)
			{
				m_showHideAnimation = m_simpleShowHideAnimation;
				return;
			}

			var components = GetComponents<MonoBehaviour>();
			foreach (var component in components)
			{
				if (component is IShowHidePanelAnimation)
				{
					m_showHideAnimation = (IShowHidePanelAnimation)component;
					break;
				}
			}
		}

		/// <summary>
		/// Internal helper to play show or hide animation and hook up completion.
		/// </summary>
		private void PlayShowHideAnimation( bool _show, Action _onFinish )
		{
			m_onShowHideFinishAction = _onFinish;

			// Ensure previous once-callback is cleared before wiring a new one.
			SimpleShowHideAnimation.OnFinishOnce.RemoveListener(HideViewCallback);
			SimpleShowHideAnimation.OnFinishOnce.RemoveListener(ShowViewCallback);

			if (_show)
				SimpleShowHideAnimation.ShowViewAnimation(ShowViewCallback);
			else
				SimpleShowHideAnimation.HideViewAnimation(HideViewCallback);
		}

		/// <summary>
		/// Called by animation after show finishes.
		/// </summary>
		private void ShowViewCallback()
		{
			OnEndShow();
			EvOnEndShow.Invoke(this);
			m_onShowHideFinishAction?.Invoke();
		}

		/// <summary>
		/// Called by animation after hide finishes.
		/// Deactivates the GameObject and triggers optional destruction.
		/// </summary>
		private void HideViewCallback()
		{
			gameObject.SetActive(false);

			OnEndHide();
			EvOnEndHide.Invoke(this);
			m_onShowHideFinishAction?.Invoke();
			m_onShowHideFinishAction = null;

			DestroyIfNecessary();
		}

		/// <summary>
		/// Destroy or pool-return if configured to do so after a hide.
		/// </summary>
		private void DestroyIfNecessary()
		{
			if (!AutoDestroyOnHide)
				return;

			if (Poolable)
			{
				EvOnDestroyed.Invoke(this);
				EvOnDestroyed.RemoveAllListeners();

				if (UiMain.IsAwake)
					UiPool.Instance.DoDestroy(this);
			}
			else
				gameObject.SafeDestroy();
		}

		/// <summary>
		/// Unity OnDestroy: forwards EvOnDestroyed and lets base clean up.
		/// </summary>
		protected override void OnDestroy()
		{
			EvOnDestroyed.Invoke(this);
			base.OnDestroy();
		}
	}
}
