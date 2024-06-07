// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{	[ExecuteAlways]
	public class UiApplyStyleImage : UiAbstractApplyStyle<UnityEngine.UI.Image, UiStyleImage>
	{
		public override void Apply()
		{
			if (!SpecificMonoBehaviour || SpecificStyle == null)
				return;

			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificMonoBehaviour.sprite = SpecificStyle.Sprite.Value; } catch {}
			if (SpecificStyle.OverrideSprite.IsApplicable)
				try { SpecificMonoBehaviour.overrideSprite = SpecificStyle.OverrideSprite.Value; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificMonoBehaviour.material = SpecificStyle.Material.Value; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificMonoBehaviour.color = SpecificStyle.Color.Value; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleImage result = new UiStyleImage();

			if (!SpecificMonoBehaviour)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleImage) _template;

				result.Sprite.Value = specificTemplate.Sprite.Value;
				result.Sprite.IsApplicable = specificTemplate.Sprite.IsApplicable;
				result.OverrideSprite.Value = specificTemplate.OverrideSprite.Value;
				result.OverrideSprite.IsApplicable = specificTemplate.OverrideSprite.IsApplicable;
				result.Material.Value = specificTemplate.Material.Value;
				result.Material.IsApplicable = specificTemplate.Material.IsApplicable;
				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;

				return result;
			}

			try { result.Sprite.Value = SpecificMonoBehaviour.sprite; } catch {}
			try { result.OverrideSprite.Value = SpecificMonoBehaviour.overrideSprite; } catch {}
			try { result.Material.Value = SpecificMonoBehaviour.material; } catch {}
			try { result.Color.Value = SpecificMonoBehaviour.color; } catch {}

			return result;
		}
	}
}
