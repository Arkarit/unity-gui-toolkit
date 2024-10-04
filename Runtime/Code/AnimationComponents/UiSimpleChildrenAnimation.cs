using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSimpleChildrenAnimation : UiSimpleAnimationBase
	{
		[SerializeField] private float m_delayPerChild = 0;
		private readonly List<UiSimpleAnimationBase> m_animations = new();

		protected override void OnEnable()
		{
			base.OnEnable();
			CollectChildren();
		}

		private void OnTransformChildrenChanged()
		{
			Stop();
			CollectChildren();
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
				
			CollectChildren();
			base.Play(_backwards, _onFinishOnce);
		}

		public override void Reset(bool _toEnd = false)
		{
			CollectChildren();
			base.Reset(_toEnd);
		}
		
		private void CollectChildren()
		{
			m_slaveAnimations.Clear();
			transform.GetComponentsInDirectChildren(m_animations);
			float delay = 0;
			foreach (var animation in m_animations)
			{
				animation.Delay = delay;
				m_slaveAnimations.Add(animation);
				delay += m_delayPerChild;
			}
		}

		private void OnValidate()
		{
			CollectChildren();
		}
	}
}
