using UnityEngine.UI;

namespace GuiToolkit.Style
{
	public class UiApplyStyleImage : UiAbstractApplyStyle<Image, UiStyleImage>
	{
		public override void Apply(Image image, UiStyleImage style)
		{
			image.color = style.color;
		}
	}
}