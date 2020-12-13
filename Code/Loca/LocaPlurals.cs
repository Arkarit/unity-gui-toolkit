// Auto-generated, please do not change!
// Use menu 'UI Toolkit/Process Loca (Update plurals when added a new language)' to add new language plurals!
namespace GuiToolkit
{
	public static class LocaPlurals
	{
		// Mimicking C bool handling
		public struct CBool
		{
			private int m_value;
			public static implicit operator bool(CBool _val) => _val.m_value != 0;
			public static implicit operator int(CBool _val) => _val.m_value;
			public static implicit operator CBool(int _val) => new CBool(_val);
			public static implicit operator CBool(bool _val) => new CBool(_val);
			public CBool(int _val = 0) { m_value = _val; }
			public CBool(bool _val) { m_value = _val ? 1 : 0; }
		}
		
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
