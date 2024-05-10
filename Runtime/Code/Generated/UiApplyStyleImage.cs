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
			if (SpecificStyle.Type.IsApplicable)
				try { SpecificMonoBehaviour.type = SpecificStyle.Type.Value; } catch {}
			if (SpecificStyle.PreserveAspect.IsApplicable)
				try { SpecificMonoBehaviour.preserveAspect = SpecificStyle.PreserveAspect.Value; } catch {}
			if (SpecificStyle.FillCenter.IsApplicable)
				try { SpecificMonoBehaviour.fillCenter = SpecificStyle.FillCenter.Value; } catch {}
			if (SpecificStyle.FillMethod.IsApplicable)
				try { SpecificMonoBehaviour.fillMethod = SpecificStyle.FillMethod.Value; } catch {}
			if (SpecificStyle.FillAmount.IsApplicable)
				try { SpecificMonoBehaviour.fillAmount = SpecificStyle.FillAmount.Value; } catch {}
			if (SpecificStyle.FillClockwise.IsApplicable)
				try { SpecificMonoBehaviour.fillClockwise = SpecificStyle.FillClockwise.Value; } catch {}
			if (SpecificStyle.FillOrigin.IsApplicable)
				try { SpecificMonoBehaviour.fillOrigin = SpecificStyle.FillOrigin.Value; } catch {}
			if (SpecificStyle.UseSpriteMesh.IsApplicable)
				try { SpecificMonoBehaviour.useSpriteMesh = SpecificStyle.UseSpriteMesh.Value; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificMonoBehaviour.material = SpecificStyle.Material.Value; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificMonoBehaviour.maskable = SpecificStyle.Maskable.Value; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificMonoBehaviour.isMaskingGraphic = SpecificStyle.IsMaskingGraphic.Value; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificMonoBehaviour.color = SpecificStyle.Color.Value; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleImage result = new UiStyleImage();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
			try { result.Sprite.Value = SpecificMonoBehaviour.sprite; } catch {}
			try { result.OverrideSprite.Value = SpecificMonoBehaviour.overrideSprite; } catch {}
			try { result.Type.Value = SpecificMonoBehaviour.type; } catch {}
			try { result.PreserveAspect.Value = SpecificMonoBehaviour.preserveAspect; } catch {}
			try { result.FillCenter.Value = SpecificMonoBehaviour.fillCenter; } catch {}
			try { result.FillMethod.Value = SpecificMonoBehaviour.fillMethod; } catch {}
			try { result.FillAmount.Value = SpecificMonoBehaviour.fillAmount; } catch {}
			try { result.FillClockwise.Value = SpecificMonoBehaviour.fillClockwise; } catch {}
			try { result.FillOrigin.Value = SpecificMonoBehaviour.fillOrigin; } catch {}
			try { result.UseSpriteMesh.Value = SpecificMonoBehaviour.useSpriteMesh; } catch {}
			try { result.Material.Value = SpecificMonoBehaviour.material; } catch {}
			try { result.Maskable.Value = SpecificMonoBehaviour.maskable; } catch {}
			try { result.IsMaskingGraphic.Value = SpecificMonoBehaviour.isMaskingGraphic; } catch {}
			try { result.Color.Value = SpecificMonoBehaviour.color; } catch {}

			return result;
		}
	}
}
