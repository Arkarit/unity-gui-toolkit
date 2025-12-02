using System;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiMultiStateButton : UiButtonBase
	{
		[Serializable]
		public class State
		{
			public string Name;
			public Sprite Sprite;
			public Color BackgroundColor = Color.white;
			public Color IconColor = Color.white;
		}

		[Header("Multi State Button")]

		[Tooltip("Optional foreground image for per-state sprites")]
		[SerializeField][Optional] private UiImage m_stateImage;

		[Tooltip("States (name, sprite, background color)")]
		[SerializeField] private List<State> m_states = new List<State>();

		[Tooltip("Initial state index (clamped to valid range)")]
		[SerializeField] private int m_initialStateIndex;

		[Tooltip("Background color transition duration (seconds, 0 = instant)")]
		[FormerlySerializedAs("m_backgroundTransitionDuration")]
		[SerializeField] private float m_transitionDuration = 0.15f;

		[Tooltip("Use unscaled time for background tween")]
		[SerializeField] private bool m_useUnscaledTime;

		[Tooltip("Invoked whenever the state changes")]
		public CEvent OnStateChanged = new();

		private int m_currentIndex = -1;
		private ITween<Color> m_backgroundTween;
		private ITween<Color> m_iconTween;
		private readonly Dictionary<string, int> m_StateDict = new();

		/// <summary>
		/// Current state index (0-based). Setter clamps and updates visuals.
		/// </summary>
		public int CurrentStateIndex
		{
			get
			{
				return m_currentIndex;
			}
			set
			{
				SetStateIndex(value, false);
			}
		}

		/// <summary>
		/// Readonly access to the current state definition (may be null if no states).
		/// </summary>
		public State CurrentState
		{
			get
			{
				if (m_currentIndex < 0 || m_currentIndex >= m_states.Count)
					return null;
				return m_states[m_currentIndex];
			}
		}
		
		public string CurrentStateName
		{
			get
			{
				var state = CurrentState;
				if (state == null)
					return null;
				return state.Name;
			}
			
			set
			{
				if (m_StateDict.TryGetValue(value, out var stateIdx))
					SetStateIndex(stateIdx, true);
			}
		}

		public IReadOnlyList<State> States => m_states;

		protected override void Awake()
		{
			base.Awake();
			for (int i = 0; i < m_states.Count; i++)
			{
				string name = m_states[i].Name;
				m_StateDict.Add(name, i);
			}
			
			InitStateIfNecessary(true);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			InitStateIfNecessary(true);
		}

		public override void OnPointerUp(PointerEventData _)
		{
			if (!EvaluateButton(true))
				return;

			SetNextState();
		}

		public void SetNextState()
		{
			if (m_states.Count == 0)
				return;

			int next = m_currentIndex + 1;
			if (next >= m_states.Count)
				next = 0;

			SetStateIndex(next, false);
		}

		public void SetPrevState()
		{
			if (m_states.Count == 0)
				return;

			int next = m_currentIndex - 1;
			if (next < 0)
				next = m_states.Count - 1;

			SetStateIndex(next, false);
		}

		public void SetDefaultState()
		{
			if (m_states.Count == 0)
				return;

			SetStateIndex(0, false);
		}

		private void InitStateIfNecessary(bool _instant)
		{
			if (m_currentIndex >= 0)
				return;

			if (m_stateImage == null && m_uiImage == null)
				UiLog.LogError("UiMultiStateButton needs at least a state image or a background image", this);

			if (m_states.Count == 0)
			{
				UiLog.LogWarning("UiMultiStateButton has no states configured", this);
				return;
			}

			int index = Mathf.Clamp(m_initialStateIndex, 0, m_states.Count - 1);
			SetStateIndex(index, _instant);
		}

		private void SetStateIndex(int _index, bool _instant)
		{
			if (m_states.Count == 0)
				return;

			int clampedIndex = Mathf.Clamp(_index, 0, m_states.Count - 1);
			if (clampedIndex == m_currentIndex && !_instant)
				return;

			m_currentIndex = clampedIndex;
			State state = m_states[m_currentIndex];

			ApplySprite(state);
			if (m_uiImage != null)
				ApplyColor(m_uiImage, _instant, m_uiImage.Color, state.BackgroundColor, ref m_backgroundTween);
			if (m_stateImage != null)
				ApplyColor(m_stateImage, _instant, m_stateImage.Color, state.IconColor, ref m_iconTween);

			if (OnStateChanged != null)
				OnStateChanged.Invoke();
		}

		private void ApplySprite(State _state)
		{
			if (m_stateImage == null)
				return;

			m_stateImage.Image.sprite = _state.Sprite;
			m_stateImage.enabled = _state.Sprite != null;
		}

		private void ApplyColor(UiImage _uiImage, bool _instant, Color _startColor, Color _targetColor, ref ITween<Color> _storedTween)
		{
			if (_uiImage == null)
				return;

			if (_instant || m_transitionDuration <= 0f)
			{
				_uiImage.Color = _targetColor;
				StopTween(ref _storedTween);
				return;
			}
				
			StopTween(ref _storedTween);

			_storedTween = TweenFactory.Tween
			(
				_uiImage,
				_startColor,
				_targetColor,
				m_transitionDuration,
				TweenScaleFunctions.Linear,
				t => _uiImage.Color = t.CurrentValue
			);

			if (m_useUnscaledTime && _storedTween != null)
				_storedTween.TimeFunc = TweenFactory.TimeFuncUnscaledDeltaTimeFunc;
		}

		private void StopTween(ref ITween<Color> _storedTween)
		{
			if (_storedTween == null)
				return;

			TweenFactory.RemoveTween(_storedTween, TweenStopBehavior.DoNotModify);
			_storedTween = null;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_stateImage == null && m_uiImage == null)
				UiLog.LogWarning("UiMultiStateButton: Either state image or background image should be assigned", this);

			if (m_initialStateIndex < 0)
				m_initialStateIndex = 0;
		}
#endif
	}
}
