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

			if (SpecificStyle.Sprite.IsApplicable);
				SpecificMonoBehaviour.sprite = SpecificStyle.Sprite.Value;
			if (SpecificStyle.OverrideSprite.IsApplicable);
				SpecificMonoBehaviour.overrideSprite = SpecificStyle.OverrideSprite.Value;
			if (SpecificStyle.Type.IsApplicable);
				SpecificMonoBehaviour.type = SpecificStyle.Type.Value;
			if (SpecificStyle.PreserveAspect.IsApplicable);
				SpecificMonoBehaviour.preserveAspect = SpecificStyle.PreserveAspect.Value;
			if (SpecificStyle.FillCenter.IsApplicable);
				SpecificMonoBehaviour.fillCenter = SpecificStyle.FillCenter.Value;
			if (SpecificStyle.FillMethod.IsApplicable);
				SpecificMonoBehaviour.fillMethod = SpecificStyle.FillMethod.Value;
			if (SpecificStyle.FillAmount.IsApplicable);
				SpecificMonoBehaviour.fillAmount = SpecificStyle.FillAmount.Value;
			if (SpecificStyle.FillClockwise.IsApplicable);
				SpecificMonoBehaviour.fillClockwise = SpecificStyle.FillClockwise.Value;
			if (SpecificStyle.FillOrigin.IsApplicable);
				SpecificMonoBehaviour.fillOrigin = SpecificStyle.FillOrigin.Value;
			if (SpecificStyle.UseSpriteMesh.IsApplicable);
				SpecificMonoBehaviour.useSpriteMesh = SpecificStyle.UseSpriteMesh.Value;
			if (SpecificStyle.Material.IsApplicable);
				SpecificMonoBehaviour.material = SpecificStyle.Material.Value;
			if (SpecificStyle.Maskable.IsApplicable);
				SpecificMonoBehaviour.maskable = SpecificStyle.Maskable.Value;
			if (SpecificStyle.IsMaskingGraphic.IsApplicable);
				SpecificMonoBehaviour.isMaskingGraphic = SpecificStyle.IsMaskingGraphic.Value;
			if (SpecificStyle.Color.IsApplicable);
				SpecificMonoBehaviour.color = SpecificStyle.Color.Value;
		}

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleImage result = new UiStyleImage();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
			result.Sprite.Value = SpecificMonoBehaviour.sprite;
			result.OverrideSprite.Value = SpecificMonoBehaviour.overrideSprite;
			result.Type.Value = SpecificMonoBehaviour.type;
			result.PreserveAspect.Value = SpecificMonoBehaviour.preserveAspect;
			result.FillCenter.Value = SpecificMonoBehaviour.fillCenter;
			result.FillMethod.Value = SpecificMonoBehaviour.fillMethod;
			result.FillAmount.Value = SpecificMonoBehaviour.fillAmount;
			result.FillClockwise.Value = SpecificMonoBehaviour.fillClockwise;
			result.FillOrigin.Value = SpecificMonoBehaviour.fillOrigin;
			result.UseSpriteMesh.Value = SpecificMonoBehaviour.useSpriteMesh;
			result.Material.Value = SpecificMonoBehaviour.material;
			result.Maskable.Value = SpecificMonoBehaviour.maskable;
			result.IsMaskingGraphic.Value = SpecificMonoBehaviour.isMaskingGraphic;
			result.Color.Value = SpecificMonoBehaviour.color;

			return result;
		}
	}
}
