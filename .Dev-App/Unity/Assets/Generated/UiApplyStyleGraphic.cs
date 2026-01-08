// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.Graphic))]
	public class UiApplyStyleGraphic : UiAbstractApplyStyle<UnityEngine.UI.Graphic, UiStyleGraphic>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificComponent.material = Tweenable ? SpecificStyle.Material.Value : SpecificStyle.Material.RawValue; } catch {}
			if (SpecificStyle.RaycastPadding.IsApplicable)
				try { SpecificComponent.raycastPadding = Tweenable ? SpecificStyle.RaycastPadding.Value : SpecificStyle.RaycastPadding.RawValue; } catch {}
			if (SpecificStyle.RaycastTarget.IsApplicable)
				try { SpecificComponent.raycastTarget = Tweenable ? SpecificStyle.RaycastTarget.Value : SpecificStyle.RaycastTarget.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificStyle.Material.RawValue = SpecificComponent.material; } catch {}
			if (SpecificStyle.RaycastPadding.IsApplicable)
				try { SpecificStyle.RaycastPadding.RawValue = SpecificComponent.raycastPadding; } catch {}
			if (SpecificStyle.RaycastTarget.IsApplicable)
				try { SpecificStyle.RaycastTarget.RawValue = SpecificComponent.raycastTarget; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleGraphic result = new UiStyleGraphic(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleGraphic) _template;

				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Material.Value = specificTemplate.Material.Value;
				result.Material.IsApplicable = specificTemplate.Material.IsApplicable;
				result.RaycastPadding.Value = specificTemplate.RaycastPadding.Value;
				result.RaycastPadding.IsApplicable = specificTemplate.RaycastPadding.IsApplicable;
				result.RaycastTarget.Value = specificTemplate.RaycastTarget.Value;
				result.RaycastTarget.IsApplicable = specificTemplate.RaycastTarget.IsApplicable;

				return result;
			}

			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Material.Value = SpecificComponent.material; } catch {}
			try { result.RaycastPadding.Value = SpecificComponent.raycastPadding; } catch {}
			try { result.RaycastTarget.Value = SpecificComponent.raycastTarget; } catch {}

			return result;
		}
	}
}
