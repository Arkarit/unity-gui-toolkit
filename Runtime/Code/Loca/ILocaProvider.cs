using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaProvider
	{
		public void InitData( string _languageId );
		public string Translate(string _s, string _group = null);
		public string Translate(string _singularKey, string _pluralKey, int _n, string _group = null);

		public void ChangeLanguage( string _languageId );

#if UNITY_EDITOR
		public void CollectData();
#endif
	}
}