// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
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

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueSprite : ApplicableValue<UnityEngine.Sprite> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				Color,
				Enabled,
				Sprite,
			};
		}

#if UNITY_EDITOR
		public override List<ValueInfo> GetValueInfos()
		{
			return new List<ValueInfo>()
			{
				new ValueInfo()
				{
					GetterName = "Color",
					GetterType = typeof(ApplicableValueColor),
					Value = Color,
				},
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "Sprite",
					GetterType = typeof(ApplicableValueSprite),
					Value = Sprite,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueSprite m_sprite = new();

		public ApplicableValue<UnityEngine.Color> Color
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_color == null)
						m_color = new ApplicableValueColor();
				#endif
				return m_color;
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

		public ApplicableValue<UnityEngine.Sprite> Sprite
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_sprite == null)
						m_sprite = new ApplicableValueSprite();
				#endif
				return m_sprite;
			}
		}

	}
}
