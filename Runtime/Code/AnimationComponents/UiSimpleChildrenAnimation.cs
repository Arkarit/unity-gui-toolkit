using System;
using System.Collections;
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
					CollectChildren(null, false);
			}
		}
		
		protected override void OnEnable()
		{
			// We need to wait for one frame for the children to also become enabled
			if (m_autoOnEnable)
			{
				// But reset has to be done instantly; otherwise we see the unanimated items for one frame
				if (m_autoCollectChildren)
					CollectChildren(null, true);
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

		public void CollectChildren(Transform tf = null, bool _includeInactive = true)
		{
			if (tf == null)
				tf = transform;
			
			tf.GetComponentsInDirectChildren(m_childAnimations, _includeInactive);
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
				CollectChildren(null, false);
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
