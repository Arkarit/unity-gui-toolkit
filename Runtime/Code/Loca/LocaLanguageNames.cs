using System.Collections.Generic;

namespace GuiToolkit
{
	/// <summary>
	/// Maps IETF language tags to their native (endonym) display names.
	/// Used by <see cref="UiLanguageSelectDropdown"/> to label dropdown entries in the speaker's own language.
	/// </summary>
	public static class LocaLanguageNames
	{
		private static readonly Dictionary<string, string> s_nativeNames = new Dictionary<string, string>
		{
			{ "af",    "Afrikaans" },
			{ "ar",    "العربية" },
			{ "az",    "Azərbaycan" },
			{ "be",    "Беларуская" },
			{ "bg",    "Български" },
			{ "bn",    "বাংলা" },
			{ "bs",    "Bosanski" },
			{ "ca",    "Català" },
			{ "cs",    "Čeština" },
			{ "cy",    "Cymraeg" },
			{ "da",    "Dansk" },
			{ "de",    "Deutsch" },
			{ "el",    "Ελληνικά" },
			{ "en",    "English" },
			{ "en-us", "English (US)" },
			{ "en-gb", "English (UK)" },
			{ "es",    "Español" },
			{ "et",    "Eesti" },
			{ "eu",    "Euskara" },
			{ "fa",    "فارسی" },
			{ "fi",    "Suomi" },
			{ "fr",    "Français" },
			{ "ga",    "Gaeilge" },
			{ "gl",    "Galego" },
			{ "gu",    "ગુજરાતી" },
			{ "he",    "עברית" },
			{ "hi",    "हिन्दी" },
			{ "hr",    "Hrvatski" },
			{ "hu",    "Magyar" },
			{ "hy",    "Հայերեն" },
			{ "id",    "Indonesia" },
			{ "is",    "Íslenska" },
			{ "it",    "Italiano" },
			{ "ja",    "日本語" },
			{ "ka",    "ქართული" },
			{ "kk",    "Қазақша" },
			{ "km",    "ខ្មែរ" },
			{ "kn",    "ಕನ್ನಡ" },
			{ "ko",    "한국어" },
			{ "lt",    "Lietuvių" },
			{ "lv",    "Latviešu" },
			{ "mk",    "Македонски" },
			{ "ml",    "മലയാളം" },
			{ "mn",    "Монгол" },
			{ "mr",    "मराठी" },
			{ "ms",    "Melayu" },
			{ "my",    "မြန်မာဘာသာ" },
			{ "nb",    "Norsk bokmål" },
			{ "nl",    "Nederlands" },
			{ "no",    "Norsk" },
			{ "pa",    "ਪੰਜਾਬੀ" },
			{ "pl",    "Polski" },
			{ "pt",    "Português" },
			{ "pt-br", "Português (Brasil)" },
			{ "pt-pt", "Português (Portugal)" },
			{ "ro",    "Română" },
			{ "ru",    "Русский" },
			{ "si",    "සිංහල" },
			{ "sk",    "Slovenčina" },
			{ "sl",    "Slovenščina" },
			{ "sq",    "Shqip" },
			{ "sr",    "Српски" },
			{ "sv",    "Svenska" },
			{ "sw",    "Kiswahili" },
			{ "ta",    "தமிழ்" },
			{ "te",    "తెలుగు" },
			{ "th",    "ภาษาไทย" },
			{ "tl",    "Filipino" },
			{ "tr",    "Türkçe" },
			{ "uk",    "Українська" },
			{ "ur",    "اردو" },
			{ "uz",    "O'zbek" },
			{ "vi",    "Tiếng Việt" },
			{ "zh",    "中文（简体）" },
			{ "zh-cn", "中文（简体）" },
			{ "zh-tw", "中文（繁體）" },
			{ "zh-hk", "中文（香港）" },
			{ "zu",    "isiZulu" },
			{ "dev",   "[DEV]" },
		};

		/// <summary>
		/// Returns the native display name for <paramref name="_languageId"/>.
		/// Falls back to returning the ID itself if no entry is found.
		/// </summary>
		public static string GetNativeName(string _languageId)
		{
			if (string.IsNullOrEmpty(_languageId))
				return _languageId;

			// Normalize silently for display — warnings are emitted at ChangeLanguage() time.
			string normalized = _languageId.Replace('_', '-').ToLowerInvariant();
			if (s_nativeNames.TryGetValue(normalized, out string name))
				return name;

			return _languageId;
		}
	}
}
