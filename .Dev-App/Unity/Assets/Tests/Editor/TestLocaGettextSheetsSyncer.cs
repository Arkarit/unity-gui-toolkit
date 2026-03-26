using System.Collections.Generic;
using NUnit.Framework;
using GuiToolkit;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	public class TestLocaGettextSheetsSyncer
	{
		// -----------------------------------------------------------------------
		// ParseSheetValues
		// -----------------------------------------------------------------------

		[Test]
		public void ParseSheetValues_EmptyString_ReturnsEmptyList()
		{
			var result = LocaGettextSheetsSyncer.ParseSheetValues(string.Empty);
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void ParseSheetValues_NoValuesKey_ReturnsEmptyList()
		{
			var result = LocaGettextSheetsSyncer.ParseSheetValues("{\"range\":\"Sheet1\"}");
			Assert.That(result, Is.Empty);
		}

		[Test]
		public void ParseSheetValues_SimpleGrid_ReturnsRows()
		{
			const string json = "{\"values\":[[\"Key\",\"en\",\"de\"],[\"hello\",\"Hello\",\"Hallo\"]]}";
			var result = LocaGettextSheetsSyncer.ParseSheetValues(json);

			Assert.That(result.Count, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo(new List<string> { "Key", "en", "de" }));
			Assert.That(result[1], Is.EqualTo(new List<string> { "hello", "Hello", "Hallo" }));
		}

		[Test]
		public void ParseSheetValues_WithEscapes_DecodesCorrectly()
		{
			const string json = "{\"values\":[[\"line1\\nline2\",\"tab\\there\"]]}";
			var result = LocaGettextSheetsSyncer.ParseSheetValues(json);

			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0][0], Is.EqualTo("line1\nline2"));
			Assert.That(result[0][1], Is.EqualTo("tab\there"));
		}

		[Test]
		public void ParseSheetValues_EmptyValues_ReturnsEmptyOuterList()
		{
			const string json = "{\"values\":[]}";
			var result = LocaGettextSheetsSyncer.ParseSheetValues(json);
			Assert.That(result, Is.Empty);
		}

		// -----------------------------------------------------------------------
		// BuildValuesJson
		// -----------------------------------------------------------------------

		[Test]
		public void BuildValuesJson_SingleRow_ProducesValidJson()
		{
			var rows = new List<List<string>> { new List<string> { "hello", "world" } };
			string json = LocaGettextSheetsSyncer.BuildValuesJson(rows);

			Assert.That(json, Is.EqualTo("{\"values\":[[\"hello\",\"world\"]]}"));
		}

		[Test]
		public void BuildValuesJson_MultipleRows_SeparatedByComma()
		{
			var rows = new List<List<string>>
			{
				new List<string> { "a", "b" },
				new List<string> { "c", "d" },
			};
			string json = LocaGettextSheetsSyncer.BuildValuesJson(rows);

			Assert.That(json, Is.EqualTo("{\"values\":[[\"a\",\"b\"],[\"c\",\"d\"]]}"));
		}

		[Test]
		public void BuildValuesJson_SpecialCharsAreEscaped()
		{
			var rows = new List<List<string>> { new List<string> { "say \"hi\"", "line\nnext" } };
			string json = LocaGettextSheetsSyncer.BuildValuesJson(rows);

			Assert.That(json, Does.Contain("\\\"hi\\\""));
			Assert.That(json, Does.Contain("\\n"));
		}

		// -----------------------------------------------------------------------
		// JSON round-trip
		// -----------------------------------------------------------------------

		[Test]
		public void JsonRoundTrip_SimpleGrid_IdenticalAfterParsing()
		{
			var original = new List<List<string>>
			{
				new List<string> { "Key", "en", "de" },
				new List<string> { "save", "Save", "Speichern" },
				new List<string> { "cancel", "Cancel", "Abbrechen" },
			};

			string json   = LocaGettextSheetsSyncer.BuildValuesJson(original);
			var   parsed  = LocaGettextSheetsSyncer.ParseSheetValues(json);

			Assert.That(parsed.Count, Is.EqualTo(original.Count));
			for (int r = 0; r < original.Count; r++)
			{
				Assert.That(parsed[r], Is.EqualTo(original[r]),
					$"Row {r} differs after round-trip");
			}
		}

		[Test]
		public void JsonRoundTrip_WithSpecialChars_IdenticalAfterParsing()
		{
			var original = new List<List<string>>
			{
				new List<string> { "quote", "say \"hello\"" },
				new List<string> { "newline", "line1\nline2" },
				new List<string> { "backslash", "C:\\path\\file" },
			};

			string json  = LocaGettextSheetsSyncer.BuildValuesJson(original);
			var   parsed = LocaGettextSheetsSyncer.ParseSheetValues(json);

			Assert.That(parsed.Count, Is.EqualTo(original.Count));
			for (int r = 0; r < original.Count; r++)
				Assert.That(parsed[r], Is.EqualTo(original[r]), $"Row {r} differs");
		}

		// -----------------------------------------------------------------------
		// ExtractSpreadsheetId
		// -----------------------------------------------------------------------

		[Test]
		public void ExtractSpreadsheetId_EditUrl_ReturnsId()
		{
			const string url = "https://docs.google.com/spreadsheets/d/ABCDEF123/edit#gid=0";
			string id = LocaGettextSheetsSyncer.ExtractSpreadsheetId(url);
			Assert.That(id, Is.EqualTo("ABCDEF123"));
		}

		[Test]
		public void ExtractSpreadsheetId_ExportUrl_ReturnsId()
		{
			const string url = "https://docs.google.com/spreadsheets/d/XYZ789/export?format=xlsx";
			string id = LocaGettextSheetsSyncer.ExtractSpreadsheetId(url);
			Assert.That(id, Is.EqualTo("XYZ789"));
		}

		[Test]
		public void ExtractSpreadsheetId_Null_ReturnsNull()
		{
			Assert.That(LocaGettextSheetsSyncer.ExtractSpreadsheetId(null), Is.Null);
		}

		[Test]
		public void ExtractSpreadsheetId_Empty_ReturnsNull()
		{
			Assert.That(LocaGettextSheetsSyncer.ExtractSpreadsheetId(string.Empty), Is.Null);
		}

		[Test]
		public void ExtractSpreadsheetId_NoSlashD_ReturnsNull()
		{
			const string url = "https://docs.google.com/spreadsheets/edit?id=ABCDEF";
			Assert.That(LocaGettextSheetsSyncer.ExtractSpreadsheetId(url), Is.Null);
		}

		// -----------------------------------------------------------------------
		// ReverseKeyAffixes
		// -----------------------------------------------------------------------

		[Test]
		public void ReverseKeyAffixes_NoAffixes_Unchanged()
		{
			string result = LocaGettextSheetsSyncer.ReverseKeyAffixes("hello", string.Empty, string.Empty);
			Assert.That(result, Is.EqualTo("hello"));
		}

		[Test]
		public void ReverseKeyAffixes_PrefixStripped()
		{
			string result = LocaGettextSheetsSyncer.ReverseKeyAffixes("ui.ok", "ui.", string.Empty);
			Assert.That(result, Is.EqualTo("ok"));
		}

		[Test]
		public void ReverseKeyAffixes_PostfixStripped()
		{
			string result = LocaGettextSheetsSyncer.ReverseKeyAffixes("ok_btn", string.Empty, "_btn");
			Assert.That(result, Is.EqualTo("ok"));
		}

		[Test]
		public void ReverseKeyAffixes_BothStripped()
		{
			string result = LocaGettextSheetsSyncer.ReverseKeyAffixes("pre_key_post", "pre_", "_post");
			Assert.That(result, Is.EqualTo("key"));
		}

		[Test]
		public void ReverseKeyAffixes_KeyDoesNotMatchPrefix_Unchanged()
		{
			string result = LocaGettextSheetsSyncer.ReverseKeyAffixes("hello", "ui.", string.Empty);
			Assert.That(result, Is.EqualTo("hello"));
		}

		// -----------------------------------------------------------------------
		// BuildColumnsFromPoData
		// -----------------------------------------------------------------------

		[Test]
		public void BuildColumnsFromPoData_EmptyInput_OnlyKeyColumn()
		{
			var cols = LocaGettextSheetsSyncer.BuildColumnsFromPoData(
				new List<(string, PoFile)>());

			Assert.That(cols.Count, Is.EqualTo(1));
			Assert.That(cols[0].ColumnType, Is.EqualTo(LocaExcelBridge.EInColumnType.Key));
		}

		[Test]
		public void BuildColumnsFromPoData_SingleLangNoPlural_KeyPlusSingular()
		{
			var poFile = PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hallo\"\n");
			var cols   = LocaGettextSheetsSyncer.BuildColumnsFromPoData(
				new List<(string, PoFile)> { ("de", poFile) });

			// Key + 1 singular column
			Assert.That(cols.Count, Is.EqualTo(2));
			Assert.That(cols[0].ColumnType, Is.EqualTo(LocaExcelBridge.EInColumnType.Key));
			Assert.That(cols[1].ColumnType, Is.EqualTo(LocaExcelBridge.EInColumnType.LanguageTranslation));
			Assert.That(cols[1].LanguageId, Is.EqualTo("de"));
			Assert.That(cols[1].PluralForm, Is.EqualTo(-1));
		}

		[Test]
		public void BuildColumnsFromPoData_SingleLangWithPlural_IncludesPluralColumns()
		{
			const string po =
				"msgid \"apple\"\nmsgid_plural \"apples\"\nmsgstr[0] \"Apfel\"\nmsgstr[1] \"Äpfel\"\n";
			var poFile = PoFile.Parse(po);
			var cols   = LocaGettextSheetsSyncer.BuildColumnsFromPoData(
				new List<(string, PoFile)> { ("de", poFile) });

			// Key + singular + plural[0] + plural[1]
			Assert.That(cols.Count, Is.EqualTo(4));
			Assert.That(cols[1].PluralForm, Is.EqualTo(-1));  // singular
			Assert.That(cols[2].PluralForm, Is.EqualTo(0));
			Assert.That(cols[3].PluralForm, Is.EqualTo(1));
		}

		[Test]
		public void BuildColumnsFromPoData_MultipleLanguages_AllPresent()
		{
			var enFile = PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hello\"\n");
			var deFile = PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hallo\"\n");
			var cols   = LocaGettextSheetsSyncer.BuildColumnsFromPoData(
				new List<(string, PoFile)> { ("en", enFile), ("de", deFile) });

			var langIds = new HashSet<string>();
			foreach (var c in cols)
				if (c.ColumnType == LocaExcelBridge.EInColumnType.LanguageTranslation)
					langIds.Add(c.LanguageId);

			Assert.That(langIds, Does.Contain("en"));
			Assert.That(langIds, Does.Contain("de"));
		}

		[Test]
		public void BuildColumnsFromPoData_LanguagesSortedAlphabetically()
		{
			var zFile = PoFile.Parse("msgid \"Z\"\nmsgstr \"Z\"\n");
			var aFile = PoFile.Parse("msgid \"A\"\nmsgstr \"A\"\n");
			var cols  = LocaGettextSheetsSyncer.BuildColumnsFromPoData(
				new List<(string, PoFile)> { ("zz", zFile), ("aa", aFile) });

			// col[0] = Key, col[1] = first language alphabetically
			Assert.That(cols[1].LanguageId, Is.EqualTo("aa"));
			Assert.That(cols[2].LanguageId, Is.EqualTo("zz"));
		}

		// -----------------------------------------------------------------------
		// FindNewKeys
		// -----------------------------------------------------------------------

		[Test]
		public void FindNewKeys_AllNew_ReturnsAll()
		{
			var existing = new HashSet<string> { "a", "b" };
			var all      = new List<string> { "c", "d", "e" };
			var result   = LocaGettextSheetsSyncer.FindNewKeys(existing, all);

			Assert.That(result, Is.EqualTo(new List<string> { "c", "d", "e" }));
		}

		[Test]
		public void FindNewKeys_NoneNew_ReturnsEmpty()
		{
			var existing = new HashSet<string> { "a", "b", "c" };
			var all      = new List<string> { "a", "b", "c" };
			var result   = LocaGettextSheetsSyncer.FindNewKeys(existing, all);

			Assert.That(result, Is.Empty);
		}

		[Test]
		public void FindNewKeys_SomeNew_ReturnsMissingSubset()
		{
			var existing = new HashSet<string> { "a", "c" };
			var all      = new List<string> { "a", "b", "c", "d" };
			var result   = LocaGettextSheetsSyncer.FindNewKeys(existing, all);

			Assert.That(result, Is.EqualTo(new List<string> { "b", "d" }));
		}

		// -----------------------------------------------------------------------
		// MergeTranslationIntoPoEntry
		// -----------------------------------------------------------------------

		[Test]
		public void MergeEntry_EmptyPo_GetsTranslation()
		{
			var sheetEntry = new ProcessedLocaEntry { Key = "hello", LanguageId = "de", Text = "Hallo" };
			var poEntry    = new PoEntry { MsgId = "hello", MsgStr = string.Empty };

			bool changed = LocaGettextSheetsSyncer.MergeTranslationIntoPoEntry(sheetEntry, poEntry);

			Assert.That(changed, Is.True);
			Assert.That(poEntry.MsgStr, Is.EqualTo("Hallo"));
		}

		[Test]
		public void MergeEntry_ExistingTranslation_Overwritten()
		{
			var sheetEntry = new ProcessedLocaEntry { Key = "hello", LanguageId = "de", Text = "Neu" };
			var poEntry    = new PoEntry { MsgId = "hello", MsgStr = "Alt" };

			bool changed = LocaGettextSheetsSyncer.MergeTranslationIntoPoEntry(sheetEntry, poEntry);

			Assert.That(changed, Is.True);
			Assert.That(poEntry.MsgStr, Is.EqualTo("Neu"));
		}

		[Test]
		public void MergeEntry_EmptySheetText_PoUnchanged()
		{
			var sheetEntry = new ProcessedLocaEntry { Key = "hello", LanguageId = "de", Text = string.Empty };
			var poEntry    = new PoEntry { MsgId = "hello", MsgStr = string.Empty };

			bool changed = LocaGettextSheetsSyncer.MergeTranslationIntoPoEntry(sheetEntry, poEntry);

			Assert.That(changed, Is.False);
			Assert.That(poEntry.MsgStr, Is.EqualTo(string.Empty));
		}

		[Test]
		public void MergeEntry_PluralForms_MergedIntoEmptyForms()
		{
			var sheetEntry = new ProcessedLocaEntry
			{
				Key = "apple", LanguageId = "de",
				Forms = new[] { "Apfel", "Äpfel" }
			};
			var poEntry = new PoEntry
			{
				MsgId = "apple", MsgIdPlural = "apples",
				MsgStrForms = new[] { string.Empty, string.Empty }
			};

			bool changed = LocaGettextSheetsSyncer.MergeTranslationIntoPoEntry(sheetEntry, poEntry);

			Assert.That(changed, Is.True);
			Assert.That(poEntry.MsgStrForms[0], Is.EqualTo("Apfel"));
			Assert.That(poEntry.MsgStrForms[1], Is.EqualTo("Äpfel"));
		}

		[Test]
		public void MergeEntry_PluralFormsAlreadyTranslated_Overwritten()
		{
			var sheetEntry = new ProcessedLocaEntry
			{
				Key = "apple", LanguageId = "de",
				Forms = new[] { "NeuApfel", "NeuÄpfel" }
			};
			var poEntry = new PoEntry
			{
				MsgId = "apple", MsgIdPlural = "apples",
				MsgStrForms = new[] { "Apfel", "Äpfel" }
			};

			bool changed = LocaGettextSheetsSyncer.MergeTranslationIntoPoEntry(sheetEntry, poEntry);

			Assert.That(changed, Is.True);
			Assert.That(poEntry.MsgStrForms[0], Is.EqualTo("NeuApfel"));
			Assert.That(poEntry.MsgStrForms[1], Is.EqualTo("NeuÄpfel"));
		}

		[Test]
		public void MergeEntry_PluralFormsNull_AllocatesArray()
		{
			var sheetEntry = new ProcessedLocaEntry
			{
				Key = "apple", LanguageId = "de",
				Forms = new[] { "Apfel", "Äpfel" }
			};
			var poEntry = new PoEntry
			{
				MsgId = "apple", MsgIdPlural = "apples",
				MsgStrForms = null
			};

			bool changed = LocaGettextSheetsSyncer.MergeTranslationIntoPoEntry(sheetEntry, poEntry);

			Assert.That(changed, Is.True);
			Assert.That(poEntry.MsgStrForms, Is.Not.Null);
			Assert.That(poEntry.MsgStrForms[0], Is.EqualTo("Apfel"));
		}
	}
}
