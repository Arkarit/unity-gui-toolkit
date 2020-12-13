// Auto-generated, please do not change!
// Use menu 'UI Toolkit/Process Loca (Update plurals when added a new language)' to add new language plurals!
namespace GuiToolkit
{
	public static class LocaPlurals
	{
		public static (int numPluralForms, int pluralIdx) GetPluralIdx(string _languageId, int _number)
		{
			int nplurals = 0, n = _number;
			CBool plural = 0;
			
			switch (_languageId)
			{
				case "de":
					nplurals=2; plural=(n != 1);
					break;
			}
			
			int numPluralForms = nplurals;
			int pluralIdx = plural;
			
			return (numPluralForms, pluralIdx);
		}
	}
}
