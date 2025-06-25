using UnityEngine;

namespace GuiToolkit
{
	/// \brief Generic class with added Loca functions (according to gettext standard)
	public class LocaClass
	{
		/// Note that the convenient but weird "_" name is standard in gettext/po/pot environment, so don't blame me :-P
		protected static string _(string _s)
		{
			return gettext(_s);
		}

		protected static string gettext(string _s)
		{
			return LocaManager.Instance.Translate(_s);
		}

		protected static string _n(string _singular, string _plural, int _n)
		{
			return ngettext(_singular, _plural, _n);
		}

		protected static string ngettext(string _singular, string _plural, int _n)
		{
			return LocaManager.Instance.Translate(_singular, _plural, _n);
		}

		/// Not translated, only for POT creation
		protected static string __(string _s)
		{
			return _s;
		}
	}
}