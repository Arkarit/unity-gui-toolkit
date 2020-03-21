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
	public class UiView : UiThing, ISetDefaultSceneVisibility
	{
		[SerializeField]
		protected EUiLayerDefinition m_layer = EUiLayerDefinition.Dialog;

		[SerializeField]
		protected IShowHideViewAnimation m_showHideAnimation;

		[SerializeField]
		protected DefaultSceneVisibility m_defaultSceneVisibility = DefaultSceneVisibility.DontCare;

		public virtual bool AutoDestroyOnHide => false;
		public virtual bool Poolable => false;

		protected UiSimpleAnimationBase SimpleShowHideAnimation
		{
			get
			{
				if (m_showHideAnimation is UiSimpleAnimation)
					return (UiSimpleAnimation) m_showHideAnimation;
				return null;
			}
		}

		private Canvas m_canvas;
		
		// Have to make this public because c# programmers don't have friends. Shitty language. 
		public virtual void OnPooled() { }

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
				DestroyIfNecessary();
				return;
			}

			m_showHideAnimation.HideViewAnimation( () =>
			{
				gameObject.SetActive(false);
				if (_onFinish != null)
					_onFinish.Invoke(); 
				DestroyIfNecessary();
			});
		}

		private void DestroyIfNecessary()
		{
			if (!AutoDestroyOnHide)
				return;

			if (Poolable)
				UiPool.Instance.DoDestroy(this);
			else
				gameObject.Destroy();
		}

		public void SetRenderMode( RenderMode _renderMode, Camera _camera )
		{
#if UNITY_EDITOR
			Init();
#endif
			Canvas.renderMode = _renderMode;
			Canvas.worldCamera = _camera;

			Debug.Assert(UiMain.Instance != null);
			if (UiMain.Instance == null)
				return;

			Canvas.planeDistance = UiMain.Instance.LayerDistance * (float) m_layer;
		}

		public void SetDefaultSceneVisibility()
		{
			if (Application.isPlaying)
			{
				switch (m_defaultSceneVisibility)
				{
					default:
					case DefaultSceneVisibility.DontCare:
						break;
					case DefaultSceneVisibility.Visible:
						gameObject.SetActive(true);
						break;
					case DefaultSceneVisibility.Invisible:
						gameObject.SetActive(false);
						break;
					case DefaultSceneVisibility.VisibleInDevBuild:
						#if UNITY_EDITOR || DEVELOPMENT_BUILD
							gameObject.SetActive(true);
						#else
							gameObject.SetActive(false);
						#endif
						break;
					case DefaultSceneVisibility.VisibleWhen_DEFAULT_SCENE_VISIBLE_defined:
						#if DEFAULT_SCENE_VISIBLE
							gameObject.SetActive(true);
						#else
							gameObject.SetActive(false);
						#endif
						break;
				}
			}
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