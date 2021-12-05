namespace GuiToolkit
{
	public class UiStylingManager
	{
		private static UiStylingManager s_instance;

		public static UiStylingManager Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = new UiStylingManager();
				}
				return s_instance;
			}
		}
	}
}