namespace GuiToolkit.Style
{
	public static class UiStyleManager
	{
		public static bool SetSkin(string _skinName, float _tweenDuration = 0)
		{
			var styleConfig = UiStyleConfig.Instance;
			var previousSkin = styleConfig.CurrentSkin;

			if (!styleConfig.SetCurrentSkin(_skinName, false))
				return false;

			if (_tweenDuration > 0)
			{
				var currentSkin = styleConfig.CurrentSkin;
				if (currentSkin == null)
					return false;


			}
			return true;
		}
	}
}
