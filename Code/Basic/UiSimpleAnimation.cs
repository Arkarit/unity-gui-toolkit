using System;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiSimpleAnimation : UiSimpleAnimationBase
	{
		[Flags]
		public enum ESupport
		{
			PositionX		= 0x0001,
			PositionY		= 0x0002,
		}
		[SerializeField]
		protected ESupport m_support;

		[SerializeField]
		protected RectTransform m_target;

		// position

		[SerializeField]
		protected float m_posXStart;
		[SerializeField]
		protected float m_posXEnd;
		[SerializeField]
		protected AnimationCurve m_posXCurve;

		[SerializeField]
		protected float m_posYStart;
		[SerializeField]
		protected float m_posYEnd;
		[SerializeField]
		protected AnimationCurve m_posYCurve;

		public ESupport Support { get {return m_support;}}

		protected override void OnAnimate(float _normalizedTime)
		{
			if( m_support.HasFlags(ESupport.PositionX | ESupport.PositionY))
			{
				AnimatePosition(_normalizedTime);
			}
		}

		private void AnimatePosition( float normalizedTime )
		{
			Vector2 pos = m_target.anchoredPosition;

			if (m_support.HasFlags(ESupport.PositionX))
				pos.x = Mathf.Lerp(m_posXStart, m_posXEnd, m_posXCurve.Evaluate(normalizedTime));

			if (m_support.HasFlags(ESupport.PositionY))
				pos.y = Mathf.Lerp(m_posYStart, m_posYEnd, m_posYCurve.Evaluate(normalizedTime));

			m_target.anchoredPosition = pos;
		}
	}
}