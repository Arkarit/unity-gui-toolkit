using System;
using UnityEngine;

namespace GuiToolkit.UiStateSystem
{
	[Serializable]
	public class UiTransition
	{
		[SerializeField]
		private UiStateMachine m_stateMachine;
		[SerializeField]
		private string m_to;
		[SerializeField]
		private string m_from;
		[SerializeField]
		private float m_duration;
		[SerializeField]
		private float m_delay;
		[SerializeField]
		private AnimationCurve m_animationCurve;

		public string To
		{
			get
			{
				return m_to;
			}
		}

		public string From
		{
			get
			{
				return m_from;
			}
		}

		public UiStateMachine StateMachine
		{
			get
			{
				return m_stateMachine;
			}
		}

		public bool Eval( float _currentTime, out float _val )
		{
			float normalizedTime = (_currentTime-m_delay) / m_duration;

if (m_stateMachine != null)
Debug.Log($"GO:{m_stateMachine.gameObject} normalizedTime:{normalizedTime}");

			if (normalizedTime < 0)
			{
				_val = m_animationCurve.Evaluate(0);
Debug.Log("In Delay");
				return false;
			}

			if (normalizedTime < 1)
			{
				_val = m_animationCurve.Evaluate(normalizedTime);
Debug.Log("In Transition");
				return false;
			}

Debug.Log("End");
			_val = m_animationCurve.Evaluate(1);
			return true;
		}
	}

}
