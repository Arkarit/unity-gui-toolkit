using System.Collections.Generic;

namespace GuiToolkit.Style.Editor
{
	public class UiStyleEditorFilter
	{
		private readonly Dictionary<string, List<string>> m_searchStringByKeyword = new();
		private bool m_showAll = true;
		private string m_currentKeyword = string.Empty;
		private string m_currentWord = string.Empty;
		private string m_originalString = string.Empty;

		public bool ShowAll => m_showAll;
		public string OriginalString => m_originalString;

		public void Update(string _filterInputString) => Init(_filterInputString);
		public bool HasName(string _name) => Has(_name, string.Empty, true);
		public bool HasSkin(string _skin) => Has(_skin, "skin", false);
		public bool HasType(string _type) => Has(_type, "t", false);

		public bool Has(string _name, string _keyword, bool _and)
		{
			if (m_showAll)
				return true;
			
			_name = _name.ToLower();
			
			if (m_searchStringByKeyword.TryGetValue(_keyword, out List<string> values))
			{
				foreach (var value in values)
				{
					bool contains = _name.Contains(value);
					if (_and)
					{
						if (!contains)
							return false;
						continue;
					}
					
					if (contains)
						return true;
				}
			}
			else
			{
				return true;
			}
			
			return _and;
		}

		private void Init(string _filterInputString)
		{
			m_searchStringByKeyword.Clear();
			m_showAll = true;
			m_currentKeyword = string.Empty;
			m_currentWord = string.Empty;
			m_originalString = _filterInputString;
			_filterInputString = _filterInputString.ToLower();
			
			for (int i = 0; i < _filterInputString.Length; i++)
			{
				var c = _filterInputString[i];

				if (c == ':')
				{
					if (!hasCurrentWord)
						continue;

					if (hasCurrentKeyword)
					{
						m_currentWord += m_currentKeyword + ":";
						m_currentKeyword = string.Empty;
						continue;
					}

					m_currentKeyword = m_currentWord;
					m_currentWord = string.Empty;
					continue;
				}

				if (c == ' ')
				{
					if (hasCurrentKeyword)
					{
						if (!hasCurrentWord)
							continue;

						AddToFilter();
						continue;
					}
					
					AddToFilter();
					continue;
				}

				m_currentWord += c;
			}

			if (hasCurrentWord)
				AddToFilter();
		}

		private bool hasCurrentKeyword => !string.IsNullOrEmpty(m_currentKeyword);
		private bool hasCurrentWord => !string.IsNullOrEmpty(m_currentWord);
		private void AddToFilter()
		{
			if (m_currentKeyword == null)
				m_currentKeyword = string.Empty;

			if (!m_searchStringByKeyword.ContainsKey(m_currentKeyword))
				m_searchStringByKeyword.Add(m_currentKeyword, new List<string>());

			m_searchStringByKeyword[m_currentKeyword].Add(m_currentWord);
			m_currentKeyword = string.Empty;
			m_currentWord = string.Empty;
			m_showAll = false;
		}
	}
}
