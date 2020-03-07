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
		private EUiLayerDefinition m_layer = EUiLayerDefinition.Dialog;

		[SerializeField]
		private IShowHideViewAnimation m_showHideAnimation;

		public virtual bool AutoDestroyOnHide => false;

		private Canvas m_canvas;

		public Canvas Canvas {
			get
			{
				if (m_canvas == null)
					m_canvas = GetComponent<Canvas>();
				return m_canvas;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			Init();
		}

		public virtual void Show(bool _instant = false, Action _onFinish = null)
		{
			if (m_showHideAnimation == null)
				_instant = true;

			gameObject.SetActive(true);

			if (_instant)
			{
				if (m_showHideAnimation != null)
					m_showHideAnimation.StopViewAnimation();
				return;
			}

			m_showHideAnimation.ShowViewAnimation(_onFinish);
		}

		public virtual void Hide(bool _instant = false, Action _onFinish = null)
		{
			if (m_showHideAnimation == null)
				_instant = true;

			if (_instant)
			{
				gameObject.SetActive(false);
				if (m_showHideAnimation != null)
					m_showHideAnimation.StopViewAnimation();
				if (AutoDestroyOnHide)
					Destroy(gameObject);
				return;
			}

			m_showHideAnimation.HideViewAnimation( () =>
			{
				gameObject.SetActive(false);
				if (_onFinish != null)
					_onFinish.Invoke(); 
				if (AutoDestroyOnHide)
					Destroy(gameObject);
			});
		}

		public void SetRenderMode( RenderMode _renderMode, Camera _camera )
		{
#if UNITY_EDITOR
			Init();
#endif
			Canvas.renderMode = _renderMode;
			Canvas.worldCamera = _camera;
			Canvas.planeDistance = UiMain.Instance.LayerDistance * (float) m_layer;
		}

		private void Init()
		{
			var components = GetComponents<MonoBehaviour>();
			foreach (var component in components)
			{
				if (component is IShowHideViewAnimation)
				{
					m_showHideAnimation = (IShowHideViewAnimation) component;
					break;
				}
			}
		}

		public void OnValidate()
		{
			Init();
		}
	}
}