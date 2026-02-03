using System;
using UnityEngine;

namespace GuiToolkit
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(CanvasGroup))]
	public sealed class UiRubberband : UiThing
	{
		[SerializeField] private KeyCode m_keyCode;
		[SerializeField] private KeyBinding.EModifiers m_modifiers;

		public readonly CEvent<Rect> OnRubberbandScreenRect = new();
		public readonly CEvent<Rect> OnRubberbandLocalRect = new();
		public readonly CEvent<Rect> OnEndRubberbandScreenRect = new();
		public readonly CEvent<Rect> OnEndRubberbandLocalRect = new();

		private KeyBinding m_keyBinding;
		private PlayerSetting m_playerSetting;
		private CanvasGroup m_canvasGroup;

		private PlayerSettings PlayerSettings => PlayerSettings.Instance;


		protected override void Awake()
		{
			base.Awake();
			m_canvasGroup = GetComponent<CanvasGroup>();
			m_canvasGroup.blocksRaycasts = false;
			m_canvasGroup.interactable = false;

			RectTransform.anchorMin = Vector2.zero;
			RectTransform.anchorMax = Vector2.zero;
			RectTransform.pivot = Vector2.zero;

			SetVisible(false);
			m_keyBinding = new KeyBinding(m_keyCode, m_modifiers);
			m_playerSetting = PlayerSettings.GetKeyBindingPlayerSetting(m_keyBinding.Encoded);
			if (m_playerSetting == null)
				throw new InvalidOperationException($"No player setting defined. Please ensure to create a player setting which matches your key binding.");
			if (!m_playerSetting.IsKeyBinding)
				throw new InvalidOperationException($"Player setting '{m_playerSetting.Key}' is not a key binding");
			if (!m_playerSetting.SupportDrag)
				throw new InvalidOperationException($"Player setting '{m_playerSetting.Key}' does not support drag");
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			AddListeners();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			RemoveListeners();
		}

		public void RemoveListeners()
		{
			if (m_playerSetting == null)
				return;

			PlayerSettings.RemoveBeginDragListener(m_keyBinding, OnBeginDrag);
			PlayerSettings.RemoveDragListener(m_keyBinding, OnDrag);
			PlayerSettings.RemoveEndDragListener(m_keyBinding, OnEndDrag);
		}

		public void AddListeners()
		{
			if (m_playerSetting == null)
				return;

			RemoveListeners();
			PlayerSettings.AddBeginDragListener(m_keyBinding, OnBeginDrag);
			PlayerSettings.AddDragListener(m_keyBinding, OnDrag);
			PlayerSettings.AddEndDragListener(m_keyBinding, OnEndDrag);
		}

		private void OnBeginDrag( PlayerSetting _, Vector3 __, Vector3 ___, Vector3 ____ )
		{
			SetVisible(true);
		}

		private void OnDrag( PlayerSetting _, Vector3 _startPosition, Vector3 ___, Vector3 _currentPosition )
		{
			Apply(_startPosition, _currentPosition, false);
		}

		private void OnEndDrag( PlayerSetting _, Vector3 _startPosition, Vector3 ___, Vector3 _currentPosition )
		{
			Apply(_startPosition, _currentPosition, true);
			SetVisible(false);
		}

		public void SetVisible( bool _visible )
		{
			m_canvasGroup.alpha = _visible ? 1 : 0;
			
			if (!_visible)
				RectTransform.sizeDelta = Vector2.zero;
		}

		public void Apply( Vector3 _startPosition, Vector3 _currentPosition, bool _isEnd )
		{
			Vector2 start = _startPosition;
			Vector2 current = _currentPosition;

			Vector2 min = Vector2.Min(start, current);
			Vector2 max = Vector2.Max(start, current);

			RectTransform parent = (RectTransform)RectTransform.parent;
			var uiCamera = UiCamera;

			if (parent != null)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parent,
					min,
					uiCamera,
					out Vector2 localMin);

				RectTransformUtility.ScreenPointToLocalPointInRectangle(
					parent,
					max,
					uiCamera,
					out Vector2 localMax);

				Vector2 localSize = localMax - localMin;

				Vector2 parentSize = parent.rect.size;
				Vector2 parentPivotOffset = new Vector2(
					parentSize.x * parent.pivot.x,
					parentSize.y * parent.pivot.y);

				Vector2 anchoredMin = localMin + parentPivotOffset;

				RectTransform.anchoredPosition = anchoredMin;
				RectTransform.sizeDelta = localSize;

				Rect screenRect = new Rect(min, max - min);
				OnRubberbandScreenRect.Invoke(screenRect);
				OnRubberbandLocalRect.Invoke(new Rect(anchoredMin, localSize));
				if (_isEnd)
				{
					OnEndRubberbandScreenRect.Invoke(screenRect);
					OnEndRubberbandLocalRect.Invoke(new Rect(anchoredMin, localSize));
				}
				return;
			}

			Rect fallbackScreenRect = new Rect(min, max - min);

			RectTransform.anchoredPosition = fallbackScreenRect.position;
			RectTransform.sizeDelta = fallbackScreenRect.size;

			OnRubberbandScreenRect.Invoke(fallbackScreenRect);
			OnRubberbandLocalRect.Invoke(fallbackScreenRect);
			
			if (_isEnd)
			{
				OnEndRubberbandScreenRect.Invoke(fallbackScreenRect);
				OnEndRubberbandLocalRect.Invoke(fallbackScreenRect);
			}
		}
	}
}
