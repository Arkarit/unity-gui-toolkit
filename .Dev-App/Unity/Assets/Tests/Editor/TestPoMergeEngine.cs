using NUnit.Framework;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	public class TestPoMergeEngine
	{
		private static PoFile ParsePo(string _content) => PoFile.Parse(_content);

		[Test]
		public void MergeAddsNewKeys()
		{
			var po  = ParsePo("msgid \"Existing\"\nmsgstr \"Vorhanden\"\n");
			var pot = ParsePo("msgid \"Existing\"\nmsgstr \"\"\n\nmsgid \"New\"\nmsgstr \"\"\n");
			var (merged, _) = PoMergeEngine.Merge(po, pot);
			var lookup = merged.BuildLookup();
			Assert.That(lookup.ContainsKey("New"), Is.True);
			Assert.That(lookup["New"].MsgStr, Is.EqualTo(string.Empty));
		}

		[Test]
		public void MergePreservesTranslation()
		{
			var po  = ParsePo("msgid \"Hello\"\nmsgstr \"Hallo\"\n");
			var pot = ParsePo("msgid \"Hello\"\nmsgstr \"\"\n");
			var (merged, _) = PoMergeEngine.Merge(po, pot);
			var lookup = merged.BuildLookup();
			Assert.That(lookup["Hello"].MsgStr, Is.EqualTo("Hallo"));
		}

		[Test]
		public void MergeObsoletesRemovedKey()
		{
			var po  = ParsePo("msgid \"A\"\nmsgstr \"Alpha\"\n\nmsgid \"B\"\nmsgstr \"Beta\"\n");
			var pot = ParsePo("msgid \"A\"\nmsgstr \"\"\n");
			var (merged, _) = PoMergeEngine.Merge(po, pot);
			bool obsoleteFound = false;
			foreach (var entry in merged.Entries)
			{
				if (entry.MsgId == "B" && entry.IsObsolete)
					obsoleteFound = true;
			}
			Assert.That(obsoleteFound, Is.True);
		}

		[Test]
		public void MergeObsoleteSkipWhenDisabled()
		{
			var po  = ParsePo("msgid \"A\"\nmsgstr \"Alpha\"\n\nmsgid \"B\"\nmsgstr \"Beta\"\n");
			var pot = ParsePo("msgid \"A\"\nmsgstr \"\"\n");
			var (merged, _) = PoMergeEngine.Merge(po, pot, _markObsolete: false);
			bool bFound = false;
			foreach (var entry in merged.Entries)
			{
				if (entry.MsgId == "B")
					bFound = true;
			}
			Assert.That(bFound, Is.False);
		}

		[Test]
		public void MergePreservesOrder()
		{
			var po  = ParsePo("msgid \"A\"\nmsgstr \"Alpha\"\n\nmsgid \"B\"\nmsgstr \"Beta\"\n\nmsgid \"C\"\nmsgstr \"Gamma\"\n");
			var pot = ParsePo("msgid \"C\"\nmsgstr \"\"\n\nmsgid \"A\"\nmsgstr \"\"\n\nmsgid \"B\"\nmsgstr \"\"\n");
			var (merged, _) = PoMergeEngine.Merge(po, pot);
			Assert.That(merged.Entries[0].MsgId, Is.EqualTo("C"));
			Assert.That(merged.Entries[1].MsgId, Is.EqualTo("A"));
			Assert.That(merged.Entries[2].MsgId, Is.EqualTo("B"));
		}

		[Test]
		public void MergeResultStats()
		{
			var po  = ParsePo("msgid \"Keep\"\nmsgstr \"Behalten\"\n\nmsgid \"Remove\"\nmsgstr \"Entfernen\"\n");
			var pot = ParsePo("msgid \"Keep\"\nmsgstr \"\"\n\nmsgid \"Added\"\nmsgstr \"\"\n");
			var (_, stats) = PoMergeEngine.Merge(po, pot);
			Assert.That(stats.PreservedKeys, Is.EqualTo(1));
			Assert.That(stats.AddedKeys, Is.EqualTo(1));
			Assert.That(stats.ObsoleteKeys, Is.EqualTo(1));
		}

		[Test]
		public void MergePluralEntry()
		{
			var po  = ParsePo("msgid \"apple\"\nmsgid_plural \"apples\"\nmsgstr[0] \"Apfel\"\nmsgstr[1] \"Äpfel\"\n");
			var pot = ParsePo("msgid \"apple\"\nmsgid_plural \"apples\"\nmsgstr[0] \"\"\nmsgstr[1] \"\"\n");
			var (merged, _) = PoMergeEngine.Merge(po, pot);
			Assert.That(merged.Entries.Count, Is.EqualTo(1));
			var entry = merged.Entries[0];
			Assert.That(entry.MsgStrForms, Is.Not.Null);
			Assert.That(entry.MsgStrForms[0], Is.EqualTo("Apfel"));
			Assert.That(entry.MsgStrForms[1], Is.EqualTo("Äpfel"));
		}

		[Test]
		public void MergeWithContext()
		{
			var po  = ParsePo("msgctxt \"verb\"\nmsgid \"Open\"\nmsgstr \"Öffnen\"\n");
			var pot = ParsePo(
				"msgctxt \"verb\"\nmsgid \"Open\"\nmsgstr \"\"\n"
				+ "\n"
				+ "msgctxt \"adjective\"\nmsgid \"Open\"\nmsgstr \"\"\n");
			var (merged, stats) = PoMergeEngine.Merge(po, pot);
			Assert.That(stats.PreservedKeys, Is.EqualTo(1));
			Assert.That(stats.AddedKeys, Is.EqualTo(1));
			var lookup = merged.BuildLookup();
			Assert.That(lookup["verb\u0004Open"].MsgStr, Is.EqualTo("Öffnen"));
			Assert.That(lookup["adjective\u0004Open"].MsgStr, Is.EqualTo(string.Empty));
		}

		[Test]
		public void MergeEmptyPo()
		{
			var po  = ParsePo(string.Empty);
			var pot = ParsePo("msgid \"A\"\nmsgstr \"\"\n\nmsgid \"B\"\nmsgstr \"\"\n");
			var (merged, stats) = PoMergeEngine.Merge(po, pot);
			Assert.That(stats.AddedKeys, Is.EqualTo(2));
			Assert.That(stats.PreservedKeys, Is.EqualTo(0));
			var lookup = merged.BuildLookup();
			Assert.That(lookup["A"].MsgStr, Is.EqualTo(string.Empty));
			Assert.That(lookup["B"].MsgStr, Is.EqualTo(string.Empty));
		}

		[Test]
		public void MergeEmptyPot()
		{
			var po  = ParsePo("msgid \"A\"\nmsgstr \"Alpha\"\n\nmsgid \"B\"\nmsgstr \"Beta\"\n");
			var pot = ParsePo(string.Empty);
			var (merged, stats) = PoMergeEngine.Merge(po, pot);
			Assert.That(stats.ObsoleteKeys, Is.EqualTo(2));
			int obsoleteCount = 0;
			foreach (var entry in merged.Entries)
			{
				if (entry.IsObsolete)
					obsoleteCount++;
			}
			Assert.That(obsoleteCount, Is.EqualTo(2));
		}
	}
}
