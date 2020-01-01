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
			float normalizedTime = _currentTime / m_duration;
			if (normalizedTime < 1)
			{
				_val = m_animationCurve.Evaluate(normalizedTime);
				return false;
			}

			_val = m_animationCurve.Evaluate(1);
			return true;
		}
	}

}
