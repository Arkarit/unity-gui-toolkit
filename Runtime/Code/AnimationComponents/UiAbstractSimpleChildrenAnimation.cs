using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiAbstractSimpleChildrenAnimation : UiSimpleAnimationBase
	{
		[SerializeField] protected bool m_autoCollectChildren = true;
		[SerializeField] protected List<UiSimpleAnimationBase> m_childAnimations = new();
		[SerializeField][Optional] protected RectTransform m_container;
		
		// This event is invoked only if m_autoCollectChildren is false.
		// You can collect the children in that case and set them via ChildAnimations
		public readonly CEvent<UiAbstractSimpleChildrenAnimation> ShouldCollectChildren = new();

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
					CollectChildren(m_container, false);
			}
		}
		
		
		protected abstract void InitChildren();
		protected abstract void DoPlay();
		
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
			DoPlay();
		}

		private void OnTransformChildrenChanged()
		{
			Stop();
			CollectChildrenOrInvokeEvent();
		}

		public void CollectChildren(Transform tf = null, bool _includeInactive = true)
		{
			if (tf == null)
				tf = transform;
			
			tf.GetComponentsInDirectChildren(m_childAnimations, _includeInactive);
			InitChildren();
		}


		public override void Reset(bool _toEnd = false)
		{
			CollectChildrenOrInvokeEvent();
			base.Reset(_toEnd);
		}
		
		protected void CollectChildrenOrInvokeEvent()
		{
			if (m_autoCollectChildren)
			{
				CollectChildren(m_container, false);
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
