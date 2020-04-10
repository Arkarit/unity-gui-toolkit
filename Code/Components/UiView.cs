using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GuiToolkit
{
	public interface IShowHideViewAnimation : IShowHidePanelAnimation
	{
		void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards );
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

		public virtual void Push(bool _instant = false, EStackAnimationType _stackAnimationType = EStackAnimationType.DontTouch, Action _onFinishHide = null, Action _onFinishShow = null)
		{
			UiMain.Instance.Push(this, _instant, _stackAnimationType, _onFinishHide, _onFinishShow);
		}

		public virtual void Pop(bool _instant = false, EStackAnimationType _stackAnimationType = EStackAnimationType.DontTouch, int _skip = 0, Action _onFinishHide = null, Action _onFinishShow = null)
		{
			Debug.Assert(UiMain.Instance.Peek() == this, "Attempting to pop wrong dialog");
			UiMain.Instance.Pop(_skip, _instant, _stackAnimationType, _onFinishHide, _onFinishShow);
		}

		public void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards )
		{
			InitAnimation();
			if (m_showHideAnimation == null || !(m_showHideAnimation is IShowHideViewAnimation))
				return;

			((IShowHideViewAnimation)m_showHideAnimation).SetStackAnimationType(_stackAnimationType, _backwards);
		}

		public void Init( RenderMode _renderMode, Camera _camera )
		{
			Init();
			UiPanel[] panels = GetComponentsInChildren<UiPanel>();
			foreach (var panel in panels)
				panel.Init();

			Canvas.renderMode = _renderMode;
			Canvas.worldCamera = _camera;

			Debug.Assert(UiMain.Instance != null);
			if (UiMain.Instance == null)
				return;

			Canvas.planeDistance = UiMain.Instance.LayerDistance * (float) m_layer;

		}

		public void OnValidate()
		{
			InitAnimation();
		}

	}
}