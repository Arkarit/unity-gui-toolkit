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
				if (m_AlphaCanvasGroup == null)
					m_AlphaCanvasGroup = new ApplicableValueCanvasGroup();
				return m_AlphaCanvasGroup;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> AlphaCurve
		{
			get
			{
				if (m_AlphaCurve == null)
					m_AlphaCurve = new ApplicableValueAnimationCurve();
				return m_AlphaCurve;
			}
		}

		public ApplicableValue<UnityEngine.UI.Graphic> AlphaGraphic
		{
			get
			{
				if (m_AlphaGraphic == null)
					m_AlphaGraphic = new ApplicableValueGraphic();
				return m_AlphaGraphic;
			}
		}

		public ApplicableValue<GuiToolkit.UiSimpleAnimationBase> BackwardsAnimation
		{
			get
			{
				if (m_BackwardsAnimation == null)
					m_BackwardsAnimation = new ApplicableValueUiSimpleAnimationBase();
				return m_BackwardsAnimation;
			}
		}

		public ApplicableValue<UnityEngine.RectTransform> CanvasRectTransform
		{
			get
			{
				if (m_CanvasRectTransform == null)
					m_CanvasRectTransform = new ApplicableValueRectTransform();
				return m_CanvasRectTransform;
			}
		}

		public ApplicableValue<UnityEngine.UI.CanvasScaler> CanvasScaler
		{
			get
			{
				if (m_CanvasScaler == null)
					m_CanvasScaler = new ApplicableValueCanvasScaler();
				return m_CanvasScaler;
			}
		}

		public ApplicableValue<System.Single> Delay
		{
			get
			{
				if (m_Delay == null)
					m_Delay = new ApplicableValueSingle();
				return m_Delay;
			}
		}

		public ApplicableValue<System.Single> Duration
		{
			get
			{
				if (m_Duration == null)
					m_Duration = new ApplicableValueSingle();
				return m_Duration;
			}
		}

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

		public ApplicableValue<System.Boolean> EnabledInHierarchy
		{
			get
			{
				if (m_EnabledInHierarchy == null)
					m_EnabledInHierarchy = new ApplicableValueBoolean();
				return m_EnabledInHierarchy;
			}
		}

		public ApplicableValue<System.Boolean> MarkTargetForLayoutRebuild
		{
			get
			{
				if (m_MarkTargetForLayoutRebuild == null)
					m_MarkTargetForLayoutRebuild = new ApplicableValueBoolean();
				return m_MarkTargetForLayoutRebuild;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> PosXCurve
		{
			get
			{
				if (m_PosXCurve == null)
					m_PosXCurve = new ApplicableValueAnimationCurve();
				return m_PosXCurve;
			}
		}

		public ApplicableValue<System.Single> PosXEnd
		{
			get
			{
				if (m_PosXEnd == null)
					m_PosXEnd = new ApplicableValueSingle();
				return m_PosXEnd;
			}
		}

		public ApplicableValue<System.Single> PosXStart
		{
			get
			{
				if (m_PosXStart == null)
					m_PosXStart = new ApplicableValueSingle();
				return m_PosXStart;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> PosYCurve
		{
			get
			{
				if (m_PosYCurve == null)
					m_PosYCurve = new ApplicableValueAnimationCurve();
				return m_PosYCurve;
			}
		}

		public ApplicableValue<System.Single> PosYEnd
		{
			get
			{
				if (m_PosYEnd == null)
					m_PosYEnd = new ApplicableValueSingle();
				return m_PosYEnd;
			}
		}

		public ApplicableValue<System.Single> PosYStart
		{
			get
			{
				if (m_PosYStart == null)
					m_PosYStart = new ApplicableValueSingle();
				return m_PosYStart;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> RotZCurve
		{
			get
			{
				if (m_RotZCurve == null)
					m_RotZCurve = new ApplicableValueAnimationCurve();
				return m_RotZCurve;
			}
		}

		public ApplicableValue<System.Single> RotZEnd
		{
			get
			{
				if (m_RotZEnd == null)
					m_RotZEnd = new ApplicableValueSingle();
				return m_RotZEnd;
			}
		}

		public ApplicableValue<System.Single> RotZStart
		{
			get
			{
				if (m_RotZStart == null)
					m_RotZStart = new ApplicableValueSingle();
				return m_RotZStart;
			}
		}

		public ApplicableValue<System.Boolean> ScaleByCanvasScaler
		{
			get
			{
				if (m_ScaleByCanvasScaler == null)
					m_ScaleByCanvasScaler = new ApplicableValueBoolean();
				return m_ScaleByCanvasScaler;
			}
		}

		public ApplicableValue<System.Boolean> ScaleLocked
		{
			get
			{
				if (m_ScaleLocked == null)
					m_ScaleLocked = new ApplicableValueBoolean();
				return m_ScaleLocked;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> ScaleXCurve
		{
			get
			{
				if (m_ScaleXCurve == null)
					m_ScaleXCurve = new ApplicableValueAnimationCurve();
				return m_ScaleXCurve;
			}
		}

		public ApplicableValue<System.Single> ScaleXEnd
		{
			get
			{
				if (m_ScaleXEnd == null)
					m_ScaleXEnd = new ApplicableValueSingle();
				return m_ScaleXEnd;
			}
		}

		public ApplicableValue<System.Single> ScaleXStart
		{
			get
			{
				if (m_ScaleXStart == null)
					m_ScaleXStart = new ApplicableValueSingle();
				return m_ScaleXStart;
			}
		}

		public ApplicableValue<UnityEngine.AnimationCurve> ScaleYCurve
		{
			get
			{
				if (m_ScaleYCurve == null)
					m_ScaleYCurve = new ApplicableValueAnimationCurve();
				return m_ScaleYCurve;
			}
		}

		public ApplicableValue<System.Single> ScaleYEnd
		{
			get
			{
				if (m_ScaleYEnd == null)
					m_ScaleYEnd = new ApplicableValueSingle();
				return m_ScaleYEnd;
			}
		}

		public ApplicableValue<System.Single> ScaleYStart
		{
			get
			{
				if (m_ScaleYStart == null)
					m_ScaleYStart = new ApplicableValueSingle();
				return m_ScaleYStart;
			}
		}

		public ApplicableValue<GuiToolkit.UiSimpleAnimation.ESupport> Support
		{
			get
			{
				if (m_Support == null)
					m_Support = new ApplicableValueESupport();
				return m_Support;
			}
		}

		public ApplicableValue<UnityEngine.RectTransform> Target
		{
			get
			{
				if (m_Target == null)
					m_Target = new ApplicableValueRectTransform();
				return m_Target;
			}
		}

	}
}
