using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public interface IShowHidePanelAnimation
	{
		void ShowViewAnimation(Action _onFinish = null);
		void HideViewAnimation(Action _onFinish = null);
		void StopViewAnimation(bool _visible);
	}

	public class UiPanel : UiThing, ISetDefaultSceneVisibility
	{
		[SerializeField] protected EDefaultSceneVisibility m_defaultSceneVisibility = EDefaultSceneVisibility.DontCare;
		[SerializeField] protected IShowHidePanelAnimation m_showHideAnimation;

		// Additional events independent of the Show/Hide actions
		public CEvent<UiPanel> OnShow = new CEvent<UiPanel>();
		public CEvent<UiPanel> OnHide = new CEvent<UiPanel>();
		public CEvent<UiPanel> OnDestroyed = new CEvent<UiPanel>();

		public virtual bool AutoDestroyOnHide => false;
		public virtual bool Poolable => false;

		private bool m_defaultSceneVisibilityApplied;
		private bool m_animationInitialized;

		public bool Visible { get; private set; }

		public UiSimpleAnimationBase SimpleShowHideAnimation
		{
			get
			{
				InitAnimationIfNecessary();
				if (m_showHideAnimation is UiSimpleAnimation)
					return (UiSimpleAnimation) m_showHideAnimation;
				return null;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			InitAnimationIfNecessary();
		}

		private void OnEvHideInstant( Type _type )
		{
			if (GetType() == _type)
			{
				Hide(true);
			}
		}

		// Have to make this public because c# programmers don't have friends. Shitty language. 
		public virtual void OnPooled() { }

		public virtual void Show(bool _instant = false, Action _onFinish = null)
		{
			if (SimpleShowHideAnimation == null)
				_instant = true;

			gameObject.SetActive(true);

			if (Visible && !_instant)
				return;
			Visible = true;

			if (_instant)
			{
				if (SimpleShowHideAnimation != null)
				{
					SimpleShowHideAnimation.StopViewAnimation(true);
				}
				OnShow.Invoke(this);
				_onFinish?.Invoke();
				return;
			}

			SimpleShowHideAnimation.ShowViewAnimation(() =>
			{
				OnShow.Invoke(this);
				_onFinish?.Invoke();
			});
		}

		public virtual void Hide(bool _instant = false, Action _onFinish = null)
		{
			if (SimpleShowHideAnimation == null)
				_instant = true;

			if (!Visible && !_instant)
				return;
			Visible = false;

			if (_instant)
			{
				gameObject.SetActive(false);
				if (SimpleShowHideAnimation != null)
					SimpleShowHideAnimation.StopViewAnimation(false);

				OnHide.Invoke(this);
				_onFinish?.Invoke();

				DestroyIfNecessary();
				return;
			}

			SimpleShowHideAnimation.HideViewAnimation( () =>
			{
				gameObject.SetActive(false);

				OnHide.Invoke(this);
				_onFinish?.Invoke(); 

				DestroyIfNecessary();
			});
		}

		public void SetVisible(bool _visible, bool _instant = false, Action _onFinish = null)
		{
			if (_visible)
				Show(_instant, _onFinish);
			else
				Hide(_instant, _onFinish);
		}

		private void DestroyIfNecessary()
		{
			if (!AutoDestroyOnHide)
				return;

			if (Poolable)
			{
				OnDestroyed.Invoke(this);
				OnDestroyed.RemoveAllListeners();
				UiPool.Instance.DoDestroy(this);
			}
			else
				gameObject.Destroy();
		}

		protected override void OnDestroy()
		{
			OnDestroyed.Invoke(this);
			base.OnDestroy();
		}

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

		private void InitAnimationIfNecessary()
		{
			if (m_animationInitialized)
				return;

			m_animationInitialized = true;

			var components = GetComponents<MonoBehaviour>();
			foreach (var component in components)
			{
				if (component is IShowHidePanelAnimation)
				{
					m_showHideAnimation = (IShowHidePanelAnimation) component;
					break;
				}
			}
		}

	}
}
