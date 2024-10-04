// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<UnityEngine.UI.Image>
	{
		public UiStyleImage(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueSprite : ApplicableValue<UnityEngine.Sprite> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				m_sprite,
				m_color,
			};
		}

		[SerializeReference] private ApplicableValueSprite m_sprite = new();
		[SerializeReference] private ApplicableValueColor m_color = new();

		public ApplicableValue<UnityEngine.Sprite> Sprite => m_sprite;
		public ApplicableValue<UnityEngine.Color> Color => m_color;
	}
}
