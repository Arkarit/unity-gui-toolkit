// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiGradient))]
	public class UiApplyStyleUiGradient : UiAbstractApplyStyle<GuiToolkit.UiGradient, UiStyleUiGradient>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificMonoBehaviour || SpecificStyle == null)
				return;

			if (SpecificStyle.Gradient.IsApplicable)
				try { SpecificMonoBehaviour.Gradient = SpecificStyle.Gradient.Value; } catch {}
			if (SpecificStyle.Axis.IsApplicable)
				try { SpecificMonoBehaviour.Axis = SpecificStyle.Axis.Value; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiGradient result = new UiStyleUiGradient();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiGradient) _template;

				result.Gradient.Value = specificTemplate.Gradient.Value;
				result.Gradient.IsApplicable = specificTemplate.Gradient.IsApplicable;
				result.Axis.Value = specificTemplate.Axis.Value;
				result.Axis.IsApplicable = specificTemplate.Axis.IsApplicable;

				return result;
			}

			try { result.Gradient.Value = SpecificMonoBehaviour.Gradient; } catch {}
			try { result.Axis.Value = SpecificMonoBehaviour.Axis; } catch {}

			return result;
		}
	}
}
