using UnityEngine.UI;

namespace GuiToolkit.Style
{
	public class UiApplyStyleImage : UiAbstractApplyStyle<Image, UiStyleImage>
	{
		public override void Apply()
		{
			SpecificMonoBehaviour.color = SpecificStyle.Color;
			SpecificMonoBehaviour.sprite = SpecificStyle.Sprite;
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