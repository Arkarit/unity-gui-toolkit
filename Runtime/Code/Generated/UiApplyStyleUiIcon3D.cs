// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiIcon3D))]
	public class UiApplyStyleUiIcon3D : UiAbstractApplyStyle<GuiToolkit.UiIcon3D, UiStyleUiIcon3D>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AnimationSpeed.IsApplicable)
				try { SpecificComponent.AnimationSpeed = Tweenable ? SpecificStyle.AnimationSpeed.Value : SpecificStyle.AnimationSpeed.RawValue; } catch {}
			if (SpecificStyle.AnimationStartTime.IsApplicable)
				try { SpecificComponent.AnimationStartTime = Tweenable ? SpecificStyle.AnimationStartTime.Value : SpecificStyle.AnimationStartTime.RawValue; } catch {}
			if (SpecificStyle.Bounds.IsApplicable)
				try { SpecificComponent.Bounds = Tweenable ? SpecificStyle.Bounds.Value : SpecificStyle.Bounds.RawValue; } catch {}
			if (SpecificStyle.CanvasInParent.IsApplicable)
				try { SpecificComponent.CanvasInParent = Tweenable ? SpecificStyle.CanvasInParent.Value : SpecificStyle.CanvasInParent.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.EnabledInHierarchy.IsApplicable)
				try { SpecificComponent.EnabledInHierarchy = Tweenable ? SpecificStyle.EnabledInHierarchy.Value : SpecificStyle.EnabledInHierarchy.RawValue; } catch {}
			if (SpecificStyle.FrameDivider.IsApplicable)
				try { SpecificComponent.FrameDivider = Tweenable ? SpecificStyle.FrameDivider.Value : SpecificStyle.FrameDivider.RawValue; } catch {}
			if (SpecificStyle.LoadingTexture.IsApplicable)
				try { SpecificComponent.LoadingTexture = Tweenable ? SpecificStyle.LoadingTexture.Value : SpecificStyle.LoadingTexture.RawValue; } catch {}
			if (SpecificStyle.Mode.IsApplicable)
				try { SpecificComponent.Mode = Tweenable ? SpecificStyle.Mode.Value : SpecificStyle.Mode.RawValue; } catch {}
			if (SpecificStyle.OverrideBounds.IsApplicable)
				try { SpecificComponent.OverrideBounds = Tweenable ? SpecificStyle.OverrideBounds.Value : SpecificStyle.OverrideBounds.RawValue; } catch {}
			if (SpecificStyle.OverrideViewRotation.IsApplicable)
				try { SpecificComponent.OverrideViewRotation = Tweenable ? SpecificStyle.OverrideViewRotation.Value : SpecificStyle.OverrideViewRotation.RawValue; } catch {}
			if (SpecificStyle.Paused.IsApplicable)
				try { SpecificComponent.Paused = Tweenable ? SpecificStyle.Paused.Value : SpecificStyle.Paused.RawValue; } catch {}
			if (SpecificStyle.Prefab.IsApplicable)
				try { SpecificComponent.Prefab = Tweenable ? SpecificStyle.Prefab.Value : SpecificStyle.Prefab.RawValue; } catch {}
			if (SpecificStyle.PrefabId.IsApplicable)
				try { SpecificComponent.PrefabId = Tweenable ? SpecificStyle.PrefabId.Value : SpecificStyle.PrefabId.RawValue; } catch {}
			if (SpecificStyle.Preset.IsApplicable)
				try { SpecificComponent.Preset = Tweenable ? SpecificStyle.Preset.Value : SpecificStyle.Preset.RawValue; } catch {}
			if (SpecificStyle.RefreshInterval.IsApplicable)
				try { SpecificComponent.RefreshInterval = Tweenable ? SpecificStyle.RefreshInterval.Value : SpecificStyle.RefreshInterval.RawValue; } catch {}
			if (SpecificStyle.ResolutionScale.IsApplicable)
				try { SpecificComponent.ResolutionScale = Tweenable ? SpecificStyle.ResolutionScale.Value : SpecificStyle.ResolutionScale.RawValue; } catch {}
			if (SpecificStyle.RewindSpeed.IsApplicable)
				try { SpecificComponent.RewindSpeed = Tweenable ? SpecificStyle.RewindSpeed.Value : SpecificStyle.RewindSpeed.RawValue; } catch {}
			if (SpecificStyle.ViewRotation.IsApplicable)
				try { SpecificComponent.ViewRotation = Tweenable ? SpecificStyle.ViewRotation.Value : SpecificStyle.ViewRotation.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AnimationSpeed.IsApplicable)
				try { SpecificStyle.AnimationSpeed.RawValue = SpecificComponent.AnimationSpeed; } catch {}
			if (SpecificStyle.AnimationStartTime.IsApplicable)
				try { SpecificStyle.AnimationStartTime.RawValue = SpecificComponent.AnimationStartTime; } catch {}
			if (SpecificStyle.Bounds.IsApplicable)
				try { SpecificStyle.Bounds.RawValue = SpecificComponent.Bounds; } catch {}
			if (SpecificStyle.CanvasInParent.IsApplicable)
				try { SpecificStyle.CanvasInParent.RawValue = SpecificComponent.CanvasInParent; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.EnabledInHierarchy.IsApplicable)
				try { SpecificStyle.EnabledInHierarchy.RawValue = SpecificComponent.EnabledInHierarchy; } catch {}
			if (SpecificStyle.FrameDivider.IsApplicable)
				try { SpecificStyle.FrameDivider.RawValue = SpecificComponent.FrameDivider; } catch {}
			if (SpecificStyle.LoadingTexture.IsApplicable)
				try { SpecificStyle.LoadingTexture.RawValue = SpecificComponent.LoadingTexture; } catch {}
			if (SpecificStyle.Mode.IsApplicable)
				try { SpecificStyle.Mode.RawValue = SpecificComponent.Mode; } catch {}
			if (SpecificStyle.OverrideBounds.IsApplicable)
				try { SpecificStyle.OverrideBounds.RawValue = SpecificComponent.OverrideBounds; } catch {}
			if (SpecificStyle.OverrideViewRotation.IsApplicable)
				try { SpecificStyle.OverrideViewRotation.RawValue = SpecificComponent.OverrideViewRotation; } catch {}
			if (SpecificStyle.Paused.IsApplicable)
				try { SpecificStyle.Paused.RawValue = SpecificComponent.Paused; } catch {}
			if (SpecificStyle.Prefab.IsApplicable)
				try { SpecificStyle.Prefab.RawValue = SpecificComponent.Prefab; } catch {}
			if (SpecificStyle.PrefabId.IsApplicable)
				try { SpecificStyle.PrefabId.RawValue = SpecificComponent.PrefabId; } catch {}
			if (SpecificStyle.Preset.IsApplicable)
				try { SpecificStyle.Preset.RawValue = SpecificComponent.Preset; } catch {}
			if (SpecificStyle.RefreshInterval.IsApplicable)
				try { SpecificStyle.RefreshInterval.RawValue = SpecificComponent.RefreshInterval; } catch {}
			if (SpecificStyle.ResolutionScale.IsApplicable)
				try { SpecificStyle.ResolutionScale.RawValue = SpecificComponent.ResolutionScale; } catch {}
			if (SpecificStyle.RewindSpeed.IsApplicable)
				try { SpecificStyle.RewindSpeed.RawValue = SpecificComponent.RewindSpeed; } catch {}
			if (SpecificStyle.ViewRotation.IsApplicable)
				try { SpecificStyle.ViewRotation.RawValue = SpecificComponent.ViewRotation; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiIcon3D result = new UiStyleUiIcon3D(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiIcon3D) _template;

				result.AnimationSpeed.Value = specificTemplate.AnimationSpeed.Value;
				result.AnimationSpeed.IsApplicable = specificTemplate.AnimationSpeed.IsApplicable;
				result.AnimationStartTime.Value = specificTemplate.AnimationStartTime.Value;
				result.AnimationStartTime.IsApplicable = specificTemplate.AnimationStartTime.IsApplicable;
				result.Bounds.Value = specificTemplate.Bounds.Value;
				result.Bounds.IsApplicable = specificTemplate.Bounds.IsApplicable;
				result.CanvasInParent.Value = specificTemplate.CanvasInParent.Value;
				result.CanvasInParent.IsApplicable = specificTemplate.CanvasInParent.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.EnabledInHierarchy.Value = specificTemplate.EnabledInHierarchy.Value;
				result.EnabledInHierarchy.IsApplicable = specificTemplate.EnabledInHierarchy.IsApplicable;
				result.FrameDivider.Value = specificTemplate.FrameDivider.Value;
				result.FrameDivider.IsApplicable = specificTemplate.FrameDivider.IsApplicable;
				result.LoadingTexture.Value = specificTemplate.LoadingTexture.Value;
				result.LoadingTexture.IsApplicable = specificTemplate.LoadingTexture.IsApplicable;
				result.Mode.Value = specificTemplate.Mode.Value;
				result.Mode.IsApplicable = specificTemplate.Mode.IsApplicable;
				result.OverrideBounds.Value = specificTemplate.OverrideBounds.Value;
				result.OverrideBounds.IsApplicable = specificTemplate.OverrideBounds.IsApplicable;
				result.OverrideViewRotation.Value = specificTemplate.OverrideViewRotation.Value;
				result.OverrideViewRotation.IsApplicable = specificTemplate.OverrideViewRotation.IsApplicable;
				result.Paused.Value = specificTemplate.Paused.Value;
				result.Paused.IsApplicable = specificTemplate.Paused.IsApplicable;
				result.Prefab.Value = specificTemplate.Prefab.Value;
				result.Prefab.IsApplicable = specificTemplate.Prefab.IsApplicable;
				result.PrefabId.Value = specificTemplate.PrefabId.Value;
				result.PrefabId.IsApplicable = specificTemplate.PrefabId.IsApplicable;
				result.Preset.Value = specificTemplate.Preset.Value;
				result.Preset.IsApplicable = specificTemplate.Preset.IsApplicable;
				result.RefreshInterval.Value = specificTemplate.RefreshInterval.Value;
				result.RefreshInterval.IsApplicable = specificTemplate.RefreshInterval.IsApplicable;
				result.ResolutionScale.Value = specificTemplate.ResolutionScale.Value;
				result.ResolutionScale.IsApplicable = specificTemplate.ResolutionScale.IsApplicable;
				result.RewindSpeed.Value = specificTemplate.RewindSpeed.Value;
				result.RewindSpeed.IsApplicable = specificTemplate.RewindSpeed.IsApplicable;
				result.ViewRotation.Value = specificTemplate.ViewRotation.Value;
				result.ViewRotation.IsApplicable = specificTemplate.ViewRotation.IsApplicable;

				return result;
			}

			try { result.AnimationSpeed.Value = SpecificComponent.AnimationSpeed; } catch {}
			try { result.AnimationStartTime.Value = SpecificComponent.AnimationStartTime; } catch {}
			try { result.Bounds.Value = SpecificComponent.Bounds; } catch {}
			try { result.CanvasInParent.Value = SpecificComponent.CanvasInParent; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.EnabledInHierarchy.Value = SpecificComponent.EnabledInHierarchy; } catch {}
			try { result.FrameDivider.Value = SpecificComponent.FrameDivider; } catch {}
			try { result.LoadingTexture.Value = SpecificComponent.LoadingTexture; } catch {}
			try { result.Mode.Value = SpecificComponent.Mode; } catch {}
			try { result.OverrideBounds.Value = SpecificComponent.OverrideBounds; } catch {}
			try { result.OverrideViewRotation.Value = SpecificComponent.OverrideViewRotation; } catch {}
			try { result.Paused.Value = SpecificComponent.Paused; } catch {}
			try { result.Prefab.Value = SpecificComponent.Prefab; } catch {}
			try { result.PrefabId.Value = SpecificComponent.PrefabId; } catch {}
			try { result.Preset.Value = SpecificComponent.Preset; } catch {}
			try { result.RefreshInterval.Value = SpecificComponent.RefreshInterval; } catch {}
			try { result.ResolutionScale.Value = SpecificComponent.ResolutionScale; } catch {}
			try { result.RewindSpeed.Value = SpecificComponent.RewindSpeed; } catch {}
			try { result.ViewRotation.Value = SpecificComponent.ViewRotation; } catch {}

			return result;
		}
	}
}
