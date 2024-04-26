using UnityEngine.UI;

namespace GuiToolkit.Style
{
	public class UiApplyStyleImage : UiAbstractApplyStyle<Image, UiStyleImage>
	{
		public override void Apply(UiStyleImage style)
		{
			SpecificMonoBehaviour.color = style.color;
		}

		public override UiAbstractStyleBase CreateStyle()
		{
			UiStyleImage result = new UiStyleImage();
			result.color = SpecificMonoBehaviour.color;
			return result;
		}
	}
}