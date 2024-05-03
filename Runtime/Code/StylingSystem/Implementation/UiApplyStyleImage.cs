using UnityEngine.UI;

namespace GuiToolkit.Style
{
	public class UiApplyStyleImage : UiAbstractApplyStyle<Image, UiStyleImage>
	{
		public override void Apply(UiStyleImage style)
		{
			SpecificMonoBehaviour.color = style.Color;
			SpecificMonoBehaviour.sprite = style.Sprite;
		}

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleImage result = new UiStyleImage();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
			result.Color = SpecificMonoBehaviour.color;
			result.Sprite = SpecificMonoBehaviour.sprite;
			return result;
		}
	}
}