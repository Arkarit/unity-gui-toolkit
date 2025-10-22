using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiTransitionOverlay : UiAbstractTransitionOverlay
	{
		public enum EFading
		{
			FadingIn,
			FadingOut,
			FadedIn,
			FadedOut
		}

		private static UiAbstractTransitionOverlay s_instance;

		[SerializeField][Mandatory] private UiSimpleAnimation m_animation;
		[SerializeField][Mandatory] private CanvasGroup m_canvasGroup;
		[SerializeField] private bool m_setNonInteractive = true;

		private EFading m_state = EFading.FadedOut;

		public static UiAbstractTransitionOverlay Instance
		{
			get
			{
				if (s_instance == null)
				{
					if (UiToolkitConfiguration.Instance.TransitionOverlay != null)
					{
						var go = Instantiate(UiToolkitConfiguration.Instance.TransitionOverlay.gameObject);
						s_instance = go.GetComponent<UiAbstractTransitionOverlay>();
					}
					else
					{
						s_instance = BuildFallbackOverlay();
					}
				}

				return s_instance;
			}

			set
			{
				if (s_instance)
					Destroy(s_instance.gameObject);
				
				s_instance = value;
			}
		}

		public override void FadeInOutOverlay( Action _onFadedIn, Func<bool> _readyForFadeOut = null, Action _onFadedOut = null )
		{
			FadeInOverlay(() =>
			{
				_onFadedIn?.Invoke();
				if (_readyForFadeOut == null)
				{
					FadeOutOverlay(() =>
					{
						_onFadedOut?.Invoke();
					});
					return;
				}

				CoRoutineRunner.Instance.StartCoroutine(FadeOutWhenReady(_readyForFadeOut, _onFadedOut));
			});
		}

		private IEnumerator FadeOutWhenReady( Func<bool> _readyForFadeOut, Action _onFadedOut )
		{
			yield return new WaitUntil(() => _readyForFadeOut());
			FadeOutOverlay(() =>
			{
				_onFadedOut?.Invoke();
			});
		}

		public override void FadeInOverlay( Action _onFadedIn = null, bool _instant = false )
		{
			if (m_state == EFading.FadedIn)
			{
				_onFadedIn?.Invoke();
				return;
			}

			SetBlocking(true);

			// FIXME #73 We can NOT use OnFinishOnce here, because the listener handling breaks when you call FadeOutOverlay() from
			// finish hook of FadeInOverlay() - the animation is still "playing"
			// OnFinish hasn't got this problem, because listeners are not automatically removed.
			// https://github.com/Arkarit/unity-gui-toolkit/issues/73
			m_animation.OnFinish.AddListener(OnFinish);
			if (m_state == EFading.FadingIn || _instant)
			{
				if (_instant)
					m_animation.Reset(true);

				return;
			}

			m_state = EFading.FadingIn;

			m_animation.Log("Fading in");
			m_animation.Play(false);

			void OnFinish()
			{
				m_animation.Log("OnFinish FadeInOverlay()");
				_onFadedIn?.Invoke();
				m_state = EFading.FadedIn;
				m_animation.OnFinish.RemoveListener(OnFinish);
			}
		}

		public override void FadeOutOverlay( Action _onFadedOut = null, bool _instant = false )
		{
			if (m_state == EFading.FadedOut)
			{
				_onFadedOut?.Invoke();
				return;
			}

			// FIXME #73 We can NOT use OnFinishOnce here, because the listener handling breaks when you call FadeOutOverlay() from
			// finish hook of FadeInOverlay() - the animation is still "playing"
			// OnFinish hasn't got this problem, because listeners are not automatically removed.
			// https://github.com/Arkarit/unity-gui-toolkit/issues/73
			m_animation.OnFinish.AddListener(OnFinish);

			if (m_state == EFading.FadingOut || _instant)
			{
				if (_instant)
					m_animation.Reset();

				return;
			}

			m_state = EFading.FadingOut;

			m_animation.Log("Fading out");
			m_animation.Play(true);

			void OnFinish()
			{
				m_animation.Log("OnFinish FadeOutOverlay()");
				SetBlocking(false);
				_onFadedOut?.Invoke();
				m_state = EFading.FadedOut;
				m_animation.OnFinish.RemoveListener(OnFinish);
			}
		}

		private void SetBlocking( bool _isBlocking )
		{
			m_animation.Log($"SetBlocking({_isBlocking}); m_setNonInteractive:{m_setNonInteractive}");
			if (!m_setNonInteractive)
				return;

			m_canvasGroup.interactable = _isBlocking;
			m_canvasGroup.blocksRaycasts = _isBlocking;
		}

		private static UiTransitionOverlay BuildFallbackOverlay()
		{
			// Root object with Canvas + Group + Raycaster
			GameObject root = new GameObject(
				"UiTransitionOverlay",
				typeof(RectTransform),
				typeof(Canvas),
				typeof(CanvasGroup),
				typeof(GraphicRaycaster),
				typeof(UiSimpleAnimation)
			);

			// Try to use UI layer if present
			int uiLayer = LayerMask.NameToLayer("UI");
			root.layer = uiLayer >= 0 ? uiLayer : 0;

			RectTransform rootRt = root.GetComponent<RectTransform>();
			rootRt.anchorMin = Vector2.zero;
			rootRt.anchorMax = Vector2.one;
			rootRt.offsetMin = Vector2.zero;
			rootRt.offsetMax = Vector2.zero;

			Canvas canvas = root.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 32760; // very high but below extreme sentinels
			canvas.pixelPerfect = false;

			CanvasGroup group = root.GetComponent<CanvasGroup>();
			group.alpha = 0f;            // start invisible
			group.interactable = false;  // toggle at runtime if needed
			group.blocksRaycasts = false;

			GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
			raycaster.ignoreReversedGraphics = true;

			// Child with black Image filling the canvas
			GameObject blackGo = new GameObject(
				"BlackOverlay",
				typeof(RectTransform),
				typeof(Image)
			);
			blackGo.layer = root.layer;

			RectTransform blackRt = blackGo.GetComponent<RectTransform>();
			blackRt.SetParent(rootRt, false);
			blackRt.anchorMin = Vector2.zero;
			blackRt.anchorMax = Vector2.one;
			blackRt.offsetMin = Vector2.zero;
			blackRt.offsetMax = Vector2.zero;

			Image blackImage = blackGo.GetComponent<Image>();
			blackImage.color = Color.black;
			blackImage.raycastTarget = true;

			UiSimpleAnimation animation = root.GetComponent<UiSimpleAnimation>();
			animation.AlphaCanvasGroup = group;
			animation.Duration = .5f;
			animation.AlphaCurve = AnimationCurve.Linear(0, 0, 1, 1);
			animation.Support = UiSimpleAnimation.ESupport.Alpha;
			// does nothing when not compiled with DEBUG_SIMPLE_ANIMATION
			animation.Debug = true;

			// Attach component and cache instance
			UiTransitionOverlay overlay = root.AddComponent<UiTransitionOverlay>();
			overlay.m_animation = animation;
			overlay.m_canvasGroup = group;
			DontDestroyOnLoad(root);

			return overlay;
		}
	}
}
