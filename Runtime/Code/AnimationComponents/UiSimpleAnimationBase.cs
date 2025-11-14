using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiSimpleAnimationBase : UiThing, IShowHidePanelAnimation
	{
		protected const int INFINITE_LOOPS = -1;
		private const int DONT_SET_LOOPS = -2;

		// Timing values
		[Tooltip("Duration of this animation, excluding delay.")]
		[SerializeField] protected float m_duration = 1;

		[Tooltip("Delay for the beginning of the animation (when played forwards)")]
		[SerializeField] protected float m_delay = 0;
		
		[Tooltip("Animation speed")]
		[Range(0.01f, 100f)]
		[SerializeField] protected float m_animationSpeed = 1;
		
		[Tooltip("Propagates the speed setting to all slaves (hierarchically)")]
		[SerializeField] protected bool m_setSpeedForSlaves = false;

		[Tooltip("If set to false, this animation is skipped when playing backwards")]
		[SerializeField] protected bool m_backwardsPlayable = true;

		[Tooltip("If set, this animation is played instead when playing backwards")]
		[SerializeField] protected UiSimpleAnimationBase m_backwardsAnimation;

		[Tooltip("If set to true, the animation instantly goes to start when played backwards")]
		[SerializeField] protected bool m_gotoStartOnBackwards = false;

		[Tooltip("Automatically start the animation as soon as it becomes visible")]
		[SerializeField] protected bool m_autoStart = false;

		[Tooltip("Automatically start the animation every time it becomes visible")]
		[SerializeField] protected bool m_autoOnEnable = false;

		[Tooltip("Set the animation beginning values as it becomes visible, but don't start it")]
		[SerializeField] protected bool m_setOnStart = true;

		[Tooltip("Number of loops. -1: infinite loops 0: no loops, >0: Arbitrary number of loops")]
		[SerializeField] protected int m_numberOfLoops = 0;

		[FormerlySerializedAs("m_finishInstantOnOrientationChange")]
		[Tooltip("Finish animation instantly on screen resolution change. This is important to set for animations, which differ in landscape and portrait.")]
		[SerializeField] protected bool m_finishInstantOnResolutionChange = false;

		// Slave animations

		[Tooltip("Slave animations which are automatically started when this animation is started.")]
		[SerializeField] protected List<UiSimpleAnimationBase> m_slaveAnimations = new();

		[SerializeField] protected bool m_setLoopsForSlaves = true;
		[SerializeField] protected bool m_supportViewAnimations = true;
		
// Workaround #95
[Tooltip("Currently, the calculation of the backwards time is broken. It's pretty complicated and I don't have the time to fix it. " + 
         "If you do: You know where the Repo is :) Activating this workaround sets the backward delay to 0, and lets you enter a correction factor for the backwards time.")]
[SerializeField] protected bool m_timingWorkaroundActive;
[SerializeField] protected float m_timingWorkaroundBackwardsBias = 0f;
//

		protected override bool NeedsOnScreenResolutionChangedCallback => m_finishInstantOnResolutionChange;

		public List<UiSimpleAnimationBase> SlaveAnimations => m_slaveAnimations;
		public bool IsPlaying
		{
			get => m_playing;
			private set
			{
#if DEBUG_SIMPLE_ANIMATION
				if (m_playing == value)
					return;
				Log($"Set IsPlaying:{value}");
#endif
				m_playing = value;
			}
		}
		
		public bool IsBackwards => m_backwards;
		public bool IsAtBeginning => m_currentTime == 0;
		public bool HasBackwardsAnimation => m_backwardsAnimation;

		public float Duration
		{
			get => m_duration; 
			set
			{
				Reset(); 
				m_duration = value;
			}
		}
		
		public float Delay
		{
			get => m_delay;
			set
			{
				Reset(); 
				m_delay = value;
			}
		}
		
		public UiSimpleAnimationBase BackwardsAnimation
		{
			get => m_backwardsAnimation; 
			set
			{
				Reset(); 
				m_backwardsAnimation = value;
			}
		}

		public void SetDuration(float _duration, bool _reset)
		{
			if (_reset)
				Reset();
			
			m_duration = _duration;
		}
		
		public void SetDelay(float _delay, bool _reset)
		{
			if (_reset)
				Reset();
			
			m_delay = _delay;
		}
		
		public void SetBackwardsAnimation(UiSimpleAnimationBase _backwardsAnimation, bool _reset)
		{
			if (_reset)
				Reset();
			
			m_backwardsAnimation = _backwardsAnimation;
		}
		
		// delegates
		private readonly UnityEvent m_onFinish = new();
		private readonly UnityEvent m_onFinishOnce = new();

		protected virtual void OnInitAnimate() { }
		protected virtual void OnBeginAnimate() { }
		protected virtual void OnEndAnimate() { }
		protected virtual void OnStopAnimate() { }
		protected virtual void OnLoop() { }
		protected virtual void OnAnimate(float _normalizedTime) { }

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private float m_forwardsDelay;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private float m_backwardsDelay;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private float m_completeTime;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private float m_completeBackwardsTime;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private float m_currentTime;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_playing = false;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_pause = false;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_backwards = false;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_beginAnimateCalled;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_endAnimateCalled;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private int m_currentNumberOfLoops;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_slave;

		#region debug serialize member
#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
#endif
		#endregion
		private bool m_initAnimateDone = false;

#if DEBUG_SIMPLE_ANIMATION
			[SerializeField] protected bool m_debug;
#endif

		public bool Debug
		{
			get
			{
#if DEBUG_SIMPLE_ANIMATION
				return m_debug;
#else
				return false;
#endif
			}
			
			// ReSharper disable once ValueParameterNotUsed
			set
			{
#if DEBUG_SIMPLE_ANIMATION
				m_debug = value;
#endif
			}
		}
		
		protected virtual void OnEnable()
		{
			if (m_autoOnEnable)
			{
				Log("auto on enable");
				Reset();
				Play(m_backwards);
			}
		}

		protected virtual void OnDisable()
		{
			if (m_autoOnEnable && IsPlaying)
				Reset();
		}
		
		public UnityEvent OnFinish => m_onFinish;
		public UnityEvent OnFinishOnce => m_onFinishOnce;
		
		public void Play() => Play(false, null);
		public void Play(bool _backwards) => Play(_backwards, null);
		
		public IEnumerator PlayUntilFinished()
		{
			yield return PlayUntilFinished(false, null);
		}

		public IEnumerator PlayUntilFinished(bool _backwards)
		{
			yield return PlayUntilFinished(_backwards, null);
		}

		//FIXME: does not work with dedicated backwards animation
		public IEnumerator PlayUntilFinished(bool _backwards, Action _onFinishOnce)
		{
			Play(_backwards, _onFinishOnce);
			yield return 0;
			while (IsPlaying)
				yield return 0;
		}
		
		public virtual void Play(bool _backwards, Action _onFinishOnce)
		{
			Log($"Play({_backwards})");
			if (!enabled)
			{
				UiLog.LogWarning($"Animation on '{gameObject.GetPath()}' was disabled - enabling it, otherwise it won't play properly");
				enabled = true;
			}

			m_backwards = _backwards;
			if (_onFinishOnce != null)
			{
				Log("Add m_onFinishOnce listener");
				m_onFinishOnce.AddListener(() => _onFinishOnce());
			}
			
			InitAnimateIfNecessary();
			m_completeTime = m_completeBackwardsTime = 0;
			CalculateCompleteTimeRecursive(ref m_completeTime, ref m_completeBackwardsTime, 0);
			
// Workaround #95		
if (m_timingWorkaroundActive)
	m_completeBackwardsTime += m_timingWorkaroundBackwardsBias;
//

			PlayRecursive(m_completeTime, m_completeBackwardsTime, 0, true, m_backwards, m_setLoopsForSlaves ? m_numberOfLoops : DONT_SET_LOOPS);
		}

#if UNITY_EDITOR
		public void EditorPlay(bool _backwards)
		{
			Log($"EditorPlay({_backwards})");
			m_initAnimateDone = false;
			Play(_backwards);
		}
#endif
		public void Pause()
		{
			Log("Pause()");
			if (IsPlaying)
				m_pause = true;
		}

		public void Resume()
		{
			Log("Resume()");
			m_pause = false;
		}

		public void Stop(bool _invokeOnStopDelegates = true)
		{
			Log($"Stop({_invokeOnStopDelegates})");
			if (!IsPlaying)
			{
				Log("Not running, returning");
				return;
			}
			FinishAnimation(_invokeOnStopDelegates);
		}

		public virtual void Reset(bool _toEnd = false)
		{
			Log($"Reset({_toEnd})");
			Stop();
			InitAnimateIfNecessary();
			ResetRecursive(_toEnd);
			float _ = 0;
			float completeDuration = 0;
			CalculateCompleteTimeRecursive(ref completeDuration, ref _, 0);
			m_currentTime = _toEnd ? completeDuration : 0;
			
			OnBeginAnimate();
			OnAnimate(_toEnd ? 1 : 0);
			OnEndAnimate();
		}

		protected virtual void Awake()
		{
		}

		protected virtual void Start()
		{
			Log("Start()");
			if (m_slave)
			{
				Log("is slave, returning");
				return;
			}

			if (m_autoStart)
			{
				Log("auto start");
				Play(m_backwards);
				return;
			}

			if (m_setOnStart)
			{
				Log("set on start");
				InitAnimateIfNecessary();
				OnAnimate(m_backwards ? 1 : 0);
			}
		}

		protected virtual void Update()
		{
			Update(Time.deltaTime);
		}

		protected virtual void Update(float _timeDelta)
		{
			// first check running status.
			if (!IsPlaying || m_pause)
				return;

			Log($"Update({_timeDelta})");
			
			_timeDelta *= m_animationSpeed;
			var animationSpeedInverse = 1 / m_animationSpeed;
			var duration = m_duration * animationSpeedInverse;

			if (m_backwards)
			{
				Log("Backwards playing");

				if (!m_backwardsPlayable)
				{
					Log("Not backwards playable -> stopping");
					if (m_gotoStartOnBackwards)
						OnAnimate(0);
					IsPlaying = false;
					FinishAnimation(true);
					return;
				}

				if (m_backwardsAnimation)
				{
					Log("Update backwards animation and return");
					m_backwardsAnimation.Update(_timeDelta);
					m_playing = m_backwardsAnimation.IsPlaying;
					if (!IsPlaying)
						FinishAnimation(true);
					return;
				}
			}

			// calculate current time and wait for delay finished
			m_currentTime += _timeDelta;
			
// Workaround #95		
if (m_timingWorkaroundActive)
	m_backwardsDelay = 0;
//

			float delay = m_backwards ? m_backwardsDelay : m_forwardsDelay;
			delay *= animationSpeedInverse;
			if (m_currentTime < delay)
				return;

			// tell the subclass, that now the real animation will begin.
			if (!m_beginAnimateCalled)
			{
				Log("Call OnBeginAnimate()");
				OnBeginAnimate();
				m_beginAnimateCalled = true;
			}

			// do the animation itself.
			bool durationExceeded = false;
			if (duration <= 0)
				durationExceeded = true;
			else
			{
				float normalizedTime = (m_currentTime - delay) / duration;

				if (normalizedTime <= 1)
				{
					float time = m_backwards ? 1.0f - normalizedTime : normalizedTime;
					Log($"Call OnAnimate({time})");
					OnAnimate(time);
				}
				else
				{
					durationExceeded = true;
				}
			}

			// tell the subclass, that now the real animation ends
			if (durationExceeded && !m_endAnimateCalled)
			{
				float time = m_backwards ? 0 : 1;
				Log($"Duration exceeded, call OnAnimate({time})");
				OnAnimate(time);
				Log("Call OnEndAnimate()");
				OnEndAnimate();
				m_endAnimateCalled = true;
			}

			float completeTime = m_backwards ? m_completeBackwardsTime : m_completeTime;
			completeTime *= animationSpeedInverse;

			// wait for the complete end
			if (m_currentTime < completeTime)
				return;

			// handle looping
			bool infiniteLoops = m_currentNumberOfLoops == INFINITE_LOOPS;
			bool loop = m_currentNumberOfLoops > 0;

			if (loop)
				m_currentNumberOfLoops--;

			if (loop || infiniteLoops)
			{
				Log($"Call OnLoop(), loop:{loop}, infiniteLoops:{infiniteLoops} m_currentNumberOfLoops: {m_currentNumberOfLoops}");

				OnLoop();
				m_currentTime %= completeTime;
				OnAnimate(m_backwards ? 1 : 0);
				m_beginAnimateCalled = false;
				m_endAnimateCalled = false;
				return;
			}

			FinishAnimation(true);
		}

		protected override void OnScreenResolutionChanged( ScreenResolution _oldScreenResolution, ScreenResolution _newScreenResolution )
		{
			if (!_oldScreenResolution.IsInvalid)
			{
				Log($"Screen resolution changed to {_newScreenResolution}, stopping (finishing) animation");
				Stop();
			}
		}

		private void FinishAnimation(bool _invokeOnStopDelegates)
		{
			Log($"FinishAnimation({_invokeOnStopDelegates})");

			if (m_backwardsAnimation != null)
				m_backwardsAnimation.Stop(false);

			IsPlaying = false;
			m_pause = false;
			OnStopAnimate();

			if (_invokeOnStopDelegates)
			{
				m_onFinish.Invoke();
				Log("Invoke m_onFinishOnce listeners");
				m_onFinishOnce.Invoke();
			}

			Log("m_onFinishOnce Remove All Listeners");
			m_onFinishOnce.RemoveAllListeners();
		}

		private void PlayRecursive(float _completeTime, float _completeBackwardsTime, float _completeForwardsDelay, bool _master, bool _backwards, int _loops)
		{
			InitAnimateIfNecessary();
			OnAnimate(_backwards ? 1 : 0);

			m_completeTime = _completeTime;
			m_completeBackwardsTime = _completeBackwardsTime;
			m_slave = !_master;
			m_backwards = _backwards;
			m_currentTime = 0;
			m_beginAnimateCalled = false;
			m_endAnimateCalled = false;
			if (_loops != DONT_SET_LOOPS)
				m_numberOfLoops = _loops;
			m_currentNumberOfLoops = m_numberOfLoops;

			m_forwardsDelay = _completeForwardsDelay + m_delay;
			m_backwardsDelay = m_completeBackwardsTime - m_forwardsDelay - m_duration;
			IsPlaying = true;

			if (m_backwards)
			{
				if (!m_backwardsPlayable)
					return;

				if (m_backwardsAnimation)
				{
					m_backwardsAnimation.Play();
					return;
				}
			}
			else
			{
				if (m_backwardsAnimation)
					m_backwardsAnimation.Stop(false);
			}

			IterateSlaveAnimations(slave =>
			{
				if (m_setSpeedForSlaves)
					slave.m_animationSpeed = m_animationSpeed;
				slave.PlayRecursive(_completeTime, _completeBackwardsTime, m_forwardsDelay, false, _backwards, _loops);
			});
		}

		private void CalculateCompleteTimeRecursive(ref float _completeTime, ref float _completeBackwardsTime, float _completeDelay)
		{
			_completeTime = Mathf.Max(_completeTime, _completeDelay + m_delay + m_duration);

			float dummy = 0;
			float backwardsDuration = m_delay;
			if (m_backwardsPlayable)
			{
				if (m_backwardsAnimation)
				{
					m_backwardsAnimation.CalculateCompleteTimeRecursive(ref dummy, ref backwardsDuration, 0);
				}
				else
				{
					backwardsDuration += m_duration;
				}
			}

			_completeBackwardsTime = Mathf.Max(_completeBackwardsTime, _completeDelay + backwardsDuration);
			
			var len = m_slaveAnimations.Count;
			
			if (m_backwardsPlayable && m_backwardsAnimation)
			{
				// can not use IterateSlaveAnimations - stupid c# doesn't allow ref in lambda
				for (int i = 0; i < len; i++)
				{
					var slave = m_slaveAnimations[i];
					
					if (slave == null)
					{
						UiLog.LogError($"Slave animation at index {i} in '{gameObject.GetPath()}' is null");
						continue;
					}
					
					slave.CalculateCompleteTimeRecursive(ref _completeTime, ref dummy, _completeDelay + m_delay);
				}
				return;
			}

			// can not use IterateSlaveAnimations - stupid c# doesn't allow ref in lambda
			for (int i = 0; i < len; i++)
			{
				var slave = m_slaveAnimations[i];
				
				if (slave == null)
				{
					UiLog.LogError($"Slave animation at index {i} in '{gameObject.GetPath()}' is null");
					continue;
				}
					
				slave.CalculateCompleteTimeRecursive(ref _completeTime, ref _completeBackwardsTime, _completeDelay + m_delay);
			}
		}

		private void ResetRecursive(bool _toEnd)
		{
			Log($"ResetRecursive({_toEnd})");

			if (_toEnd)
				OnAnimate(1);
			
			IterateSlaveAnimations(slave => slave.ResetRecursive(_toEnd));
			
			if (!_toEnd)
				OnAnimate(0);
		}

		private void InitAnimateIfNecessary()
		{
			if (m_initAnimateDone)
				return;

			Log("Call OnInitAnimate()");

			OnInitAnimate();
			m_initAnimateDone = true;
		}

		private void IterateSlaveAnimations(Action<UiSimpleAnimationBase> _action)
		{
			var len = m_slaveAnimations.Count;
			for (int i = 0; i < len; i++)
			{
				var slave = m_slaveAnimations[i];
				
				if (slave == null)
				{
					UiLog.LogError($"Slave animation at index {i} in '{gameObject.GetPath()}' is null");
					continue;
				}
				
				_action(slave);
			}
		}
		
		public void ShowViewAnimation(UnityAction _onFinishOnce = null)
		{
			if (!m_supportViewAnimations)
			{
				_onFinishOnce?.Invoke();
				return;
			}

			if (_onFinishOnce != null)
			{
				Log("Add m_onFinishOnce listener");
				m_onFinishOnce.AddListener(_onFinishOnce);
			}

			Play();
		}

		public void HideViewAnimation(UnityAction _onFinishOnce = null)
		{
			if (!m_supportViewAnimations)
			{
				_onFinishOnce?.Invoke();
				return;
			}

			if (_onFinishOnce != null)
			{
				Log("Add m_onFinishOnce listener");
				m_onFinishOnce.AddListener(_onFinishOnce);
			}
			Play(true);
		}

		public void StopViewAnimation(bool _visible)
		{
			if (!m_supportViewAnimations)
				return;

			Log("m_onFinishOnce Remove All Listeners");
			m_onFinishOnce.RemoveAllListeners();
			Reset(_visible);
		}

		[System.Diagnostics.Conditional("DEBUG_SIMPLE_ANIMATION")]
		public void Log(string _s)
		{
#if DEBUG_SIMPLE_ANIMATION
				if (!m_debug)
					return;
#endif
			UiLog.Log($"{GetType().Name} {gameObject.name}:{_s}\n{gameObject.GetPath()}");
		}

#if UNITY_EDITOR
		public void UpdateInEditor(float _deltaTime)
		{
			Update(_deltaTime);
		}
#endif
	}
}
