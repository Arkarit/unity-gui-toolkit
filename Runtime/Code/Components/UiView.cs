using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public interface IShowHideViewAnimation : IShowHidePanelAnimation
	{
		void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards, AnimationCurve _animationCurve );
	}

	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasScaler))]
	[RequireComponent(typeof(GraphicRaycaster))]
	public class UiView : UiPanel
	{
		[SerializeField] protected EUiLayerDefinition m_layer = EUiLayerDefinition.Dialog;
		[SerializeField] protected bool m_isFullScreen;
		[Tooltip("Uses the global canvas scaler template in Ui toolkit configuration (if it is set)")]
		[SerializeField] protected bool m_useGlobalCanvasScalerTemplate = true;

		private UiModal m_uiModal;
		private bool m_uiModalChecked;
		private Canvas m_canvas;
		private CanvasScaler m_canvasScaler;
		
		public Canvas Canvas {
			get
			{
				if (m_canvas == null)
					m_canvas = GetComponent<Canvas>();
				return m_canvas;
			}
		}

		public CanvasScaler CanvasScaler {
			get
			{
				if (m_canvasScaler == null)
					m_canvasScaler = GetComponent<CanvasScaler>();
				return m_canvasScaler;
			}
		}

		public EUiLayerDefinition Layer => m_layer;

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

		public void InitView(RenderMode _renderMode, Camera _camera, float _planeDistance, int _orderInLayer)
		{
			Canvas.renderMode = _renderMode;

			Canvas.worldCamera = _camera;
			Canvas.planeDistance = _planeDistance;
			Canvas.sortingOrder = _orderInLayer;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			var globalCanvasScalerTemplate = UiToolkitConfiguration.Instance.m_globalCanvasScalerTemplate;
			if (globalCanvasScalerTemplate && m_useGlobalCanvasScalerTemplate)
				StartCoroutine(ApplyGlobalCanvasScalerTemplateDelayed(globalCanvasScalerTemplate));
		}
		
		protected IEnumerator ApplyGlobalCanvasScalerTemplateDelayed(CanvasScaler _template)
		{
			yield return 0;
			_template.CopyTo(CanvasScaler);
		}

		public override void Show(bool _instant = false, Action _onFinish = null)
		{
			base.Show(_instant, ()=>
			{
				if (m_isFullScreen)
					UiMain.Instance.SetFullScreenView(this);
				_onFinish?.Invoke();
			});
		}

		public override void Hide(bool _instant = false, Action _onFinish = null)
		{
			UiMain.Instance.SetFullScreenView(null);
			base.Hide(_instant, _onFinish);
		}

		public void ShowTopmost( bool _instant = false, Action _onFinish = null )
		{
			Show(_instant, _onFinish);
			UiMain.Instance.SetAsLastSiblingOfLayer(this);
		}

		public virtual void NavigationPush(bool _instant = false, Action _onFinishHide = null, Action _onFinishShow = null)
		{
			UiMain.Instance.NavigationPush(this, _instant, _onFinishHide, _onFinishShow);
		}

		public virtual void NavigationPop(bool _instant = false, int _skip = 0, Action _onFinishHide = null, Action _onFinishShow = null)
		{
			Debug.Assert(UiMain.Instance.Peek() == this, "Attempting to pop wrong dialog");
			UiMain.Instance.NavigationPop(_skip, _instant, _onFinishHide, _onFinishShow);
		}

		public void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards, AnimationCurve _animationCurve )
		{
			if (SimpleShowHideAnimation == null || !(SimpleShowHideAnimation is IShowHideViewAnimation))
				return;

			((IShowHideViewAnimation)SimpleShowHideAnimation).SetStackAnimationType(_stackAnimationType, _backwards, _animationCurve);
		}

	}
}