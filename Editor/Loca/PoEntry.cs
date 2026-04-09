using System.Collections.Generic;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Data model for a single entry in a GNU gettext PO or POT file.
	/// Covers singular, plural, and obsolete entries with all standard comment types.
	/// </summary>
	public class PoEntry
	{
		/// <summary>Extracted comments for translators, from <c>#.</c> lines.</summary>
		public List<string> TranslatorComments = new List<string>();

		/// <summary>Source file references, from <c>#:</c> lines.</summary>
		public List<string> SourceReferences = new List<string>();

		/// <summary>Whether this entry is marked fuzzy via <c>#, fuzzy</c>.</summary>
		public bool IsFuzzy;

		/// <summary>Whether this entry is obsolete (all keyword lines prefixed with <c>#~</c>).</summary>
		public bool IsObsolete;

		/// <summary>Optional disambiguation context from <c>msgctxt</c>. Null means no context.</summary>
		public string Context;

		/// <summary>The message identifier (<c>msgid</c>).</summary>
		public string MsgId = string.Empty;

		/// <summary>The plural message identifier (<c>msgid_plural</c>). Null if not a plural entry.</summary>
		public string MsgIdPlural;

		/// <summary>
		/// The translated string for singular form (<c>msgstr</c>).
		/// Empty string means untranslated. Null if only plural forms are used.
		/// </summary>
		public string MsgStr = string.Empty;

		/// <summary>
		/// Translated plural forms (<c>msgstr[0]</c>, <c>msgstr[1]</c>, …).
		/// Null if this is not a plural entry.
		/// </summary>
		public string[] MsgStrForms;

		/// <summary>True if this entry has plural forms (<see cref="MsgIdPlural"/> is not null).</summary>
		public bool IsPlural => MsgIdPlural != null;

		/// <summary>
		/// Composed lookup key following the GNU gettext convention:
		/// <c>Context\u0004MsgId</c> when context is present, otherwise just <c>MsgId</c>.
		/// </summary>
		public string ComposedKey => string.IsNullOrEmpty(Context) ? MsgId : $"{Context}\u0004{MsgId}";

		/// <summary>
		/// True if <see cref="MsgStr"/> is non-empty, or any element of <see cref="MsgStrForms"/> is non-empty.
		/// </summary>
		public bool IsTranslated
		{
			get
			{
				if (!string.IsNullOrEmpty(MsgStr))
					return true;
				if (MsgStrForms != null)
					foreach (var form in MsgStrForms)
						if (!string.IsNullOrEmpty(form))
							return true;
				return false;
			}
		}
	}
}
