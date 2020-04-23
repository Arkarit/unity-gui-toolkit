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

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			Init();
			base.Show(_instant, _onFinish);
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
			InitAnimation();
			if (m_showHideAnimation == null || !(m_showHideAnimation is IShowHideViewAnimation))
				return;

			((IShowHideViewAnimation)m_showHideAnimation).SetStackAnimationType(_stackAnimationType, _backwards, _animationCurve);
		}

		public override void Init()
		{
			base.Init();

			UiPanel[] panels = GetComponentsInChildren<UiPanel>();
			foreach (var panel in panels)
				if (panel != this)
					panel.Init();

			Canvas.renderMode = UiMain.Instance.RenderMode;
			Canvas.worldCamera = UiMain.Instance.Camera;

			Debug.Assert(UiMain.Instance != null);
			if (UiMain.Instance == null)
				return;

			Canvas.planeDistance = UiMain.Instance.GetTopmostPlaneDistance(this);
		}

		public void OnValidate()
		{
			InitAnimation();
		}

	}
}