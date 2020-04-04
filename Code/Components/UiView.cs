using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GuiToolkit
{
	public interface IShowHideViewAnimation
	{
		void ShowViewAnimation(Action _onFinish = null);
		void HideViewAnimation(Action _onFinish = null);
		void StopViewAnimation(bool _visible);
	}

	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(CanvasScaler))]
	[RequireComponent(typeof(GraphicRaycaster))]
	public class UiView : UiThing, ISetDefaultSceneVisibility
	{
		[SerializeField]
		protected EUiLayerDefinition m_layer = EUiLayerDefinition.Dialog;

		[SerializeField]
		protected IShowHideViewAnimation m_showHideAnimation;

		[SerializeField]
		protected EDefaultSceneVisibility m_defaultSceneVisibility = EDefaultSceneVisibility.DontCare;

		public virtual bool AutoDestroyOnHide => false;
		public virtual bool Poolable => false;

		[System.Serializable]
		public class CEvHideInstant : UnityEvent<Type> {}
		public static CEvHideInstant EvHideInstant = new CEvHideInstant();

		[System.Serializable]
		public class CEvSetTag : UnityEvent<string, bool> {}
		public static CEvSetTag EvSetTag = new CEvSetTag();

		private bool m_isVisible = false;

		private bool m_defaultSceneVisibilityApplied;

		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			EvHideInstant.AddListener(OnEvHideInstant);
			EvSetTag.AddListener(OnEvSetTag);
		}

		protected override void RemoveEventListeners()
		{
			base.RemoveEventListeners();
			EvHideInstant.RemoveListener(OnEvHideInstant);
			EvSetTag.RemoveListener(OnEvSetTag);
		}

		private void OnEvHideInstant( Type _type )
		{
			if (GetType() == _type)
			{
				Hide(true);
			}
		}

		private void OnEvSetTag( string _tag, bool _instant )
		{
			string tag = gameObject.tag;
			if (string.IsNullOrEmpty(tag))
				return;

			if (tag == _tag)
			{
				if (!isActiveAndEnabled)
				{
					if (m_isVisible)
						Show(_instant);
				}
			}
			else
			{
				m_isVisible = isActiveAndEnabled;
				if (isActiveAndEnabled)
					Hide(_instant);
			}
		}

		public static void InvokeHideInstant<T>()
		{
			EvHideInstant.Invoke(typeof(T));
		}

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

		public virtual void Show(bool _instant = false, Action _onFinish = null)
		{
			if (m_showHideAnimation == null)
				_instant = true;

			gameObject.SetActive(true);

			if (_instant)
			{
				if (m_showHideAnimation != null)
				{
					m_showHideAnimation.StopViewAnimation(true);
				}
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
					m_showHideAnimation.StopViewAnimation(false);
				if (_onFinish != null)
					_onFinish.Invoke(); 
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

		public void Init( RenderMode _renderMode, Camera _camera )
		{
			InitAnimation();

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

		private void InitAnimation()
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
			InitAnimation();
		}
	}
}