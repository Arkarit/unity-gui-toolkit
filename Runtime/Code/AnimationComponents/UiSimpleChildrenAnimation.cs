using System;
using System.Collections;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSimpleChildrenAnimation : UiAbstractSimpleChildrenAnimation
	{
		[Tooltip("A delay, which is added to every child")]
		[SerializeField] private float m_baseDelayPerChild = 0;
		[Tooltip("An increasing delay, which is added to every child. Negative values reverse the direction of children.")]
		[SerializeField] private float m_delayPerChild = 0;
		[Tooltip("Base Duration per child. If left 0, each child sets its duration itself")]
		[SerializeField] private float m_baseDurationPerChild = 0;
		[Tooltip("Duration per child. If left 0, each child sets its duration itself. Negative values reverse the direction of children.")]
		[SerializeField] private float m_durationPerChild = 0;

		protected override void DoPlay()
		{
			Play(IsBackwards);
		}

		protected override void OnEnable()
		{
			// We need to wait for one frame for the children to also become enabled
			if (m_autoOnEnable)
			{
				// But reset has to be done instantly; otherwise we see the unanimated items for one frame
				if (m_autoCollectChildren)
					CollectChildren(m_container, true);
				Reset();
				
				StartCoroutine(AutoOnEnableDelayed());
				return;
			}
			
			base.OnEnable();
		}
		
		IEnumerator AutoOnEnableDelayed()
		{
			yield return null;
			
			Log("auto on enable");
			CollectChildrenOrInvokeEvent();
			Play(IsBackwards);
		}

		private void OnTransformChildrenChanged()
		{
			Stop();
			CollectChildrenOrInvokeEvent();
		}

		public override void Play(bool _backwards, Action _onFinishOnce)
		{
			if (HasBackwardsAnimation)
			{
				if (_backwards)
				{
					m_slaveAnimations.Clear();
					base.Play(_backwards, () =>
					{
						Reset();
						_onFinishOnce?.Invoke();
					});
					
					return;
				}
				
				BackwardsAnimation.Reset();
			}
				
			CollectChildrenOrInvokeEvent();
			base.Play(_backwards, _onFinishOnce);
		}

		protected override void InitChildren()
		{
			float delay = 0;
			if (m_delayPerChild < 0)
				delay = -m_delayPerChild * (m_childAnimations.Count -1);
			
			float duration = m_durationPerChild;
			if (duration < 0)
				duration = -duration * m_childAnimations.Count;
			
			m_slaveAnimations.Clear();
			foreach (var animation in m_childAnimations)
			{
				if (m_delayPerChild != 0 || m_baseDelayPerChild > 0)
					animation.SetDelay(delay + m_baseDelayPerChild, false);
				
				if (m_durationPerChild != 0 || m_baseDurationPerChild > 0)
					animation.SetDuration(duration + m_baseDurationPerChild, false);
				
				m_slaveAnimations.Add(animation);
				
				delay += m_delayPerChild;
				duration += m_durationPerChild;
			}
		}
	}
}
