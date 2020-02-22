using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSimpleAnimationBase : MonoBehaviour
	{
		protected const int INFINITE_LOOPS = -1;

		// Timing values
		protected float m_duration = 1;
		protected float m_delay = 0;
		protected bool m_autoStart = false;
		protected bool m_setOnStart = true;
		protected int m_numberOfLoops = 0;

		// Slave animations
		protected UiSimpleAnimationBase[] m_slaveAnimations;
		
		protected virtual void OnInitAnimate(){}
		protected virtual void OnBeginAnimate(){}
		protected virtual void OnEndAnimate(){}
		protected virtual void OnStopAnimate(){}
		protected virtual void OnLoop(){}
		protected virtual void OnAnimate(float _normalizedTime){}

		private float m_forwardsDelay;
		private float m_backwardsDelay;
		private float m_completeTime;
		private float m_currentTime;

		private bool m_running = false;
		private bool m_pause = false;
		private bool m_backwards = false;
		private bool m_beginAnimateCalled;
		private bool m_endAnimateCalled;
		private int m_currentNumberOfLoops;

		private bool m_master;
		private bool m_slave;

		private bool m_initAnimateDone = false;

		public void Play(bool _backwards = false)
		{
			m_backwards = _backwards;
			InitAnimateIfNecessary();
			m_completeTime = 0;
			CalculateCompleteTimeRecursive(ref m_completeTime);
			PlayRecursive(m_completeTime, 0, true, m_backwards);
		}

		public void Pause()
		{

		}

		public void Resume()
		{

		}

		public void Stop()
		{

		}


		protected virtual void Awake()
		{
			
		}

		protected virtual void Start()
		{
			
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

			// wait for the complete end
			if (m_currentTime < m_completeTime)
				return;

			// handle looping
			bool infiniteLoops = m_currentNumberOfLoops == INFINITE_LOOPS;
			bool loop = m_currentNumberOfLoops > 0;
			if (loop)
				m_currentNumberOfLoops--;
			if (loop || infiniteLoops)
			{
				OnLoop();
				m_currentTime %= m_completeTime;
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
		}

		private void PlayRecursive( float _completeTime, float _completeForwardsDelay, bool _master, bool _backwards )
		{
			m_completeTime = _completeTime;
			m_master = _master;
			m_slave = !_master;
			m_backwards = _backwards;
			m_currentTime = 0;
			m_beginAnimateCalled = false;
			m_endAnimateCalled = false;
			m_currentNumberOfLoops = m_numberOfLoops;

			m_forwardsDelay = _completeForwardsDelay + m_delay;
			m_backwardsDelay = m_completeTime - m_forwardsDelay - m_duration;
			m_running = true;

			foreach( var slave in m_slaveAnimations)
				slave.PlayRecursive(_completeTime, m_forwardsDelay, false, _backwards);
		}

		private void CalculateCompleteTimeRecursive( ref float _completeTime )
		{
			_completeTime = Mathf.Max(_completeTime, m_delay + m_duration);
			foreach( var slave in m_slaveAnimations )
				slave.CalculateCompleteTimeRecursive( ref _completeTime );
		}

		private void InitAnimateIfNecessary()
		{
			if (m_initAnimateDone)
				return;

			OnInitAnimate();
			m_initAnimateDone = true;
		}
	}
}