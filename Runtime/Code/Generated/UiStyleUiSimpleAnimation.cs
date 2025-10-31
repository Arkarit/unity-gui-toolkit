// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiSimpleAnimation : UiAbstractStyle<GuiToolkit.UiSimpleAnimation>
	{
		public UiStyleUiSimpleAnimation(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueCanvasGroup : ApplicableValue<UnityEngine.CanvasGroup> {}
		private class ApplicableValueAnimationCurve : ApplicableValue<UnityEngine.AnimationCurve> {}
		private class ApplicableValueGraphic : ApplicableValue<UnityEngine.UI.Graphic> {}
		private class ApplicableValueUiSimpleAnimationBase : ApplicableValue<GuiToolkit.UiSimpleAnimationBase> {}
		private class ApplicableValueRectTransform : ApplicableValue<UnityEngine.RectTransform> {}
		private class ApplicableValueCanvasScaler : ApplicableValue<UnityEngine.UI.CanvasScaler> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueESupport : ApplicableValue<GuiToolkit.UiSimpleAnimation.ESupport> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				AlphaCanvasGroup,
				AlphaCurve,
				AlphaGraphic,
				BackwardsAnimation,
				CanvasRectTransform,
				CanvasScaler,
				Delay,
				Duration,
				Enabled,
				EnabledInHierarchy,
				MarkTargetForLayoutRebuild,
				PosXCurve,
				PosXEnd,
				PosXStart,
				PosYCurve,
				PosYEnd,
				PosYStart,
				RotZCurve,
				RotZEnd,
				RotZStart,
				ScaleByCanvasScaler,
				ScaleLocked,
				ScaleXCurve,
				ScaleXEnd,
				ScaleXStart,
				ScaleYCurve,
				ScaleYEnd,
				ScaleYStart,
				Support,
				Target,
			};
		}

		[SerializeReference] private ApplicableValueCanvasGroup m_AlphaCanvasGroup = new();
		[SerializeReference] private ApplicableValueAnimationCurve m_AlphaCurve = new();
		[SerializeReference] private ApplicableValueGraphic m_AlphaGraphic = new();
		[SerializeReference] private ApplicableValueUiSimpleAnimationBase m_BackwardsAnimation = new();
		[SerializeReference] private ApplicableValueRectTransform m_CanvasRectTransform = new();
		[SerializeReference] private ApplicableValueCanvasScaler m_CanvasScaler = new();
		[SerializeReference] private ApplicableValueSingle m_Delay = new();
		[SerializeReference] private ApplicableValueSingle m_Duration = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_EnabledInHierarchy = new();
		[SerializeReference] private ApplicableValueBoolean m_MarkTargetForLayoutRebuild = new();
		[SerializeReference] private ApplicableValueAnimationCurve m_PosXCurve = new();
		[SerializeReference] private ApplicableValueSingle m_PosXEnd = new();
		[SerializeReference] private ApplicableValueSingle m_PosXStart = new();
		[SerializeReference] private ApplicableValueAnimationCurve m_PosYCurve = new();
		[SerializeReference] private ApplicableValueSingle m_PosYEnd = new();
		[SerializeReference] private ApplicableValueSingle m_PosYStart = new();
		[SerializeReference] private ApplicableValueAnimationCurve m_RotZCurve = new();
		[SerializeReference] private ApplicableValueSingle m_RotZEnd = new();
		[SerializeReference] private ApplicableValueSingle m_RotZStart = new();
		[SerializeReference] private ApplicableValueBoolean m_ScaleByCanvasScaler = new();
		[SerializeReference] private ApplicableValueBoolean m_ScaleLocked = new();
		[SerializeReference] private ApplicableValueAnimationCurve m_ScaleXCurve = new();
		[SerializeReference] private ApplicableValueSingle m_ScaleXEnd = new();
		[SerializeReference] private ApplicableValueSingle m_ScaleXStart = new();
		[SerializeReference] private ApplicableValueAnimationCurve m_ScaleYCurve = new();
		[SerializeReference] private ApplicableValueSingle m_ScaleYEnd = new();
		[SerializeReference] private ApplicableValueSingle m_ScaleYStart = new();
		[SerializeReference] private ApplicableValueESupport m_Support = new();
		[SerializeReference] private ApplicableValueRectTransform m_Target = new();

		public ApplicableValue<UnityEngine.CanvasGroup> AlphaCanvasGroup
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_AlphaCanvasGroup == null)
						m_AlphaCanvasGroup = new ApplicableValueCanvasGroup();
				#endif
				return m_AlphaCanvasGroup;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> AlphaCurve
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_AlphaCurve == null)
						m_AlphaCurve = new ApplicableValueAnimationCurve();
				#endif
				return m_AlphaCurve;
			}
		}

		public ApplicableValue<UnityEngine.UI.Graphic> AlphaGraphic
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_AlphaGraphic == null)
						m_AlphaGraphic = new ApplicableValueGraphic();
				#endif
				return m_AlphaGraphic;
			}
		}

		public ApplicableValue<GuiToolkit.UiSimpleAnimationBase> BackwardsAnimation
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_BackwardsAnimation == null)
						m_BackwardsAnimation = new ApplicableValueUiSimpleAnimationBase();
				#endif
				return m_BackwardsAnimation;
			}
		}

		public ApplicableValue<UnityEngine.RectTransform> CanvasRectTransform
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_CanvasRectTransform == null)
						m_CanvasRectTransform = new ApplicableValueRectTransform();
				#endif
				return m_CanvasRectTransform;
			}
		}

		public ApplicableValue<UnityEngine.UI.CanvasScaler> CanvasScaler
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_CanvasScaler == null)
						m_CanvasScaler = new ApplicableValueCanvasScaler();
				#endif
				return m_CanvasScaler;
			}
		}

		public ApplicableValue<System.Single> Delay
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Delay == null)
						m_Delay = new ApplicableValueSingle();
				#endif
				return m_Delay;
			}
		}

		public ApplicableValue<System.Single> Duration
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Duration == null)
						m_Duration = new ApplicableValueSingle();
				#endif
				return m_Duration;
			}
		}

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_enabled == null)
						m_enabled = new ApplicableValueBoolean();
				#endif
				return m_enabled;
			}
		}

		public ApplicableValue<System.Boolean> EnabledInHierarchy
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_EnabledInHierarchy == null)
						m_EnabledInHierarchy = new ApplicableValueBoolean();
				#endif
				return m_EnabledInHierarchy;
			}
		}

		public ApplicableValue<System.Boolean> MarkTargetForLayoutRebuild
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_MarkTargetForLayoutRebuild == null)
						m_MarkTargetForLayoutRebuild = new ApplicableValueBoolean();
				#endif
				return m_MarkTargetForLayoutRebuild;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> PosXCurve
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PosXCurve == null)
						m_PosXCurve = new ApplicableValueAnimationCurve();
				#endif
				return m_PosXCurve;
			}
		}

		public ApplicableValue<System.Single> PosXEnd
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PosXEnd == null)
						m_PosXEnd = new ApplicableValueSingle();
				#endif
				return m_PosXEnd;
			}
		}

		public ApplicableValue<System.Single> PosXStart
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PosXStart == null)
						m_PosXStart = new ApplicableValueSingle();
				#endif
				return m_PosXStart;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> PosYCurve
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PosYCurve == null)
						m_PosYCurve = new ApplicableValueAnimationCurve();
				#endif
				return m_PosYCurve;
			}
		}

		public ApplicableValue<System.Single> PosYEnd
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PosYEnd == null)
						m_PosYEnd = new ApplicableValueSingle();
				#endif
				return m_PosYEnd;
			}
		}

		public ApplicableValue<System.Single> PosYStart
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PosYStart == null)
						m_PosYStart = new ApplicableValueSingle();
				#endif
				return m_PosYStart;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> RotZCurve
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_RotZCurve == null)
						m_RotZCurve = new ApplicableValueAnimationCurve();
				#endif
				return m_RotZCurve;
			}
		}

		public ApplicableValue<System.Single> RotZEnd
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_RotZEnd == null)
						m_RotZEnd = new ApplicableValueSingle();
				#endif
				return m_RotZEnd;
			}
		}

		public ApplicableValue<System.Single> RotZStart
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_RotZStart == null)
						m_RotZStart = new ApplicableValueSingle();
				#endif
				return m_RotZStart;
			}
		}

		public ApplicableValue<System.Boolean> ScaleByCanvasScaler
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleByCanvasScaler == null)
						m_ScaleByCanvasScaler = new ApplicableValueBoolean();
				#endif
				return m_ScaleByCanvasScaler;
			}
		}

		public ApplicableValue<System.Boolean> ScaleLocked
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleLocked == null)
						m_ScaleLocked = new ApplicableValueBoolean();
				#endif
				return m_ScaleLocked;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> ScaleXCurve
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleXCurve == null)
						m_ScaleXCurve = new ApplicableValueAnimationCurve();
				#endif
				return m_ScaleXCurve;
			}
		}

		public ApplicableValue<System.Single> ScaleXEnd
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleXEnd == null)
						m_ScaleXEnd = new ApplicableValueSingle();
				#endif
				return m_ScaleXEnd;
			}
		}

		public ApplicableValue<System.Single> ScaleXStart
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleXStart == null)
						m_ScaleXStart = new ApplicableValueSingle();
				#endif
				return m_ScaleXStart;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> ScaleYCurve
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleYCurve == null)
						m_ScaleYCurve = new ApplicableValueAnimationCurve();
				#endif
				return m_ScaleYCurve;
			}
		}

		public ApplicableValue<System.Single> ScaleYEnd
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleYEnd == null)
						m_ScaleYEnd = new ApplicableValueSingle();
				#endif
				return m_ScaleYEnd;
			}
		}

		public ApplicableValue<System.Single> ScaleYStart
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ScaleYStart == null)
						m_ScaleYStart = new ApplicableValueSingle();
				#endif
				return m_ScaleYStart;
			}
		}

		public ApplicableValue<GuiToolkit.UiSimpleAnimation.ESupport> Support
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Support == null)
						m_Support = new ApplicableValueESupport();
				#endif
				return m_Support;
			}
		}

		public ApplicableValue<UnityEngine.RectTransform> Target
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Target == null)
						m_Target = new ApplicableValueRectTransform();
				#endif
				return m_Target;
			}
		}

	}
}
