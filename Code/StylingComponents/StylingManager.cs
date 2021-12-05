namespace GuiToolkit
{
	public class StylingManager
	{
		private static StylingManager s_instance;

		public static StylingManager Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = new StylingManager();
				}
				return s_instance;
			}
		}
	}
}