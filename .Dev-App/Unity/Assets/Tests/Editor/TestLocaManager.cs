using NUnit.Framework;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for msgctxt (message context) support in the PO parser.
	/// </summary>
	public class TestLocaManager
	{
		// -------------------------------------------------------------------------
		// Helpers
		// -------------------------------------------------------------------------

		/// <summary>
		/// Creates a fresh <see cref="LocaManagerDefaultImpl"/> and immediately
		/// parses the supplied PO content into it, bypassing Unity Resources loading.
		/// </summary>
		private static LocaManagerDefaultImpl CreateAndParse(string _poContent, string _group = null)
		{
			var impl = new LocaManagerDefaultImpl();
			impl.ParsePoContentForTest(_poContent, _group);
			return impl;
		}

		// -------------------------------------------------------------------------
		// Tests
		// -------------------------------------------------------------------------

		[Test]
		public void ContextKey_ComposedWithUnitSeparator()
		{
			const string context = "finance";
			const string key     = "Bank";
			string composed = $"{context}\u0004{key}";
			Assert.AreEqual("finance\u0004Bank", composed,
				"Context key must be composed as context + U+0004 + msgid");
		}

		[Test]
		public void ContextKey_EmptyContextLeavesKeyUnchanged()
		{
			const string key = "Bank";
			string composed = string.IsNullOrEmpty(string.Empty) ? key : $"{string.Empty}\u0004{key}";
			Assert.AreEqual("Bank", composed,
				"An empty context must not alter the key");
		}

		[Test]
		public void PO_ParseWithMsgctxt_TwoEntriesSameMsgid()
		{
			const string po =
				"msgctxt \"finance\"\n"
				+ "msgid \"Bank\"\n"
				+ "msgstr \"Financial institution\"\n"
				+ "\n"
				+ "msgctxt \"nature\"\n"
				+ "msgid \"Bank\"\n"
				+ "msgstr \"Riverbank\"\n";

			var impl = CreateAndParse(po);

			string financeTranslation = impl.Translate("Bank", "finance");
			string natureTranslation  = impl.Translate("Bank", "nature");

			Assert.AreEqual("Financial institution", financeTranslation,
				"'Bank' with context 'finance' should translate to 'Financial institution'");
			Assert.AreEqual("Riverbank", natureTranslation,
				"'Bank' with context 'nature' should translate to 'Riverbank'");
		}

		[Test]
		public void PO_ParseWithoutMsgctxt_ContextOverloadFallsBackToPlainKey()
		{
			const string po =
				"msgid \"Hello\"\n"
				+ "msgstr \"Hallo\"\n";

			var impl = CreateAndParse(po);

			// Translating without context must return the plain translation.
			Assert.AreEqual("Hallo", impl.Translate("Hello"),
				"Plain key without context must still be translated");

			// Translating with null/empty context must also return the plain translation.
			Assert.AreEqual("Hallo", impl.Translate("Hello", (string)null),
				"Null context must fall back to the plain key translation");
		}

		[Test]
		public void PO_ParseWithMsgctxt_PlainKeyRemainsInaccessibleViaContextLookup()
		{
			// When a key is stored without context, a context-qualified lookup must not match it.
			const string po =
				"msgid \"OK\"\n"
				+ "msgstr \"In Ordnung\"\n";

			var impl = CreateAndParse(po);

			// A lookup with an arbitrary context should NOT return the context-less entry.
			string result = impl.Translate("OK", "some_context", null,
				LocaManager.RetValIfNotFound.Null);
			Assert.IsNull(result,
				"A context-qualified lookup must not match a context-less entry");
		}

		[Test]
		public void PO_ParseWithMsgctxt_MsgctxtDoesNotSurviveToNextEntry()
		{
			// A msgctxt preceding one entry must not contaminate the entry that follows it.
			const string po =
				"msgctxt \"finance\"\n"
				+ "msgid \"Bank\"\n"
				+ "msgstr \"Financial institution\"\n"
				+ "\n"
				+ "msgid \"River\"\n"
				+ "msgstr \"Fluss\"\n";

			var impl = CreateAndParse(po);

			Assert.AreEqual("Fluss", impl.Translate("River"),
				"'River' has no context and must be translated as a plain key");

			// Ensure the finance-Bank entry is still correct.
			Assert.AreEqual("Financial institution", impl.Translate("Bank", "finance"),
				"'Bank' with context 'finance' must still be translated correctly");
		}
	}
}
