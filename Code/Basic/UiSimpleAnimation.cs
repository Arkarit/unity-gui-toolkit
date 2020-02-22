using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiSimpleAnimation : UiSimpleAnimationBase
	{
		[Flags]
		public enum ESupport
		{
			PositionX		= 0x00000001,
			PositionY		= 0x00000002,
			RotationZ		= 0x00000100,
			ScaleX			= 0x00001000,
			ScaleY			= 0x00002000,
			Alpha			= 0x00010000,
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

		// rotation

		[SerializeField]
		protected float m_rotZStart;
		[SerializeField]
		protected float m_rotZEnd;
		[SerializeField]
		protected AnimationCurve m_rotZCurve;

		// scale

		[SerializeField]
		protected float m_scaleXStart;
		[SerializeField]
		protected float m_scaleXEnd;
		[SerializeField]
		protected AnimationCurve m_scaleXCurve;

		[SerializeField]
		protected float m_scaleYStart;
		[SerializeField]
		protected float m_scaleYEnd;
		[SerializeField]
		protected AnimationCurve m_scaleYCurve;

		[SerializeField]
		protected bool m_scaleLocked;

		// alpha

		[SerializeField]
		protected AnimationCurve m_alphaCurve;
		[SerializeField]
		protected Image m_alphaImage;
		[SerializeField]
		protected CanvasGroup m_alphaCanvasGroup;

		public ESupport Support { get {return m_support;}}

		protected override void OnAnimate(float _normalizedTime)
		{
			if( m_support.HasFlags(ESupport.PositionX | ESupport.PositionY))
				AnimatePosition(_normalizedTime);

			if (m_support.HasFlags(ESupport.RotationZ))
				AnimateRotation(_normalizedTime);

			if (m_support.HasFlags(ESupport.ScaleX | ESupport.ScaleY))
				AnimateScale(_normalizedTime);

			if (m_support.HasFlags(ESupport.Alpha))
				AnimateAlpha(_normalizedTime);
		}

		private void AnimatePosition( float _normalizedTime )
		{
			Vector2 pos = m_target.anchoredPosition;

			if (m_support.HasFlags(ESupport.PositionX))
				pos.x = Mathf.Lerp(m_posXStart, m_posXEnd, m_posXCurve.Evaluate(_normalizedTime));

			if (m_support.HasFlags(ESupport.PositionY))
				pos.y = Mathf.Lerp(m_posYStart, m_posYEnd, m_posYCurve.Evaluate(_normalizedTime));

			m_target.anchoredPosition = pos;
		}

		private void AnimateRotation( float _normalizedTime )
		{
			Quaternion rot = m_target.localRotation;
			Vector3 angles = rot.eulerAngles;
			angles.z = Mathf.Lerp(m_rotZStart, m_rotZEnd, m_rotZCurve.Evaluate(_normalizedTime));
			rot.eulerAngles = angles;
			m_target.localRotation = rot;
		}

		private void AnimateScale( float _normalizedTime )
		{
			Vector3 scale = m_target.localScale;

			if (m_scaleLocked || m_support.HasFlags(ESupport.ScaleX))
				scale.x = Mathf.Lerp(m_scaleXStart, m_scaleXEnd, m_scaleXCurve.Evaluate(_normalizedTime));

			if (m_scaleLocked)
				scale.y = scale.x;
			else if (m_support.HasFlags(ESupport.ScaleY))
				scale.y = Mathf.Lerp(m_scaleYStart, m_scaleYEnd, m_scaleYCurve.Evaluate(_normalizedTime));

			m_target.localScale = scale;
		}

		private void AnimateAlpha( float _normalizedTime )
		{
			float alpha = m_alphaCurve.Evaluate(_normalizedTime);
			if (m_alphaImage != null)
			{
				Color c = m_alphaImage.color;
				c.a = alpha;
				m_alphaImage.color = c;
			}

			if (m_alphaCanvasGroup != null)
				m_alphaCanvasGroup.alpha = alpha;
		}

	}
}