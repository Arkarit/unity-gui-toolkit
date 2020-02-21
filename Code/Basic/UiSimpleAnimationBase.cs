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
		protected virtual void OnStartAnimate(){}
		protected virtual void OnStopAnimate(){}
		protected virtual void OnLoop(){}

		private float m_forwardsDelay;
		private float m_backwardsDelay;
		private bool m_running = false;
		private bool m_pause = false;

		private bool m_master;
		private bool m_slave;

		private bool m_initAnimateDone = false;

		public void Play(bool _backwards = false)
		{
			InitAnimateIfNecessary();
		}

		public void Pause(bool _pause)
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