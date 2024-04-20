using UnityEngine;

namespace GuiToolkit
{
	/// \brief MonoBehaviour with added Loca functions (according to gettext standard)
	public class LocaMonoBehaviour : MonoBehaviour
	{
		/// Note that the convenient but weird "_" name is standard in gettext/po/pot environment, so don't blame me :-P
		protected string _(string _s)
		{
			return gettext(_s);
		}

		protected string gettext(string _s)
		{
			return LocaManager.Instance.Translate(_s);
		}

		protected string _n(string _singular, string _plural, int _n)
		{
			return ngettext(_singular, _plural, _n);
		}

		protected string ngettext(string _singular, string _plural, int _n)
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