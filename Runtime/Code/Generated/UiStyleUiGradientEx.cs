// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiGradientEx : UiAbstractStyle<GuiToolkit.UiGradientEx>
	{
		public UiStyleUiGradientEx(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueGradient : ApplicableValue<UnityEngine.Gradient> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Gradient,
			};
		}

		[SerializeReference] private ApplicableValueGradient m_Gradient = new();

		public ApplicableValue<UnityEngine.Gradient> Gradient
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Gradient == null)
						m_Gradient = new ApplicableValueGradient();
				#endif
				return m_Gradient;
			}
		}

	}
}
