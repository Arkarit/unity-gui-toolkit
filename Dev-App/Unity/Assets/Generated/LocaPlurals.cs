// Auto-generated, please do not change!
// Use menu 'UI Toolkit/Process Loca (Update plurals when added a new language)' to add new language plurals!
namespace GuiToolkit
{
	public static partial class LocaPlurals
	{
		static partial void GetPluralIdx(string _languageId, int _number, ref int _numPluralForms, ref int _pluralIdx)
		{
			int nplurals = 0, n = _number;
			CBool plural = 0;
			
			switch (_languageId)
			{
				case "dev":
					nplurals=2; plural=(n != 1);
					break;
				case "de":
					nplurals=2; plural=(n != 1);
					break;
				case "en_us":
					nplurals=2; plural=(n != 1);
					break;
				case "lol":
					nplurals=1; plural=0;
					break;
			}

			_numPluralForms = nplurals;
			_pluralIdx = plural;
		}
	}
}
