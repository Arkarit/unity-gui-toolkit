using System.Collections.Generic;
using NUnit.Framework;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	public class TestLocaCsvExporter
	{
		private static string[] CsvLines(string _csv)
			=> _csv.Replace("\r\n", "\n").Split('\n');

		private static string[] SplitRow(string _row)
			=> _row.Split(',');

		[Test]
		public void CsvHasHeaderRow()
		{
			var data = new List<(string lang, PoFile file)>
			{
				("en", PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hello\"\n")),
			};
			string csv = LocaCsvExporter.BuildCsvFromData(data);
			string[] headers = SplitRow(CsvLines(csv)[0]);
			Assert.That(headers[0], Is.EqualTo("Key"));
			Assert.That(headers[1], Is.EqualTo("Context"));
			Assert.That(headers[2], Is.EqualTo("en"));
		}

		[Test]
		public void CsvSingularEntry()
		{
			var data = new List<(string lang, PoFile file)>
			{
				("en", PoFile.Parse("msgid \"Hello\"\nmsgstr \"World\"\n")),
			};
			string csv = LocaCsvExporter.BuildCsvFromData(data);
			string[] lines = CsvLines(csv);
			string[] row = SplitRow(lines[1]);
			Assert.That(row[0], Is.EqualTo("Hello"));
			Assert.That(row[1], Is.EqualTo(string.Empty));
			Assert.That(row[2], Is.EqualTo("World"));
		}

		[Test]
		public void CsvPluralEntryHasPluralColumns()
		{
			const string po =
				"msgid \"apple\"\n"
				+ "msgid_plural \"apples\"\n"
				+ "msgstr[0] \"Apfel\"\n"
				+ "msgstr[1] \"Äpfel\"\n";
			var data = new List<(string lang, PoFile file)>
			{
				("de", PoFile.Parse(po)),
			};
			string csv = LocaCsvExporter.BuildCsvFromData(data);
			string[] headers = SplitRow(CsvLines(csv)[0]);
			Assert.That(headers[2], Is.EqualTo("de"));
			Assert.That(headers[3], Is.EqualTo("de[0]"));
			Assert.That(headers[4], Is.EqualTo("de[1]"));
		}

		[Test]
		public void CsvContextEntry()
		{
			const string po = "msgctxt \"ui\"\nmsgid \"OK\"\nmsgstr \"Bestätigen\"\n";
			var data = new List<(string lang, PoFile file)>
			{
				("de", PoFile.Parse(po)),
			};
			string csv = LocaCsvExporter.BuildCsvFromData(data);
			string[] row = SplitRow(CsvLines(csv)[1]);
			Assert.That(row[0], Is.EqualTo("OK"));
			Assert.That(row[1], Is.EqualTo("ui"));
		}

		[Test]
		public void CsvAllLanguagesPresent()
		{
			var data = new List<(string lang, PoFile file)>
			{
				("en", PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hello\"\n")),
				("de", PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hallo\"\n")),
			};
			string csv = LocaCsvExporter.BuildCsvFromData(data);
			string[] headers = SplitRow(CsvLines(csv)[0]);
			Assert.That(headers, Has.Member("en"));
			Assert.That(headers, Has.Member("de"));
		}

		[Test]
		public void CsvMissingTranslationEmpty()
		{
			// "en" has "Hello", "de" has "Welt" — neither shares a key with the other.
			var data = new List<(string lang, PoFile file)>
			{
				("en", PoFile.Parse("msgid \"Hello\"\nmsgstr \"Hello\"\n")),
				("de", PoFile.Parse("msgid \"Welt\"\nmsgstr \"Welt\"\n")),
			};
			string csv = LocaCsvExporter.BuildCsvFromData(data);
			string[] lines = CsvLines(csv);
			// Header: Key, Context, de, en  (sorted: de before en)
			// Data rows sorted by key: "Hello" < "Welt"
			// lines[1] = Hello row: Hello,,<empty>,Hello
			string[] helloRow = SplitRow(lines[1]);
			Assert.That(helloRow[0], Is.EqualTo("Hello"));
			Assert.That(helloRow[2], Is.EqualTo(string.Empty)); // de has no "Hello"
			Assert.That(helloRow[3], Is.EqualTo("Hello"));      // en has "Hello"
		}
	}
}
