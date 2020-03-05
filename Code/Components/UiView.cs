using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	public interface IShowHideViewAnimation
	{
		void ShowViewAnimation(Action _onFinish = null);
		void HideViewAnimation(Action _onFinish = null);
		void StopViewAnimation();
	}

	[RequireComponent(typeof(Canvas))]
	public class UiView : UiThing
	{
		public string m_name;

		[SerializeField]
		private EUiLayerDefinition m_layer;

		[SerializeField]
		private IShowHideViewAnimation m_showHideAnimation;

		private Canvas m_canvas;
		private UiMain m_main;

		protected override void Awake()
		{
			base.Awake();
			Init();
		}

		public virtual void Show(bool _instant = false)
		{
			if (m_showHideAnimation == null)
				_instant = true;

			gameObject.SetActive(true);

			if (_instant)
			{
				m_showHideAnimation?.StopViewAnimation();
				return;
			}

			m_showHideAnimation.ShowViewAnimation(null);
		}

		public virtual void Hide(bool _instant = false)
		{
			if (m_showHideAnimation == null)
				_instant = true;


			if (_instant)
			{
				gameObject.SetActive(false);
				m_showHideAnimation?.StopViewAnimation();
				return;
			}

			m_showHideAnimation.HideViewAnimation( () => gameObject.SetActive(false) );
		}

		public void SetRenderMode( RenderMode _renderMode, Camera _camera )
		{
#if UNITY_EDITOR
			Init();
#endif
			m_canvas.renderMode = _renderMode;
			m_canvas.worldCamera = _camera;

			if (m_main)
				m_canvas.planeDistance = m_main.LayerDistance * (float) m_layer;
		}

		private void Init()
		{
			m_main = GetComponentInParent<UiMain>();
			m_canvas = GetComponent<Canvas>();

			var components = GetComponents<MonoBehaviour>();
			foreach (var component in components)
			{
				if (component is IShowHideViewAnimation)
				{
					m_showHideAnimation = (IShowHideViewAnimation) component;
					break;
				}
			}

			if (m_main)
				m_canvas.planeDistance = m_main.LayerDistance * (float) m_layer;
		}

		public void OnValidate()
		{
			Init();
		}
	}
}