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
				try { SpecificMonoBehaviour.Gradient = SpecificStyle.Gradient.m_value; } catch {}
			if (SpecificStyle.Axis.IsApplicable)
				try { SpecificMonoBehaviour.Axis = SpecificStyle.Axis.m_value; } catch {}
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

				result.Gradient.m_value = specificTemplate.Gradient.m_value;
				result.Gradient.IsApplicable = specificTemplate.Gradient.IsApplicable;
				result.Axis.m_value = specificTemplate.Axis.m_value;
				result.Axis.IsApplicable = specificTemplate.Axis.IsApplicable;

				return result;
			}

			try { result.Gradient.m_value = SpecificMonoBehaviour.Gradient; } catch {}
			try { result.Axis.m_value = SpecificMonoBehaviour.Axis; } catch {}

			return result;
		}
	}
}
