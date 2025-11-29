using System;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;
using UnityEngine.EventSystems;
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
		}

		[Header("Multi State Button")]

		[Tooltip("Optional foreground image for per-state sprites")]
		[SerializeField][Optional] private Image m_stateImage;

		[Tooltip("States (name, sprite, background color)")]
		[SerializeField] private List<State> m_states = new List<State>();

		[Tooltip("Initial state index (clamped to valid range)")]
		[SerializeField] private int m_initialStateIndex;

		[Tooltip("Background color transition duration (seconds, 0 = instant)")]
		[SerializeField] private float m_backgroundTransitionDuration = 0.15f;

		[Tooltip("Use unscaled time for background tween")]
		[SerializeField] private bool m_useUnscaledTime;

		[Tooltip("Invoked whenever the state changes")]
		[SerializeField] public CEvent OnStateChanged;

		private int m_currentIndex = -1;
		private ITween<Color> m_backgroundTween;
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
			ApplyBackgroundColor(state, _instant);

			if (OnStateChanged != null)
				OnStateChanged.Invoke();
		}

		private void ApplySprite(State _state)
		{
			if (m_stateImage == null)
				return;

			m_stateImage.sprite = _state.Sprite;
			m_stateImage.enabled = _state.Sprite != null;
		}

		private void ApplyBackgroundColor(State _state, bool _instant)
		{
			if (m_uiImage == null)
				return;

			Color targetColor = _state.BackgroundColor;

			if (_instant || m_backgroundTransitionDuration <= 0f)
			{
				m_uiImage.Color = targetColor;
				StopBackgroundTween();
				return;
			}
				
			Color startColor = m_uiImage.Color;
			StopBackgroundTween();

			m_backgroundTween = TweenFactory.Tween
			(
				this,
				startColor,
				targetColor,
				m_backgroundTransitionDuration,
				TweenScaleFunctions.Linear,
				t => m_uiImage.Color = t.CurrentValue
			);

			if (m_useUnscaledTime && m_backgroundTween != null)
				m_backgroundTween.TimeFunc = TweenFactory.TimeFuncUnscaledDeltaTimeFunc;
		}

		private void StopBackgroundTween()
		{
			if (m_backgroundTween == null)
				return;

			TweenFactory.RemoveTween(m_backgroundTween, TweenStopBehavior.DoNotModify);
			m_backgroundTween = null;
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
