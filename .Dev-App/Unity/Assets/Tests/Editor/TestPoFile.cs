using NUnit.Framework;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	public class TestPoFile
	{
		[Test]
		public void ParseSimpleEntry()
		{
			const string po = "msgid \"Hello\"\nmsgstr \"Hallo\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].MsgId, Is.EqualTo("Hello"));
			Assert.That(file.Entries[0].MsgStr, Is.EqualTo("Hallo"));
		}

		[Test]
		public void ParseWithContext()
		{
			const string po = "msgctxt \"finance\"\nmsgid \"Bank\"\nmsgstr \"Geldinstitut\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].Context, Is.EqualTo("finance"));
			Assert.That(file.Entries[0].MsgId, Is.EqualTo("Bank"));
			Assert.That(file.Entries[0].MsgStr, Is.EqualTo("Geldinstitut"));
		}

		[Test]
		public void ParsePluralEntry()
		{
			const string po =
				"msgid \"apple\"\n"
				+ "msgid_plural \"apples\"\n"
				+ "msgstr[0] \"Apfel\"\n"
				+ "msgstr[1] \"Äpfel\"\n"
				+ "msgstr[2] \"viele Äpfel\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			var e = file.Entries[0];
			Assert.That(e.MsgId, Is.EqualTo("apple"));
			Assert.That(e.MsgIdPlural, Is.EqualTo("apples"));
			Assert.That(e.MsgStrForms, Is.Not.Null);
			Assert.That(e.MsgStrForms.Length, Is.EqualTo(3));
			Assert.That(e.MsgStrForms[0], Is.EqualTo("Apfel"));
			Assert.That(e.MsgStrForms[1], Is.EqualTo("Äpfel"));
			Assert.That(e.MsgStrForms[2], Is.EqualTo("viele Äpfel"));
		}

		[Test]
		public void ParseFuzzyEntry()
		{
			const string po = "#, fuzzy\nmsgid \"Hello\"\nmsgstr \"Hallo\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].IsFuzzy, Is.True);
			Assert.That(file.Entries[0].MsgStr, Is.EqualTo("Hallo"));
		}

		[Test]
		public void ParseObsoleteEntry()
		{
			const string po = "#~ msgid \"Old\"\n#~ msgstr \"Alt\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].IsObsolete, Is.True);
			Assert.That(file.Entries[0].MsgId, Is.EqualTo("Old"));
			Assert.That(file.Entries[0].MsgStr, Is.EqualTo("Alt"));
		}

		[Test]
		public void ParseTranslatorComment()
		{
			const string po = "#. translator note\nmsgid \"Hello\"\nmsgstr \"Hallo\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].TranslatorComments.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].TranslatorComments[0], Is.EqualTo("translator note"));
		}

		[Test]
		public void ParseSourceRef()
		{
			const string po = "#: file.cs:10\nmsgid \"Hello\"\nmsgstr \"Hallo\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].SourceReferences.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].SourceReferences[0], Is.EqualTo("file.cs:10"));
		}

		[Test]
		public void ParseMultilineString()
		{
			const string po =
				"msgid \"\"\n"
				+ "\"First \"\n"
				+ "\"Second\"\n"
				+ "msgstr \"\"\n"
				+ "\"Erste \"\n"
				+ "\"Zweite\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].MsgId, Is.EqualTo("First Second"));
			Assert.That(file.Entries[0].MsgStr, Is.EqualTo("Erste Zweite"));
		}

		[Test]
		public void ParseHeaderBlock()
		{
			const string po =
				"msgid \"\"\n"
				+ "msgstr \"\"\n"
				+ "\"Content-Type: text/plain; charset=UTF-8\\n\"\n"
				+ "\n"
				+ "msgid \"Hello\"\n"
				+ "msgstr \"Hallo\"\n";
			var file = PoFile.Parse(po);
			Assert.That(file.HasHeader, Is.True);
			Assert.That(file.Entries.Count, Is.EqualTo(1));
			Assert.That(file.Entries[0].MsgId, Is.EqualTo("Hello"));
		}

		[Test]
		public void RoundTrip()
		{
			const string po =
				"msgid \"\"\n"
				+ "msgstr \"\"\n"
				+ "\"Content-Type: text/plain; charset=UTF-8\\n\"\n"
				+ "\n"
				+ "msgid \"Hello\"\n"
				+ "msgstr \"Hallo\"\n"
				+ "\n"
				+ "#~ msgid \"Old\"\n"
				+ "#~ msgstr \"Alt\"\n"
				+ "\n";
			var file = PoFile.Parse(po);
			string serialized = file.Serialize();
			string normalizedOriginal = po.Replace("\r\n", "\n").TrimEnd();
			string normalizedSerialized = serialized.Replace("\r\n", "\n").TrimEnd();
			Assert.That(normalizedSerialized, Is.EqualTo(normalizedOriginal));
		}

		[Test]
		public void BuildLookup()
		{
			const string po =
				"msgid \"Hello\"\n"
				+ "msgstr \"Hallo\"\n"
				+ "\n"
				+ "msgid \"World\"\n"
				+ "msgstr \"Welt\"\n";
			var file = PoFile.Parse(po);
			var lookup = file.BuildLookup();
			Assert.That(lookup.ContainsKey("Hello"), Is.True);
			Assert.That(lookup["Hello"].MsgStr, Is.EqualTo("Hallo"));
			Assert.That(lookup.ContainsKey("World"), Is.True);
			Assert.That(lookup["World"].MsgStr, Is.EqualTo("Welt"));
		}

		[Test]
		public void BuildLookupWithContext()
		{
			const string po = "msgctxt \"finance\"\nmsgid \"Bank\"\nmsgstr \"Geldinstitut\"\n";
			var file = PoFile.Parse(po);
			var lookup = file.BuildLookup();
			string expectedKey = "finance\u0004Bank";
			Assert.That(lookup.ContainsKey(expectedKey), Is.True);
			Assert.That(lookup[expectedKey].MsgStr, Is.EqualTo("Geldinstitut"));
		}
	}
}
