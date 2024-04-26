using UnityEngine.UI;

namespace GuiToolkit.Style
{
	public class UiApplyStyleImage : UiAbstractApplyStyle<Image, UiStyleImage>
	{
		public override void Apply(UiStyleImage style)
		{
			SpecificMonoBehaviour.color = style.Color;
		}

		public override UiAbstractStyleBase CreateStyle()
		{
			UiStyleImage result = new UiStyleImage();

			if (!SpecificMonoBehaviour)
				return result;

			result.Color = SpecificMonoBehaviour.color;
			return result;
		}
	}
}