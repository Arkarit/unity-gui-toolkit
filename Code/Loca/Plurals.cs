namespace GuiToolkit
{
	public static class Plurals
	{
		// Mimicking C bool handling
		public struct CBool
		{
			private int m_value;
			public static implicit operator bool(CBool d) => d.m_value != 0;
			public static implicit operator int(CBool d) => d.m_value;
			public static implicit operator CBool(int d) => new CBool(d);
			public static implicit operator CBool(bool d) => new CBool(d);
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
				case "cr":
					nplurals=3; plural=n%10==1 && n%100!=11 ? 0 : n%10>=2 && n%10<=4 && (n%100<10 || n%100>=20) ? 1 : 2;
					break;
			}

			int numPluralForms = nplurals;
			int pluralIdx = plural;

			return (numPluralForms, pluralIdx); 
		}
	}
}