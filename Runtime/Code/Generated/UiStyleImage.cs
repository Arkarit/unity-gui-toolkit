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
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Sprite,
				Color,
				Enabled,
			};
		}

		[SerializeReference] private ApplicableValueSprite m_sprite = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();

		public ApplicableValue<UnityEngine.Sprite> Sprite
		{
			get
			{
				if (m_sprite == null)
					m_sprite = new ApplicableValueSprite();
				return m_sprite;
			}
		}

		public ApplicableValue<UnityEngine.Color> Color
		{
			get
			{
				if (m_color == null)
					m_color = new ApplicableValueColor();
				return m_color;
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

	}
}
