using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiSimpleAnimation : UiSimpleAnimationBase, IShowHideViewAnimation
	{
		[Flags]
		public enum ESupport
		{
			None			= 0,
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

		[SerializeField]
		private CanvasScaler m_canvasScaler;

		[SerializeField]
		private RectTransform m_canvasRectTransform;

		[SerializeField]
		protected bool m_scaleByCanvasScaler = true;

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

		// sound

		// animator

		// particles

		public ESupport Support { get {return m_support;}}

		protected CanvasScaler CanvasScaler
		{
			get
			{
				if (m_canvasScaler == null)
					m_canvasScaler = GetComponentInParent<CanvasScaler>();
				return m_canvasScaler;
			}

			set
			{
				m_canvasScaler = value;
			}
		}

		protected RectTransform CanvasRectTransform
		{
			get
			{
				if (m_canvasScaler == null)
					m_canvasScaler = GetComponentInParent<CanvasScaler>();
				if (m_canvasRectTransform == null)
					m_canvasRectTransform = (RectTransform) m_canvasScaler.transform;
				return m_canvasRectTransform;
			}
		}

		private bool m_animatePositionX;
		private bool m_animatePositionY;
		private bool m_animatePosition;
		private bool m_animateRotationZ;
		private bool m_animateScaleX;
		private bool m_animateScaleY;
		private bool m_animateScale;
		private bool m_animateAlpha;

		private float xRatio
		{
			get
			{
				if (!m_scaleByCanvasScaler)
					return 1;

				return CanvasRectTransform.sizeDelta.x / CanvasScaler.referenceResolution.x;
			}
		}

		private float yRatio
		{
			get
			{
				if (!m_scaleByCanvasScaler)
					return 1;

				return CanvasRectTransform.sizeDelta.y / CanvasScaler.referenceResolution.y;
			}
		}

		private void InitFlags()
		{
			// HasFlags() creates GC allocs, so we cache it here (OnAnimate(), where it is used, is called quite often)
			m_animatePositionX = m_support.HasFlags(ESupport.PositionX);
			m_animatePositionY = m_support.HasFlags(ESupport.PositionY);
			m_animatePosition = m_animatePositionX || m_animatePositionY;
			m_animateRotationZ = m_support.HasFlags(ESupport.RotationZ);
			m_animateScaleX = m_support.HasFlags(ESupport.ScaleX);
			m_animateScaleY = m_support.HasFlags(ESupport.ScaleY);
			m_animateScale = m_animateScaleX || m_animateScaleY;
			m_animateAlpha = m_support.HasFlags(ESupport.Alpha);
		}

		private void ClearFlags()
		{
			m_support = ESupport.None;
			m_animatePositionX = false;
			m_animatePositionY = false;
			m_animatePosition = false;
			m_animateRotationZ = false;
			m_animateScaleX = false;
			m_animateScaleY = false;
			m_animateScale = false;
			m_animateAlpha = false;
		}

		protected override void Awake()
		{
			base.Awake();
			if (m_canvasScaler == null && m_scaleByCanvasScaler)
				m_canvasScaler = GetComponentInParent<CanvasScaler>();

			InitFlags();
		}

		public void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards )
		{
			if (_stackAnimationType == EStackAnimationType.DontTouch)
				return;

			if (CanvasScaler == null)
			{
				Debug.LogError("Can not perform stack animation without canvas scaler!");
				return;
			}

			if (_backwards)
			{
				switch(_stackAnimationType)
				{
					default:
					case EStackAnimationType.DontTouch:
						Debug.Assert(false);
						break;
					case EStackAnimationType.LeftToRight:
						_stackAnimationType = EStackAnimationType.RightToLeft;
						break;
					case EStackAnimationType.RightToLeft:
						_stackAnimationType = EStackAnimationType.LeftToRight;
						break;
					case EStackAnimationType.TopToBottom:
						_stackAnimationType = EStackAnimationType.BottomToTop;
						break;
					case EStackAnimationType.BottomToTop:
						_stackAnimationType = EStackAnimationType.TopToBottom;
						break;
				}
			}

			ClearFlags();
			m_scaleByCanvasScaler = true;
			m_animatePosition = true;

			Vector2 res = CanvasScaler.referenceResolution;
			switch (_stackAnimationType)
			{
				default:
				case EStackAnimationType.DontTouch:
					Debug.Assert(false);
					break;
				case EStackAnimationType.LeftToRight:
					CheckCurve(false);
					m_support = ESupport.PositionX;
					m_animatePositionX = true;
					m_posXStart = -res.x;
					m_posXEnd = 0;
					break;
				case EStackAnimationType.RightToLeft:
					CheckCurve(false);
					m_support = ESupport.PositionX;
					m_animatePositionX = true;
					m_posXStart = res.x;
					m_posXEnd = 0;
					break;
				case EStackAnimationType.TopToBottom:
					CheckCurve(true);
					m_support = ESupport.PositionY;
					m_animatePositionY = true;
					m_posYStart = -res.y;
					m_posYEnd = 0;
					break;
				case EStackAnimationType.BottomToTop:
					CheckCurve(true);
					m_support = ESupport.PositionY;
					m_animatePositionY = true;
					m_posYStart = res.y;
					m_posYEnd = 0;
					break;
			}
		}

		private void CheckCurve( bool _isY )
		{
			bool curveMissing = _isY ? m_posYCurve == null || m_posYCurve.length == 0 : m_posXCurve == null || m_posXCurve.length == 0;
			if (!curveMissing)
				return;
			AnimationCurve curve = new AnimationCurve(new Keyframe(0,0,0,0), new Keyframe(1,1,0,0));
			if (_isY)
				m_posYCurve = curve;
			else
				m_posXCurve = curve;
		}

#if UNITY_EDITOR
		// In editor, while not play mode, we have no Awake, so we set the flags when initializing the animation
		protected override void OnInitAnimate()
		{
			if (!Application.isPlaying)
				InitFlags();
		}
#endif

		protected override void OnAnimate(float _normalizedTime)
		{
			if( m_animatePosition )
				AnimatePosition(_normalizedTime);

			if ( m_animateRotationZ )
				AnimateRotation(_normalizedTime);

			if ( m_animateScale )
				AnimateScale(_normalizedTime);

			if ( m_animateAlpha )
				AnimateAlpha(_normalizedTime);
		}

		private void AnimatePosition( float _normalizedTime )
		{
			Vector2 pos = m_target.anchoredPosition;

			if (m_animatePositionX)
				pos.x = Mathf.LerpUnclamped(m_posXStart, m_posXEnd, m_posXCurve.Evaluate(_normalizedTime)) * xRatio;

			if (m_animatePositionY)
				pos.y = Mathf.LerpUnclamped(m_posYStart, m_posYEnd, m_posYCurve.Evaluate(_normalizedTime)) * yRatio;

			m_target.anchoredPosition = pos;
		}

		private void AnimateRotation( float _normalizedTime )
		{
			Quaternion rot = m_target.localRotation;
			Vector3 angles = rot.eulerAngles;
			angles.z = Mathf.LerpUnclamped(m_rotZStart, m_rotZEnd, m_rotZCurve.Evaluate(_normalizedTime));
			rot.eulerAngles = angles;
			m_target.localRotation = rot;
		}

		private void AnimateScale( float _normalizedTime )
		{
			Vector3 scale = m_target.localScale;

			if (m_scaleLocked || m_animateScaleX)
				scale.x = Mathf.LerpUnclamped(m_scaleXStart, m_scaleXEnd, m_scaleXCurve.Evaluate(_normalizedTime));

			if (m_scaleLocked)
				scale.y = scale.x;
			else if (m_animateScaleY)
				scale.y = Mathf.LerpUnclamped(m_scaleYStart, m_scaleYEnd, m_scaleYCurve.Evaluate(_normalizedTime));

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