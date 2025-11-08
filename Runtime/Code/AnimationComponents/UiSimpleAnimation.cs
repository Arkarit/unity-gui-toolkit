using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	public class UiSimpleAnimation : UiSimpleAnimationBase
	{
		// Its so UTTERLY IDIOTIC that C# hasn't got a usable preprocessor.
		// This class would be the classic use case for a member definition macro.

		#region General Members
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
			Skew			= 0x00020000,
		}

		[SerializeField]
		protected ESupport m_support;

		[SerializeField]
		protected RectTransform m_target;

		[SerializeField]
		protected bool m_markTargetForLayoutRebuild;

		[SerializeField]
		protected bool m_scaleByCanvasScaler = true;

		private CanvasScaler m_canvasScaler;

		private RectTransform m_canvasRectTransform;

		public ESupport Support
		{
			get => m_support;
			set
			{
				if (m_support == value)
					return;
				
				Stop(); 
				m_support = value;
				InitFlags();
			}
		}

		public RectTransform Target
		{
			get => m_target;
			set { Stop(); m_target = value; }
		}

		public bool MarkTargetForLayoutRebuild
		{
			get => m_markTargetForLayoutRebuild;
			set { Stop(); m_markTargetForLayoutRebuild = value; }
		}

		public CanvasScaler CanvasScaler
		{
			get
			{
				if (m_canvasScaler == null)
					m_canvasScaler = GetComponentInParent<CanvasScaler>();
				return m_canvasScaler;
			}

			set
			{
				Stop();
				m_canvasScaler = value;
			}
		}

		public RectTransform CanvasRectTransform
		{
			get
			{
				if (m_canvasScaler == null)
					m_canvasRectTransform = null;

				if (m_canvasRectTransform == null)
				{
					CanvasScaler canvasScaler = CanvasScaler;
					if (canvasScaler == null)
						return null;

					m_canvasRectTransform = (RectTransform) canvasScaler.transform;
				}

				return m_canvasRectTransform;
			}
			set
			{
				Stop(); 
				m_canvasRectTransform = value;
			}
		}

		public bool ScaleByCanvasScaler
		{
			get => m_scaleByCanvasScaler;
			set { Stop(); m_scaleByCanvasScaler = value; }
		}
		#endregion

		#region Position
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

		public float PosXStart
		{
			get => m_posXStart;
			set { Stop(); m_posXStart = value; }
		}

		public float PosXEnd
		{
			get => m_posXEnd;
			set { Stop(); m_posXEnd = value; }
		}

		public AnimationCurve PosXCurve
		{
			get => m_posXCurve;
			set { Stop(); m_posXCurve = value; }
		}

		public float PosYStart
		{
			get => m_posYStart;
			set { Stop(); m_posYStart = value; }
		}

		public float PosYEnd
		{
			get => m_posYEnd;
			set { Stop(); m_posYEnd = value; }
		}

		public AnimationCurve PosYCurve
		{
			get => m_posYCurve;
			set { Stop(); m_posYCurve = value; }
		}
		#endregion

		#region Rotation
		[SerializeField]
		protected float m_rotZStart;
		[SerializeField]
		protected float m_rotZEnd;
		[SerializeField]
		protected AnimationCurve m_rotZCurve;

		public float RotZStart
		{
			get => m_rotZStart;
			set { Stop(); m_rotZStart = value; }
		}

		public float RotZEnd
		{
			get => m_rotZEnd;
			set { Stop(); m_rotZEnd = value; }
		}

		public AnimationCurve RotZCurve
		{
			get => m_rotZCurve;
			set { Stop(); m_rotZCurve = value; }
		}
		#endregion

		#region Scale

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

		public float ScaleXStart
		{
			get => m_scaleXStart;
			set { Stop(); m_scaleXStart = value; }
		}

		public float ScaleXEnd
		{
			get => m_scaleXEnd;
			set { Stop(); m_scaleXEnd = value; }
		}

		public AnimationCurve ScaleXCurve
		{
			get => m_scaleXCurve;
			set { Stop(); m_scaleXCurve = value; }
		}

		public float ScaleYStart
		{
			get => m_scaleYStart;
			set { Stop(); m_scaleYStart = value; }
		}

		public float ScaleYEnd
		{
			get => m_scaleYEnd;
			set { Stop(); m_scaleYEnd = value; }
		}

		public AnimationCurve ScaleYCurve
		{
			get => m_scaleYCurve;
			set { Stop(); m_scaleYCurve = value; }
		}

		public bool ScaleLocked
		{
			get => m_scaleLocked;
			set { Stop(); m_scaleLocked = value; }
		}
		#endregion

		#region Alpha
		[SerializeField]
		protected AnimationCurve m_alphaCurve;
		[SerializeField]
		[FormerlySerializedAs("m_alphaImage")] 
		protected Graphic m_alphaGraphic;
		[SerializeField]
		protected CanvasGroup m_alphaCanvasGroup;

		public AnimationCurve AlphaCurve
		{
			get => m_alphaCurve;
			set { Stop(); m_alphaCurve = value; }
		}

		public Graphic AlphaGraphic
		{
			get => m_alphaGraphic;
			set { Stop(); m_alphaGraphic = value; }
		}

		public CanvasGroup AlphaCanvasGroup
		{
			get => m_alphaCanvasGroup;
			set { Stop(); m_alphaCanvasGroup = value; }
		}
		#endregion

		#region Skew
		[SerializeField]
		protected UiSkew m_uiSkew;
		[SerializeField]
		protected float m_skewMinHorizontal;
		[SerializeField]
		protected float m_skewMaxHorizontal;
		[SerializeField]
		protected float m_skewMinVertical;
		[SerializeField]
		protected float m_skewMaxVertical;
		[SerializeField]
		protected AnimationCurve m_skewCurveHorizontal;
		[SerializeField]
		protected AnimationCurve m_skewCurveVertical;
		#endregion
		
		// TODO: sound

		// TODO: animator

		// TODO: particles

		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animatePositionX;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animatePositionY;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animatePosition;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animateRotationZ;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animateScaleX;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animateScaleY;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animateScale;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animateAlpha;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_animateSkew;
		#region debug serialize member
		#if DEBUG_SIMPLE_ANIMATION
			[SerializeField]
		#endif
		#endregion
		private bool m_flagsSet;


		public void SetSlideX( RectTransform _rectTransform, bool _in, bool _left)
		{
			Log($"SetSlideX({_rectTransform.gameObject.name}, {_in}, {_left})");
			Stop();
			float width = _rectTransform.rect.width / xRatio;

			if (_in)
			{
				m_posXEnd = 0;
				m_posXStart = _left ? -width : width;
			}
			else
			{
				m_posXStart = 0;
				m_posXEnd = _left ? -width : width;
			}
		}

		public void SetSlideY( RectTransform _rectTransform, bool _in, bool _up)
		{
			Log($"SetSlideY({_rectTransform.gameObject.name}, {_in}, {_up})");
			Stop();
			float height = _rectTransform.rect.height / yRatio;

			if (_in)
			{
				m_posYEnd = 0;
				m_posYStart = _up ? -height : height;
			}
			else
			{
				m_posYStart = 0;
				m_posYEnd = _up ? -height : height;
			}
		}

		private float xRatio
		{
			get
			{
				if (!m_scaleByCanvasScaler || CanvasRectTransform == null)
					return 1;

				return CanvasRectTransform.sizeDelta.x / CanvasScaler.referenceResolution.x;
			}
		}

		private float yRatio
		{
			get
			{
				if (!m_scaleByCanvasScaler || CanvasRectTransform == null)
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
			m_animateSkew = m_support.HasFlags(ESupport.Skew);
			m_flagsSet = true;
			Log( "InitFlags() done: "
				+ $"m_animatePositionX: {m_animatePositionX}, "
				+ $"m_animatePositionY: {m_animatePositionY}, "
				+ $"m_animatePosition: {m_animatePosition}, "
				+ $"m_animateRotationZ: {m_animateRotationZ}, "
				+ $"m_animateScaleX: {m_animateScaleX}, "
				+ $"m_animateScaleY: {m_animateScaleY}, "
				+ $"m_animateScale: {m_animateScale}, "
				+ $"m_animateAlpha: {m_animateAlpha}, "
				+ $"m_animateSkew: {m_animateSkew}, "
				+ $"m_flagsSet: {m_flagsSet}"
			);
		}

		private void ClearFlags()
		{
			Log("ClearFlags()");
			m_support = ESupport.None;
			m_animatePositionX = false;
			m_animatePositionY = false;
			m_animatePosition = false;
			m_animateRotationZ = false;
			m_animateScaleX = false;
			m_animateScaleY = false;
			m_animateScale = false;
			m_animateAlpha = false;
			m_flagsSet = false;
		}

		protected override void Awake()
		{
			base.Awake();
			if (m_canvasScaler == null && m_scaleByCanvasScaler)
				m_canvasScaler = GetComponentInParent<CanvasScaler>();

			InitFlags();
		}

#if false
		public void SetStackAnimationType( EStackAnimationType _stackAnimationType, bool _backwards, AnimationCurve _animationCurve )
		{
			if (_stackAnimationType == EStackAnimationType.None)
				return;

			if (CanvasScaler == null)
			{
				UiLog.LogError("Can not perform stack animation without canvas scaler!");
				return;
			}

			Log($"SetStackAnimationType({_stackAnimationType}, {_backwards}, {_animationCurve} )");

			if (_backwards)
			{
				switch(_stackAnimationType)
				{
					default:
					case EStackAnimationType.None:
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
				case EStackAnimationType.None:
					Debug.Assert(false);
					break;
				case EStackAnimationType.LeftToRight:
					CheckCurve(false, _animationCurve);
					m_support = ESupport.PositionX;
					m_animatePositionX = true;
					m_posXStart = res.x;
					m_posXEnd = 0;
					break;
				case EStackAnimationType.RightToLeft:
					CheckCurve(false, _animationCurve);
					m_support = ESupport.PositionX;
					m_animatePositionX = true;
					m_posXStart = -res.x;
					m_posXEnd = 0;
					break;
				case EStackAnimationType.TopToBottom:
					CheckCurve(true, _animationCurve);
					m_support = ESupport.PositionY;
					m_animatePositionY = true;
					m_posYStart = -res.y;
					m_posYEnd = 0;
					break;
				case EStackAnimationType.BottomToTop:
					CheckCurve(true, _animationCurve);
					m_support = ESupport.PositionY;
					m_animatePositionY = true;
					m_posYStart = res.y;
					m_posYEnd = 0;
					break;
			}
			m_flagsSet = true;
		}
#endif

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
			Log($"OnAnimate({_normalizedTime}) m_support:{m_support} m_flagsSet: {m_flagsSet} m_target.gameObject:{(m_target == null ? "null" : m_target.gameObject.name) }");

			if (!m_flagsSet)
				InitFlags();

			if( m_animatePosition )
				AnimatePosition(_normalizedTime);

			if ( m_animateRotationZ )
				AnimateRotation(_normalizedTime);

			if ( m_animateScale )
				AnimateScale(_normalizedTime);

			if ( m_animateAlpha )
				AnimateAlpha(_normalizedTime);
			
			if (m_animateSkew)
				AnimateSkew(_normalizedTime);

			if (m_markTargetForLayoutRebuild)
				LayoutRebuilder.MarkLayoutForRebuild(m_target);
		}

		private void AnimateSkew(float _normalizedTime)
		{
			bool animateX = m_skewCurveHorizontal != null && m_skewCurveHorizontal.length > 1;
			bool animateY = m_skewCurveVertical != null && m_skewCurveVertical.length > 1;
			var x = animateX ? Mathf.LerpUnclamped(m_skewMinHorizontal, m_skewMaxHorizontal, m_skewCurveHorizontal.Evaluate(_normalizedTime)) : 0;
			var y = animateX ? Mathf.LerpUnclamped(m_skewMinVertical, m_skewMaxVertical, m_skewCurveVertical.Evaluate(_normalizedTime)) : 0;
			
			if (animateX && animateY)
			{
				m_uiSkew.Angles = new Vector2(x, y);
				return;
			}
			
			if (animateX)
			{
				m_uiSkew.AngleHorizontal = x;
				return;
			}
			
			if (animateY)
				m_uiSkew.AngleVertical = x;
		}

		private void AnimatePosition( float _normalizedTime )
		{
			Vector2 pos = m_target.anchoredPosition;

			if (m_animatePositionX)
				pos.x = Mathf.LerpUnclamped(m_posXStart, m_posXEnd, m_posXCurve.Evaluate(_normalizedTime)) * xRatio;

			if (m_animatePositionY)
				pos.y = Mathf.LerpUnclamped(m_posYStart, m_posYEnd, m_posYCurve.Evaluate(_normalizedTime)) * yRatio;

			Log($"AnimatePosition({_normalizedTime}), pos:{pos}");
			m_target.anchoredPosition = pos;
		}

		private void AnimateRotation( float _normalizedTime )
		{
			Quaternion rot = m_target.localRotation;
			Vector3 angles = rot.eulerAngles;
			angles.z = Mathf.LerpUnclamped(m_rotZStart, m_rotZEnd, m_rotZCurve.Evaluate(_normalizedTime));
			Log($"AnimateRotation({_normalizedTime}), angles.z:{angles.z}");
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

			Log($"AnimateScale({_normalizedTime}), scale:{scale}");

			m_target.localScale = scale;
		}

		private void AnimateAlpha( float _normalizedTime )
		{
			float alpha = m_alphaCurve.Evaluate(_normalizedTime);

			Log($"AnimateAlpha({_normalizedTime}), alpha:{alpha}");

			if (m_alphaGraphic != null)
			{
				Color c = m_alphaGraphic.color;
				c.a = alpha;
				m_alphaGraphic.color = c;
			}

			if (m_alphaCanvasGroup != null)
				m_alphaCanvasGroup.alpha = alpha;
		}

	}
}
