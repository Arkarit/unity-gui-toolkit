using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiTransitionOverlay : MonoBehaviour
	{
		public enum EFading
		{
			FadingIn,
			FadingOut,
			FadedIn,
			FadedOut
		}
		
		private static UiTransitionOverlay s_instance;

		[SerializeField][Mandatory] private UiSimpleAnimation m_animation;
		[SerializeField][Mandatory] private CanvasGroup m_canvasGroup;
		[SerializeField] private bool m_setNonInteractive = true;

		private EFading m_state = EFading.FadedOut;
		
		public static UiTransitionOverlay Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = UiToolkitConfiguration.Instance.TransitionOverlay;

					// For convenience, we create a fallback game object (black, .5s fade)
					if (s_instance == null)
						s_instance = BuildFallbackOverlay();
				}

				return s_instance;
			}

			set => s_instance = value;
		}

		public void FadeInOverlay( Action _onFadedIn = null, bool _instant = false )
		{
			if (m_state == EFading.FadedIn)
			{
				_onFadedIn?.Invoke();
				return;
			}
			
			SetBlocking(true);
			
			if (m_state == EFading.FadingIn || _instant)
			{
				m_animation.OnFinishOnce.AddListener(() => _onFadedIn?.Invoke());
				if (_instant)
					m_animation.Reset(true);
				
				return;
			}
			
			m_state = EFading.FadingIn;
			
UiLog.Log($"---::: Fading in");
			m_animation.Play(false, () =>
			{
				_onFadedIn?.Invoke();
				m_state = EFading.FadedIn;
			});
		}

		public void FadeOutOverlay( Action _onFadedOut = null, bool _instant = false )
		{
			if (m_state == EFading.FadedOut)
			{
				_onFadedOut?.Invoke();
				return;
			}
			
			if (m_state == EFading.FadingOut || _instant)
			{
				m_animation.OnFinishOnce.AddListener(() => _onFadedOut?.Invoke());
				if (_instant)
					m_animation.Reset();
				
				return;
			}
			
			m_state = EFading.FadingOut;
			
UiLog.Log($"---::: Fading out");
			m_animation.Play(true, () =>
			{
				SetBlocking(false);
				_onFadedOut?.Invoke();
				m_state = EFading.FadedOut;
			});
		}
		
		private void SetBlocking(bool _isBlocking)
		{
UiLog.Log($"---::: SetBlocking({_isBlocking}); m_setNonInteractive:{m_setNonInteractive}");
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

			// Attach component and cache instance
			UiTransitionOverlay overlay = root.AddComponent<UiTransitionOverlay>();
			overlay.m_animation = animation;
			overlay.m_canvasGroup = group;
			DontDestroyOnLoad(root);

			return overlay;
		}
	}
}
