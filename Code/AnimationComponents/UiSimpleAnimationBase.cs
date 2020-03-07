using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSimpleAnimationBase : MonoBehaviour, IShowHideViewAnimation
	{
		protected const int INFINITE_LOOPS = -1;
		private const int DONT_SET_LOOPS = -2;

		// Timing values
		[Tooltip("Duration of this animation, excluding delay.")]
		[SerializeField]
		protected float m_duration = 1;

		[Tooltip("Delay for the beginning of the animation (when played forwards)")]
		[SerializeField]
		protected float m_delay = 0;

		[Tooltip("If set to false, this animation is skipped when playing backwards")]
		[SerializeField]
		protected bool m_backwardsPlayable = true;

		[Tooltip("If set, this animation is played instead when playing backwards")]
		[SerializeField]
		protected UiSimpleAnimationBase m_backwardsAnimation;

		[Tooltip("If set to true, the animation instantly goes to start when played backwards")]
		[SerializeField]
		protected bool m_gotoStartOnBackwards = false;

		[Tooltip("Automatically start the animation as soon as it becomes visible")]
		[SerializeField]
		protected bool m_autoStart = false;

		[Tooltip("Set the animation beginning values as it becomes visible, but don't start it")]
		[SerializeField]
		protected bool m_setOnStart = true;

		[Tooltip("Number of loops. -1: infinite loops 0: no loops, >0: Arbitrary number of loops")]
		[SerializeField]
		protected int m_numberOfLoops = 0;

		// Slave animations

		[Tooltip("Slave animations which are automatically started when this animation is started.")]
		[SerializeField]
		protected UiSimpleAnimationBase[] m_slaveAnimations = new UiSimpleAnimationBase[0];

		[SerializeField]
		protected bool m_setLoopsForSlaves = true;

		[SerializeField]
		protected bool m_supportViewAnimations = false;

		public UiSimpleAnimationBase[] SlaveAnimations { get {return m_slaveAnimations; }}
		public bool Running { get { return m_running; }}
		public float Duration { get { return m_duration; } set { Reset(); m_duration = value; }}
		public float Delay { get { return m_delay; } set { Reset(); m_delay = value; } }

		// delegates
		public Action m_onFinish;
		public Action m_onFinishOnce;

		protected virtual void OnInitAnimate(){}
		protected virtual void OnBeginAnimate(){}
		protected virtual void OnEndAnimate(){}
		protected virtual void OnStopAnimate(){}
		protected virtual void OnLoop(){}
		protected virtual void OnAnimate(float _normalizedTime){}

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
		private bool m_running = false;

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
		private bool m_master;

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

		public void Play(bool _backwards = false)
		{
			m_backwards = _backwards;
			InitAnimateIfNecessary();
			m_completeTime = m_completeBackwardsTime = 0;
			CalculateCompleteTimeRecursive(ref m_completeTime, ref m_completeBackwardsTime, 0);
			PlayRecursive(m_completeTime, m_completeBackwardsTime, 0, true, m_backwards, m_setLoopsForSlaves ? m_numberOfLoops : DONT_SET_LOOPS);
		}

		public void Pause()
		{
			if (m_running)
				m_pause = true;
		}

		public void Resume()
		{
			m_pause = false;
		}

		public void Stop()
		{
			if (!m_running)
				return;
			FinishAnimation();
		}

		public void Reset(bool _toEnd = false)
		{
			Stop();
			InitAnimateIfNecessary();
			ResetRecursive(_toEnd);
		}

		protected virtual void Awake()
		{
			
		}

		protected virtual void Start()
		{
			if (m_slave)
				return;

			if (m_autoStart)
			{
				Play(m_backwards);
				return;
			}

			if (m_setOnStart)
			{
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
			if (!m_running || m_pause)
				return;

			if (m_backwards)
			{
				if (!m_backwardsPlayable)
				{
					if (m_gotoStartOnBackwards)
						OnAnimate(0);
					m_running = false;
					FinishAnimation();
					return;
				}

				if (m_backwardsAnimation)
				{
					m_backwardsAnimation.Update(_timeDelta);
					m_running = m_backwardsAnimation.m_running;
					if (!m_running)
						FinishAnimation();
					return;
				}
			}

			// calculate current time and wait for delay finished
			m_currentTime += _timeDelta;
			float delay = m_backwards ? m_backwardsDelay : m_forwardsDelay;
			if (m_currentTime < delay)
				return;

			// tell the subclass, that now the real animation will begin.
			if (!m_beginAnimateCalled)
			{
				OnBeginAnimate();
				m_beginAnimateCalled = true;
			}

			// do the animation itself.
			bool durationExceeded = false;
			if (m_duration <= 0)
				durationExceeded = true;
			else
			{
				float normalizedTime = (m_currentTime-delay) / m_duration;

				if (normalizedTime <= 1)
					OnAnimate(m_backwards ? 1.0f - normalizedTime : normalizedTime);
				else
					durationExceeded = true;
			}

			// tell the subclass, that now the real animation ends
			if (durationExceeded && !m_endAnimateCalled)
			{
				OnAnimate(m_backwards ? 0 : 1);
				OnEndAnimate();
				m_endAnimateCalled = true;
			}

			float completeTime = m_backwards ? m_completeBackwardsTime : m_completeTime;

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
				OnLoop();
				m_currentTime %= completeTime;
				OnAnimate(m_backwards ? 1 : 0);
				m_beginAnimateCalled = false;
				m_endAnimateCalled = false;
				return;
			}

			FinishAnimation();
		}

		private void FinishAnimation()
		{
			//TODO call delegates etc.
			m_running = false;
			m_pause = false;
			OnStopAnimate();

			if (m_onFinish != null)
				m_onFinish.Invoke();
			if (m_onFinishOnce != null)
				m_onFinishOnce.Invoke();
			m_onFinishOnce = null;
		}

		private void PlayRecursive( float _completeTime, float _completeBackwardsTime, float _completeForwardsDelay, bool _master, bool _backwards, int _loops )
		{
			InitAnimateIfNecessary();
			OnAnimate(_backwards ? 1:0);

			m_completeTime = _completeTime;
			m_completeBackwardsTime = _completeBackwardsTime;
			m_master = _master;
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
			m_running = true;

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

			foreach( var slave in m_slaveAnimations)
				slave.PlayRecursive(_completeTime, _completeBackwardsTime, m_forwardsDelay, false, _backwards, _loops);
		}

		private void CalculateCompleteTimeRecursive( ref float _completeTime, ref float _completeBackwardsTime, float _completeDelay )
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

			_completeBackwardsTime = Mathf.Max(_completeBackwardsTime, _completeDelay + backwardsDuration );

			if (m_backwardsPlayable && m_backwardsAnimation)
			{
				foreach( var slave in m_slaveAnimations )
					slave.CalculateCompleteTimeRecursive( ref _completeTime, ref dummy, _completeDelay + m_delay );
				return;
			}

			foreach( var slave in m_slaveAnimations )
				slave.CalculateCompleteTimeRecursive( ref _completeTime, ref _completeBackwardsTime, _completeDelay + m_delay );
		}

		private void ResetRecursive(bool _toEnd)
		{
			if (_toEnd)
				OnAnimate(_toEnd ? 1:0);
			foreach( var slave in m_slaveAnimations )
				slave.ResetRecursive(_toEnd);
			if (!_toEnd)
				OnAnimate(_toEnd ? 1:0);
		}

		private void InitAnimateIfNecessary()
		{
			if (m_initAnimateDone)
				return;

			OnInitAnimate();
			m_initAnimateDone = true;
		}

		public void ShowViewAnimation( Action _onFinish = null )
		{
			if (!m_supportViewAnimations)
			{
				if (_onFinish != null)
					_onFinish.Invoke();
				return;
			}

			m_onFinishOnce = _onFinish;
			Play();
		}

		public void HideViewAnimation( Action _onFinish = null )
		{
			if (!m_supportViewAnimations)
			{
				if (_onFinish != null)
					_onFinish.Invoke();
				return;
			}

			m_onFinishOnce = _onFinish;
			Play(true);
		}

		public void StopViewAnimation()
		{
			if (!m_supportViewAnimations)
				return;

			m_onFinishOnce = null;
			Stop();
		}

#if UNITY_EDITOR
		public void UpdateInEditor(float _deltaTime)
		{
			Update(_deltaTime);
		}

#endif
	}
}