namespace GuiToolkit
{
	public class UiLocaManagerImpl : UiLocaManager
	{
		public override bool ChangeLanguageImpl( string _languageId )
		{
			return true;
		}

		public override string Translate( string _s )
		{
			return _s;
		}
	}
}