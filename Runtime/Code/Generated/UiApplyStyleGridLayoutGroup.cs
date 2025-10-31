// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.GridLayoutGroup))]
	public class UiApplyStyleGridLayoutGroup : UiAbstractApplyStyle<UnityEngine.UI.GridLayoutGroup, UiStyleGridLayoutGroup>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.CellSize.IsApplicable)
				try { SpecificComponent.cellSize = Tweenable ? SpecificStyle.CellSize.Value : SpecificStyle.CellSize.RawValue; } catch {}
			if (SpecificStyle.ChildAlignment.IsApplicable)
				try { SpecificComponent.childAlignment = Tweenable ? SpecificStyle.ChildAlignment.Value : SpecificStyle.ChildAlignment.RawValue; } catch {}
			if (SpecificStyle.Constraint.IsApplicable)
				try { SpecificComponent.constraint = Tweenable ? SpecificStyle.Constraint.Value : SpecificStyle.Constraint.RawValue; } catch {}
			if (SpecificStyle.ConstraintCount.IsApplicable)
				try { SpecificComponent.constraintCount = Tweenable ? SpecificStyle.ConstraintCount.Value : SpecificStyle.ConstraintCount.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Padding.IsApplicable)
				try { SpecificComponent.padding = Tweenable ? SpecificStyle.Padding.Value : SpecificStyle.Padding.RawValue; } catch {}
			if (SpecificStyle.Spacing.IsApplicable)
				try { SpecificComponent.spacing = Tweenable ? SpecificStyle.Spacing.Value : SpecificStyle.Spacing.RawValue; } catch {}
			if (SpecificStyle.StartAxis.IsApplicable)
				try { SpecificComponent.startAxis = Tweenable ? SpecificStyle.StartAxis.Value : SpecificStyle.StartAxis.RawValue; } catch {}
			if (SpecificStyle.StartCorner.IsApplicable)
				try { SpecificComponent.startCorner = Tweenable ? SpecificStyle.StartCorner.Value : SpecificStyle.StartCorner.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.CellSize.IsApplicable)
				try { SpecificStyle.CellSize.RawValue = SpecificComponent.cellSize; } catch {}
			if (SpecificStyle.ChildAlignment.IsApplicable)
				try { SpecificStyle.ChildAlignment.RawValue = SpecificComponent.childAlignment; } catch {}
			if (SpecificStyle.Constraint.IsApplicable)
				try { SpecificStyle.Constraint.RawValue = SpecificComponent.constraint; } catch {}
			if (SpecificStyle.ConstraintCount.IsApplicable)
				try { SpecificStyle.ConstraintCount.RawValue = SpecificComponent.constraintCount; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Padding.IsApplicable)
				try { SpecificStyle.Padding.RawValue = SpecificComponent.padding; } catch {}
			if (SpecificStyle.Spacing.IsApplicable)
				try { SpecificStyle.Spacing.RawValue = SpecificComponent.spacing; } catch {}
			if (SpecificStyle.StartAxis.IsApplicable)
				try { SpecificStyle.StartAxis.RawValue = SpecificComponent.startAxis; } catch {}
			if (SpecificStyle.StartCorner.IsApplicable)
				try { SpecificStyle.StartCorner.RawValue = SpecificComponent.startCorner; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleGridLayoutGroup result = new UiStyleGridLayoutGroup(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleGridLayoutGroup) _template;

				result.CellSize.Value = specificTemplate.CellSize.Value;
				result.CellSize.IsApplicable = specificTemplate.CellSize.IsApplicable;
				result.ChildAlignment.Value = specificTemplate.ChildAlignment.Value;
				result.ChildAlignment.IsApplicable = specificTemplate.ChildAlignment.IsApplicable;
				result.Constraint.Value = specificTemplate.Constraint.Value;
				result.Constraint.IsApplicable = specificTemplate.Constraint.IsApplicable;
				result.ConstraintCount.Value = specificTemplate.ConstraintCount.Value;
				result.ConstraintCount.IsApplicable = specificTemplate.ConstraintCount.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Padding.Value = specificTemplate.Padding.Value;
				result.Padding.IsApplicable = specificTemplate.Padding.IsApplicable;
				result.Spacing.Value = specificTemplate.Spacing.Value;
				result.Spacing.IsApplicable = specificTemplate.Spacing.IsApplicable;
				result.StartAxis.Value = specificTemplate.StartAxis.Value;
				result.StartAxis.IsApplicable = specificTemplate.StartAxis.IsApplicable;
				result.StartCorner.Value = specificTemplate.StartCorner.Value;
				result.StartCorner.IsApplicable = specificTemplate.StartCorner.IsApplicable;

				return result;
			}

			try { result.CellSize.Value = SpecificComponent.cellSize; } catch {}
			try { result.ChildAlignment.Value = SpecificComponent.childAlignment; } catch {}
			try { result.Constraint.Value = SpecificComponent.constraint; } catch {}
			try { result.ConstraintCount.Value = SpecificComponent.constraintCount; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Padding.Value = SpecificComponent.padding; } catch {}
			try { result.Spacing.Value = SpecificComponent.spacing; } catch {}
			try { result.StartAxis.Value = SpecificComponent.startAxis; } catch {}
			try { result.StartCorner.Value = SpecificComponent.startCorner; } catch {}

			return result;
		}
	}
}
