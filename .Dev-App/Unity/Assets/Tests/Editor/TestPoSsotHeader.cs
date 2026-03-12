using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	public class TestPoSsotHeader
	{
		private string m_tempFile;

		[TearDown]
		public void TearDown()
		{
			if (m_tempFile != null && File.Exists(m_tempFile))
				File.Delete(m_tempFile);
			m_tempFile = null;
		}

		private string WriteTempFile(string _content)
		{
			m_tempFile = Path.Combine(Path.GetTempPath(), $"uitk_ssot_test_{Guid.NewGuid():N}.po");
			File.WriteAllText(m_tempFile, _content, Encoding.UTF8);
			return m_tempFile;
		}

		[Test]
		public void DetectSsotHeader()
		{
			string path = WriteTempFile(
				"# Generated from Spreadsheet SSoT\n"
				+ "# Bridge: TestBridge (GUID: abc)\n"
				+ "# Source: https://example.com\n"
				+ "# Generated: 2024-01-15T10:00:00.0000000Z\n"
				+ "# DO NOT EDIT MANUALLY \u2014 Changes will be overwritten. Use the linked spreadsheet or \"Make Local Copy\" to detach.\n"
				+ "msgid \"\"\nmsgstr \"\"\n");
			Assert.That(PoSsotHeader.HasSsotHeader(path), Is.True);
		}

		[Test]
		public void DetectNoSsotHeader()
		{
			string path = WriteTempFile("msgid \"Hello\"\nmsgstr \"Hallo\"\n");
			Assert.That(PoSsotHeader.HasSsotHeader(path), Is.False);
		}

		[Test]
		public void ParseHeaderInfo()
		{
			string path = WriteTempFile(
				"# Generated from Spreadsheet SSoT\n"
				+ "# Bridge: MyBridge (GUID: deadbeef-1234)\n"
				+ "# Source: https://docs.google.com/spreadsheets/d/XYZ\n"
				+ "# Generated: 2024-06-01T12:00:00.0000000Z\n"
				+ "# DO NOT EDIT MANUALLY \u2014 Changes will be overwritten. Use the linked spreadsheet or \"Make Local Copy\" to detach.\n"
				+ "msgid \"\"\nmsgstr \"\"\n");
			PoSsotInfo info = PoSsotHeader.ParseHeader(path);
			Assert.That(info, Is.Not.Null);
			Assert.That(info.BridgeName, Is.EqualTo("MyBridge"));
			Assert.That(info.BridgeGuid, Is.EqualTo("deadbeef-1234"));
			Assert.That(info.SourceUrl, Is.EqualTo("https://docs.google.com/spreadsheets/d/XYZ"));
			Assert.That(info.GeneratedAt, Is.Not.EqualTo(default(DateTime)));
		}

		[Test]
		public void GenerateHeaderLines()
		{
			var lines = PoSsotHeader.GenerateHeaderLines("MyBridge", "guid123", "https://example.com/sheet");
			Assert.That(lines.Count, Is.EqualTo(5));
			Assert.That(lines[0], Is.EqualTo("# Generated from Spreadsheet SSoT"));
			Assert.That(lines[1], Does.Contain("MyBridge"));
			Assert.That(lines[1], Does.Contain("guid123"));
			Assert.That(lines[2], Is.EqualTo("# Source: https://example.com/sheet"));
			Assert.That(lines[3], Does.StartWith("# Generated: "));
			Assert.That(lines[4], Does.Contain("DO NOT EDIT MANUALLY"));
		}

		[Test]
		public void StripSsotHeader()
		{
			var lines = new List<string>
			{
				"# Generated from Spreadsheet SSoT",
				"# Bridge: SomeBridge (GUID: abc)",
				"# Source: https://example.com",
				"# Generated: 2024-01-01T00:00:00.0000000Z",
				"# DO NOT EDIT MANUALLY \u2014 Changes will be overwritten. Use the linked spreadsheet or \"Make Local Copy\" to detach.",
				"# custom comment that should remain",
			};
			var stripped = PoSsotHeader.StripSsotLines(lines);
			Assert.That(stripped.Count, Is.EqualTo(1));
			Assert.That(stripped[0], Is.EqualTo("# custom comment that should remain"));
		}
	}
}
