using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSimpleChildrenAnimation : UiSimpleAnimationBase
	{
		[SerializeField] private float m_delayPerChild = 0;
		[SerializeField] private bool m_autoCollectChildren = true;
		[SerializeField] private List<UiSimpleAnimationBase> m_childAnimations = new();
		
		// This event is invoked only if m_autoCollectChildren is false.
		// You can collect the children in that case and set them via ChildAnimations
		public readonly CEvent<UiSimpleChildrenAnimation> ShouldCollectChildren = new();

		public List<UiSimpleAnimationBase> ChildAnimations => m_childAnimations;
		public bool AutoCollectChildren
		{
			get => m_autoCollectChildren;
			set
			{
				if (m_autoCollectChildren == value)
					return;
				
				m_autoCollectChildren = value;
				if (m_autoCollectChildren)
					CollectChildren();
			}
		}
		
		protected override void OnEnable()
		{
			base.OnEnable();
			CollectChildrenOrInvokeEvent();
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

		public void CollectChildren(Transform tf = null)
		{
			if (tf == null)
				tf = transform;
			
			tf.GetComponentsInDirectChildren(m_childAnimations);
			InitChildren();
		}

		public void InitChildren()
		{
			float delay = 0;
			m_slaveAnimations.Clear();
			foreach (var animation in m_childAnimations)
			{
				animation.Delay = delay;
				m_slaveAnimations.Add(animation);
				delay += m_delayPerChild;
			}
		}

		public override void Reset(bool _toEnd = false)
		{
			CollectChildrenOrInvokeEvent();
			base.Reset(_toEnd);
		}
		
		private void CollectChildrenOrInvokeEvent()
		{
			if (m_autoCollectChildren)
			{
				CollectChildren();
				return;
			}
			
			ShouldCollectChildren.Invoke(this);
			InitChildren();
		}
		
		private void OnValidate()
		{
			m_slaveAnimations.Clear();
			CollectChildrenOrInvokeEvent();
		}
	}
}
