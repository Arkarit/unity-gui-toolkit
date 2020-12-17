namespace GuiToolkit
{
	public static partial class LocaPlurals
	{
		public static (int numPluralForms, int pluralIdx) GetPluralIdx(string _languageId, int _number)
		{
			int numPluralForms = 0;
			int pluralIdx = 0;

			GetPluralIdx(_languageId, _number, ref numPluralForms, ref pluralIdx);

			return (numPluralForms, pluralIdx);
		}

		static partial void GetPluralIdx(string _languageId, int _number, ref int _numPluralForms, ref int _pluralIdx);
	}
}
