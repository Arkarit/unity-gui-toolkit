using System;
using UnityEngine;
using UnityEngine.Events;
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
		[SerializeField]
		protected EUiLayerDefinition m_layer = EUiLayerDefinition.Dialog;
		[HideInInspector]
		[SerializeField]
		private int m_lastSiblingIndex = -1;

		private Canvas m_canvas;
		
		public Canvas Canvas {
			get
			{
				if (m_canvas == null)
					m_canvas = GetComponent<Canvas>();
				return m_canvas;
			}
		}

		public EUiLayerDefinition Layer => m_layer;

		public void InitView(RenderMode _renderMode, Camera _camera, float _planeDistance, int _orderInLayer)
		{
			Canvas.renderMode = _renderMode;
			Canvas.worldCamera = _camera;
			Canvas.planeDistance = _planeDistance;
			Canvas.sortingOrder = _orderInLayer;
		}

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			base.Show(_instant, _onFinish);
		}

		public void ShowTopmost( bool _instant = false, Action _onFinish = null )
		{
			base.Show(_instant, _onFinish);
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