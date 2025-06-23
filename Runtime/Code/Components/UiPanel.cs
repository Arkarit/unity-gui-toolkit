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
		public CEvent<UiPanel> EvOnBeginShow = new ();
		public CEvent<UiPanel> EvOnEndShow = new ();
		public CEvent<UiPanel> EvOnBeginHide = new ();
		public CEvent<UiPanel> EvOnEndHide = new ();
		public CEvent<UiPanel> EvOnDestroyed = new ();

		public virtual bool AutoDestroyOnHide => false;
		public virtual bool Poolable => false;
		public virtual bool ShowDestroyFieldsInInspector => false;
		
		public virtual bool IsVisible => gameObject.activeSelf;
		public virtual bool IsVisibleInHierarchy => gameObject.activeInHierarchy;

		public virtual void OnBeginShow() {}
		public virtual void OnEndShow() {}
		public virtual void OnBeginHide() {}
		public virtual void OnEndHide() {}

		private bool m_defaultSceneVisibilityApplied;
		private bool m_animationInitialized;
		private Action m_onShowHideFinishAction;

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

		// Have to make this public because c# programmers don't have friends. Shitty language. 
		public virtual void OnPooled() { }

		public virtual void Show(bool _instant = false, Action _onFinish = null)
		{
			if (SimpleShowHideAnimation == null)
				_instant = true;

			gameObject.SetActive(true);

			if (Visible && !_instant)
				return;

			OnBeginShow();
			EvOnBeginShow.Invoke(this);
			Visible = true;

			if (_instant)
			{
				if (SimpleShowHideAnimation != null)
				{
					SimpleShowHideAnimation.StopViewAnimation(true);
				}
				OnEndShow();
				EvOnEndShow.Invoke(this);
				_onFinish?.Invoke();
				m_onShowHideFinishAction = null;
				return;
			}

			PlayShowHideAnimation(true, _onFinish);
		}

		public virtual void Hide(bool _instant = false, Action _onFinish = null)
		{
			if (SimpleShowHideAnimation == null)
				_instant = true;

			if (!Visible && !_instant)
				return;

			OnBeginHide();
			EvOnBeginHide.Invoke(this);
			Visible = false;

			if (_instant)
			{
				gameObject.SetActive(false);
				if (SimpleShowHideAnimation != null)
					SimpleShowHideAnimation.StopViewAnimation(false);

				OnEndHide();
				EvOnEndHide.Invoke(this);
				_onFinish?.Invoke();

				m_onShowHideFinishAction = null;
				DestroyIfNecessary();
				return;
			}

			PlayShowHideAnimation(false, _onFinish);
		}
		
		private void PlayShowHideAnimation(bool _show, Action _onFinish)
		{
			m_onShowHideFinishAction = _onFinish;
			SimpleShowHideAnimation.OnFinishOnce.RemoveListener(HideViewCallback);
			SimpleShowHideAnimation.OnFinishOnce.RemoveListener(ShowViewCallback);
			if (_show)
				SimpleShowHideAnimation.ShowViewAnimation(ShowViewCallback);
			else
				SimpleShowHideAnimation.HideViewAnimation(HideViewCallback);
		}
		
		private void ShowViewCallback()
		{
			OnEndShow();
			EvOnEndShow.Invoke(this);
			m_onShowHideFinishAction?.Invoke();
		}
		
		private void HideViewCallback()
		{
			gameObject.SetActive(false);

			OnEndHide();
			EvOnEndHide.Invoke(this);
			m_onShowHideFinishAction?.Invoke();
			m_onShowHideFinishAction = null;

			DestroyIfNecessary();
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
				EvOnDestroyed.Invoke(this);
				EvOnDestroyed.RemoveAllListeners();
				if (UiMain.IsAwake)
					UiPool.Instance.DoDestroy(this);
			}
			else
				gameObject.SafeDestroy();
		}

		protected override void OnDestroy()
		{
			EvOnDestroyed.Invoke(this);
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
