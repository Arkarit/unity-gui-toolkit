using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	public class UiSimpleAnimationMouseOver : UiSimpleAnimation, IPointerEnterHandler, IPointerExitHandler
	{
		// We have a separate slave animation for hover to avoid circular referencing
		[SerializeField] private UiSimpleAnimationBase m_optionalMouseOverSlaveAnimation;
		[SerializeField] private bool m_optionalMouseOverSlaveAnimationReset = true;
		
		private int m_count;

		public bool PauseMouseOverAnimation {get; set;}
		
		protected override void OnEnable()
		{
			base.OnEnable();
			Reset();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Reset();
		}

		public override void Reset(bool _toEnd = false)
		{
			base.Reset(_toEnd);
			if (m_optionalMouseOverSlaveAnimation)
			{
				m_optionalMouseOverSlaveAnimation.Reset(_toEnd);
				if (m_optionalMouseOverSlaveAnimation is UiSimpleAnimationMouseOver slaveAnim)
					slaveAnim.PauseMouseOverAnimation = false;
			}
			
			m_count = 0;
		}

		public void PointerEnter()
		{
			m_count++;
			if (m_count > 1)
				return;
			
			if (!PauseMouseOverAnimation)
				Play(false);
		}

		public void PointerExit()
		{
			m_count--;
			if (m_count > 0)
				return;
			
			// Ensure counter does not underflow
			m_count = 0;
			
			if (!PauseMouseOverAnimation)
				Play(true);
		}

		public override void Play(bool _backwards, Action _onFinishOnce)
		{
			base.Play(_backwards, _onFinishOnce);
			
			if (m_optionalMouseOverSlaveAnimation)
			{
				if (m_optionalMouseOverSlaveAnimationReset)
					m_optionalMouseOverSlaveAnimation.Reset(!_backwards);
				else
					m_optionalMouseOverSlaveAnimation.Play(_backwards);

				if (m_optionalMouseOverSlaveAnimation is UiSimpleAnimationMouseOver slaveAnim)
					slaveAnim.PauseMouseOverAnimation = !_backwards;
			}
		}

		public void OnPointerEnter(PointerEventData _)
		{
			PointerEnter();
		}

		public void OnPointerExit(PointerEventData _)
		{
			PointerExit();
		}
	}
}
