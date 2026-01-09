using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Extended animation interface for views that participate in a navigation stack.
	/// Adds stack animation configuration on top of panel show/hide.
	/// </summary>
	public interface IShowHideViewAnimation : IShowHidePanelAnimation
	{
		/// <summary>
		/// Configure the stack transition type and curve before playing.
		/// </summary>
		/// <param name="_stackAnimationType">Type of the stack transition (e.g. push, pop, modal, etc.).</param>
		/// <param name="_backwards">If true, configures the reverse direction (e.g. pop instead of push).</param>
		/// <param name="_animationCurve">Optional curve used by the animation implementation.</param>
		void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards, AnimationCurve _animationCurve );
	}

	/// <summary>
	/// Top-level UI container built on UiPanel. Provides:
	/// - Canvas, CanvasScaler and Raycaster requirements (2D UI rendering).
	/// - Layering and fullscreen-occlusion behavior across views.
	/// - Optional use of a global CanvasScaler template from configuration.
	/// - Integration with UiMain navigation (push/pop, bring-to-front).
	/// - Auto-destroy or pooling on hide, configurable per view.
	///
	/// Notes:
	/// - Show/Hide raise a global "fullscreen view" event if this view is fullscreen.
	///   Other views can hide themselves when occluded.
	/// - The occlusion logic relies on layer ordering: ensure EUiLayerDefinition
	///   reflects your intended z-order semantics.
	/// - Canvas and CanvasScaler are lazily fetched and cached per instance.
	/// </summary>
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasScaler))]
	[RequireComponent(typeof(GraphicRaycaster))]
	public class UiView : UiPanel
	{
		[Tooltip("The layer in which the view is shown.")]
		[SerializeField] protected EUiLayerDefinition m_layer = EUiLayerDefinition.Dialog;

		[Tooltip("When full screen, all other layers below are hidden.")]
		[SerializeField] protected bool m_isFullScreen;

		[Tooltip("Dialog is hidden when occluded by a full screen dialog.")]
		[SerializeField] protected bool m_hideOnOccludedByFullScreen = true;

		[Tooltip("Dialog is hidden instantly when occluded by a full screen dialog.")]
		[SerializeField] protected bool m_hideOnOccludedByFullScreenInstant;

		[Tooltip("Uses the global canvas scaler template in Ui toolkit configuration (if it is set).")]
		[SerializeField] protected bool m_useGlobalCanvasScalerTemplate = true;

		[Tooltip("When the view is closed via Hide(), the view is automatically destroyed or pooled.")]
		[SerializeField] protected bool m_autoDestroyOnHide = true;

		[Tooltip("When checked, the dialog is pooled instead of destroyed.")]
		[SerializeField] protected bool m_poolable = true;

		[Tooltip("Most UiViews need one or more close buttons. You can optionally define those here.")]
		[SerializeField] protected Button[] m_closeButtons = new Button[0];

		private UiModal m_uiModal;
		private bool m_uiModalChecked;
		private Canvas m_canvas;
		private CanvasScaler m_canvasScaler;
		private bool m_temporarilyHidden;
		private bool m_temporarilyHiddenAutoDestroyOnHide;

		/// <summary>Controls whether the view destroys (or pools) itself after Hide().</summary>
		public override bool AutoDestroyOnHide => m_autoDestroyOnHide;

		/// <summary>If true, the view returns to pool instead of being destroyed.</summary>
		public override bool Poolable => m_poolable;

		/// <summary>Expose destroy/pooling fields in inspectors.</summary>
		public override bool ShowDestroyFieldsInInspector => true;

		/// <summary>
		/// Cached Canvas component (required). Lazily fetched.
		/// </summary>
		public Canvas Canvas
		{
			get
			{
				if (m_canvas == null)
					m_canvas = GetComponent<Canvas>();
				return m_canvas;
			}
		}

		/// <summary>
		/// Cached CanvasScaler component (required). Lazily fetched.
		/// </summary>
		public CanvasScaler CanvasScaler
		{
			get
			{
				if (m_canvasScaler == null)
					m_canvasScaler = GetComponent<CanvasScaler>();
				return m_canvasScaler;
			}
		}

		/// <summary>
		/// The logical UI layer for this view. Used to resolve occlusion.
		/// </summary>
		public EUiLayerDefinition Layer => m_layer;

		/// <summary>
		/// Optional UiModal helper on the same GameObject (lazy discovery).
		/// Provides OnClickCatcher for outside-click handling when present.
		/// </summary>
		protected UiModal UiModal
		{
			get
			{
				if (!m_uiModalChecked)
				{
					m_uiModalChecked = true;
					m_uiModal = GetComponent<UiModal>();
				}
				return m_uiModal;
			}
		}

		/// <summary>
		/// Forwarded access to UiModal.OnClickCatcher when UiModal is present.
		/// No-ops when UiModal is missing.
		/// </summary>
		protected Action OnClickCatcher
		{
			get => UiModal == null ? null : UiModal.OnClickCatcher;
			set
			{
				if (UiModal == null)
					return;
				UiModal.OnClickCatcher = value;
			}
		}

		/// <summary>
		/// Initialize Canvas configuration (render mode, camera, plane distance, sorting order).
		/// Call this after instantiation and before showing the view.
		/// </summary>
		public void InitView( RenderMode _renderMode, Camera _camera, float _planeDistance, int _orderInLayer )
		{
			Canvas.renderMode = _renderMode;
			Canvas.worldCamera = _camera;
			Canvas.planeDistance = _planeDistance;
			Canvas.sortingOrder = _orderInLayer;
		}

		/// <summary>
		/// OnEnable: optionally apply a global CanvasScaler template on the next frame.
		/// </summary>
		protected override void OnEnable()
		{
			base.OnEnable();

			var globalCanvasScalerTemplate = UiToolkitConfiguration.Instance.GlobalCanvasScalerTemplate;
			if (globalCanvasScalerTemplate && m_useGlobalCanvasScalerTemplate)
				StartCoroutine(ApplyGlobalCanvasScalerTemplateDelayed(globalCanvasScalerTemplate));
		}

		/// <summary>
		/// Awake: subscribe to fullscreen view events, then perform base setup.
		/// </summary>
		protected override void Awake()
		{
			UiEventDefinitions.EvFullScreenView.AddListener(OnEvFullscreenView);
			base.Awake();
		}

		/// <summary>
		/// OnDestroy: unsubscribe from fullscreen events and let base clean up.
		/// </summary>
		protected override void OnDestroy()
		{
			UiEventDefinitions.EvFullScreenView.RemoveListener(OnEvFullscreenView);
			base.OnDestroy();
		}

		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			if (m_closeButtons == null)
				return;

			foreach (var closeButton in m_closeButtons)
				closeButton.onClick.AddListener(DoHide);
		}

		protected override void RemoveEventListeners()
		{
			base.RemoveEventListeners();
			if (m_closeButtons == null)
				return;

			foreach (var closeButton in m_closeButtons)
				closeButton.onClick.RemoveListener(DoHide);
		}

		private void DoHide() => Hide();

		/// <summary>
		/// React to other fullscreen views showing/hiding.
		/// Hides this view while occluded, then restores it when the occlusion ends.
		/// </summary>
		protected virtual void OnEvFullscreenView( UiView _view, bool _show )
		{
			if (!m_hideOnOccludedByFullScreen)
				return;

			if (_view == this)
				return;

			// Only react if the other view's layer should occlude this view.
			if (_view.Layer > Layer)
				return;

			// If our current visibility already matches the requested target, ignore.
			if (_show != Visible)
				return;

			// When another fullscreen view shows, temporarily hide this one.
			if (_show)
			{
				if (m_temporarilyHidden)
					return;

				m_temporarilyHidden = true;
				m_temporarilyHiddenAutoDestroyOnHide = m_autoDestroyOnHide;
				m_autoDestroyOnHide = false;
				Hide(m_hideOnOccludedByFullScreenInstant);
				return;
			}

			// When the other fullscreen view hides, restore this one.
			if (!m_temporarilyHidden)
				return;

			m_temporarilyHidden = false;
			m_autoDestroyOnHide = m_temporarilyHiddenAutoDestroyOnHide;
			Show(m_hideOnOccludedByFullScreenInstant);
		}

		/// <summary>
		/// Apply a global CanvasScaler template one frame later to ensure the instance is fully initialized.
		/// </summary>
		protected IEnumerator ApplyGlobalCanvasScalerTemplateDelayed( CanvasScaler _template )
		{
			yield return 0;
			_template.CopyTo(CanvasScaler);
		}

		/// <summary>
		/// Show override: broadcasts fullscreen visibility if applicable, then proceeds with base Show.
		/// </summary>
		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			if (m_isFullScreen)
				UiEventDefinitions.EvFullScreenView.Invoke(this, true);

			base.Show(_instant, _onFinish);
		}

		/// <summary>
		/// Hide override: broadcasts fullscreen visibility if applicable, then proceeds with base Hide.
		/// </summary>
		public override void Hide( bool _instant = false, Action _onFinish = null )
		{
			if (m_isFullScreen)
				UiEventDefinitions.EvFullScreenView.Invoke(this, false);

			base.Hide(_instant, _onFinish);
		}

		/// <summary>
		/// Show this view and move it to the top within its layer.
		/// </summary>
		public void ShowTopmost( bool _instant = false, Action _onFinish = null )
		{
			Show(_instant, _onFinish);
			UiMain.Instance.SetAsLastSiblingOfLayer(this);
		}

		/// <summary>
		/// Push this view via UiMain navigation. The previous top may be hidden.
		/// </summary>
		public virtual void NavigationPush( bool _instant = false, Action _onFinishHide = null, Action _onFinishShow = null )
		{
			UiMain.Instance.NavigationPush(this, _instant, _onFinishHide, _onFinishShow);
		}

		/// <summary>
		/// Pop this view via UiMain navigation. Asserts this is the current top.
		/// </summary>
		public virtual void NavigationPop( bool _instant = false, int _skip = 0, Action _onFinishHide = null, Action _onFinishShow = null )
		{
			Debug.Assert(UiMain.Instance.Peek() == this, "Attempting to pop wrong dialog");
			UiMain.Instance.NavigationPop(_skip, _instant, _onFinishHide, _onFinishShow);
		}

		/// <summary>
		/// Configure stack animation parameters on the current simple animation, if present.
		/// </summary>
		public void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards, AnimationCurve _animationCurve )
		{
			if (SimpleShowHideAnimation == null || !(SimpleShowHideAnimation is IShowHideViewAnimation))
				return;

			((IShowHideViewAnimation)SimpleShowHideAnimation).SetStackAnimationType(_stackAnimationType, _backwards, _animationCurve);
		}
	}
}
