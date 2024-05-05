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

			SpecificMonoBehaviour.sprite = SpecificStyle.Sprite;
			SpecificMonoBehaviour.overrideSprite = SpecificStyle.OverrideSprite;
			SpecificMonoBehaviour.type = SpecificStyle.Type;
			SpecificMonoBehaviour.preserveAspect = SpecificStyle.PreserveAspect;
			SpecificMonoBehaviour.fillCenter = SpecificStyle.FillCenter;
			SpecificMonoBehaviour.fillMethod = SpecificStyle.FillMethod;
			SpecificMonoBehaviour.fillAmount = SpecificStyle.FillAmount;
			SpecificMonoBehaviour.fillClockwise = SpecificStyle.FillClockwise;
			SpecificMonoBehaviour.fillOrigin = SpecificStyle.FillOrigin;
			SpecificMonoBehaviour.useSpriteMesh = SpecificStyle.UseSpriteMesh;
			SpecificMonoBehaviour.material = SpecificStyle.Material;
			SpecificMonoBehaviour.maskable = SpecificStyle.Maskable;
			SpecificMonoBehaviour.isMaskingGraphic = SpecificStyle.IsMaskingGraphic;
			SpecificMonoBehaviour.color = SpecificStyle.Color;
		}

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleImage result = new UiStyleImage();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
			result.Sprite = SpecificMonoBehaviour.sprite;
			result.OverrideSprite = SpecificMonoBehaviour.overrideSprite;
			result.Type = SpecificMonoBehaviour.type;
			result.PreserveAspect = SpecificMonoBehaviour.preserveAspect;
			result.FillCenter = SpecificMonoBehaviour.fillCenter;
			result.FillMethod = SpecificMonoBehaviour.fillMethod;
			result.FillAmount = SpecificMonoBehaviour.fillAmount;
			result.FillClockwise = SpecificMonoBehaviour.fillClockwise;
			result.FillOrigin = SpecificMonoBehaviour.fillOrigin;
			result.UseSpriteMesh = SpecificMonoBehaviour.useSpriteMesh;
			result.Material = SpecificMonoBehaviour.material;
			result.Maskable = SpecificMonoBehaviour.maskable;
			result.IsMaskingGraphic = SpecificMonoBehaviour.isMaskingGraphic;
			result.Color = SpecificMonoBehaviour.color;

			return result;
		}
	}
}
