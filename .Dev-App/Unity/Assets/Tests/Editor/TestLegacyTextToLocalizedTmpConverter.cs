using NUnit.Framework;
using UnityEngine;
using GuiToolkit.Editor;

namespace GuiToolkit.Test
{
	/// <summary>
	/// EditMode tests for <see cref="LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock"/>
	/// and <see cref="LegacyTextToLocalizedTmpConverter.EscapeYamlString"/>.
	///
	/// All tests are pure unit tests — no file I/O, no Unity scene, no import required.
	/// </summary>
	public class TestLegacyTextToLocalizedTmpConverter
	{
		private const string FakeScriptGuid = "aabbccddeeff00112233445566778899";
		private const long   FakeGoId       = 123456789L;
		private const long   FakeCompId     = 987654321L;

		// -----------------------------------------------------------------------
		// Minimal default data builder
		// -----------------------------------------------------------------------

		private static LegacyTextToLocalizedTmpConverter.LegacyTextData MakeData(
			string text            = "Hello",
			Color?  color          = null,
			float   fontSize       = 14f,
			bool    richText       = true,
			bool    autoSize       = false,
			float   autoMin        = 10f,
			float   autoMax        = 40f,
			TextAnchor alignment   = TextAnchor.MiddleCenter,
			bool    raycast        = true,
			bool    maskable       = true,
			bool    enabled        = true,
			float   lineSpacing    = 1f,
			FontStyle fontStyle    = FontStyle.Normal,
			HorizontalWrapMode hOverflow = HorizontalWrapMode.Wrap,
			VerticalWrapMode   vOverflow = VerticalWrapMode.Truncate,
			string  fontGuid       = "",
			long    fontLocalId    = 0,
			string  matGuid        = "",
			long    matLocalId     = 0)
		{
			return new LegacyTextToLocalizedTmpConverter.LegacyTextData
			{
				ComponentLocalId  = FakeCompId,
				GameObjectLocalId = FakeGoId,
				Text              = text,
				Color             = color ?? Color.white,
				FontSize          = fontSize,
				RichText          = richText,
				AutoSize          = autoSize,
				AutoSizeMin       = autoMin,
				AutoSizeMax       = autoMax,
				Alignment         = alignment,
				RaycastTarget     = raycast,
				Maskable          = maskable,
				Enabled           = enabled,
				LineSpacing       = lineSpacing,
				FontStyle         = fontStyle,
				HorizontalOverflow = hOverflow,
				VerticalOverflow   = vOverflow,
				FontAssetGuid     = fontGuid,
				FontAssetLocalId  = fontLocalId,
				MaterialGuid      = matGuid,
				MaterialLocalId   = matLocalId,
				};
		}

		// -----------------------------------------------------------------------
		// Block structure
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_StartsWithMonoBehaviour()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(MakeData(), FakeScriptGuid);
			Assert.IsTrue(block.StartsWith("MonoBehaviour:\n"),
				"Block must start with 'MonoBehaviour:\\n'");
		}

		[Test]
		public void BuildTmpBlock_ContainsCorrectScriptGuid()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(MakeData(), FakeScriptGuid);
			StringAssert.Contains($"guid: {FakeScriptGuid}, type: 3", block,
				"m_Script must reference the provided script GUID");
		}

		[Test]
		public void BuildTmpBlock_ContainsGameObjectRef()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(MakeData(), FakeScriptGuid);
			StringAssert.Contains($"m_GameObject: {{fileID: {FakeGoId}}}", block,
				"m_GameObject fileID must match GameObjectLocalId");
		}

		// -----------------------------------------------------------------------
		// Text content
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsTextContent()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(text: "My Label"), FakeScriptGuid);
			StringAssert.Contains("m_text: \"My Label\"", block);
		}

		[Test]
		public void BuildTmpBlock_EmptyText()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(text: ""), FakeScriptGuid);
			StringAssert.Contains("m_text: \"\"", block);
		}

		[Test]
		public void BuildTmpBlock_TextWithSpecialChars_EscapesCorrectly()
		{
			// Tab, newline and double-quote must all be escaped.
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(text: "a\"b\nc\td"), FakeScriptGuid);
			StringAssert.Contains("m_text: \"a\\\"b\\nc\\td\"", block);
		}

		// -----------------------------------------------------------------------
		// m_autoLocalize extras
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_ContainsAutoLocalizeFields()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(MakeData(), FakeScriptGuid);
			StringAssert.Contains("m_autoLocalize: 1", block);
			StringAssert.Contains("m_group: ", block);
			StringAssert.Contains("m_locaKey: ", block);
		}

		// -----------------------------------------------------------------------
		// Color
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsColor_White()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(color: Color.white), FakeScriptGuid);
			// rgba uint32 for white = 0xFFFFFFFF = 4294967295
			StringAssert.Contains("rgba: 4294967295", block);
		}

		[Test]
		public void BuildTmpBlock_MapsColor_Red()
		{
			// Red (1, 0, 0, 1) → r=255, g=0, b=0, a=255 → 0xFF0000FF → 4278190335
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(color: Color.red), FakeScriptGuid);
			// uint32: r | g<<8 | b<<16 | a<<24 = 255 | 0 | 0 | (255<<24) = 255 + 4278190080 = 4278190335
			StringAssert.Contains("rgba: 4278190335", block);
		}

		[Test]
		public void BuildTmpBlock_GraphicColorIsAlwaysWhite()
		{
			// m_Color (Graphic base) must always be white regardless of text color.
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(color: Color.red), FakeScriptGuid);
			StringAssert.Contains("m_Color: {r: 1, g: 1, b: 1, a: 1}", block);
		}

		// -----------------------------------------------------------------------
		// Font size
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsFontSize()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontSize: 24f), FakeScriptGuid);
			StringAssert.Contains("m_fontSize: 24", block);
			StringAssert.Contains("m_fontSizeBase: 24", block);
		}

		// -----------------------------------------------------------------------
		// Auto-size / best-fit
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsAutoSizeOff()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(autoSize: false, autoMin: 10f, autoMax: 40f), FakeScriptGuid);
			StringAssert.Contains("m_enableAutoSizing: 0", block);
			StringAssert.Contains("m_fontSizeMin: 10", block);
			StringAssert.Contains("m_fontSizeMax: 40", block);
		}

		[Test]
		public void BuildTmpBlock_MapsAutoSizeOn()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(autoSize: true, autoMin: 8f, autoMax: 72f), FakeScriptGuid);
			StringAssert.Contains("m_enableAutoSizing: 1", block);
		}

		// -----------------------------------------------------------------------
		// Alignment
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsAlignment_UpperLeft()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(alignment: TextAnchor.UpperLeft), FakeScriptGuid);
			StringAssert.Contains("m_HorizontalAlignment: 1", block);
			StringAssert.Contains("m_VerticalAlignment: 256", block);
		}

		[Test]
		public void BuildTmpBlock_MapsAlignment_MiddleCenter()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(alignment: TextAnchor.MiddleCenter), FakeScriptGuid);
			StringAssert.Contains("m_HorizontalAlignment: 2", block);
			StringAssert.Contains("m_VerticalAlignment: 512", block);
		}

		[Test]
		public void BuildTmpBlock_MapsAlignment_LowerRight()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(alignment: TextAnchor.LowerRight), FakeScriptGuid);
			StringAssert.Contains("m_HorizontalAlignment: 4", block);
			StringAssert.Contains("m_VerticalAlignment: 1024", block);
		}

		[Test]
		public void BuildTmpBlock_MapsAlignment_UpperCenter()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(alignment: TextAnchor.UpperCenter), FakeScriptGuid);
			StringAssert.Contains("m_HorizontalAlignment: 2", block);
			StringAssert.Contains("m_VerticalAlignment: 256", block);
		}

		[Test]
		public void BuildTmpBlock_MapsAlignment_MiddleRight()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(alignment: TextAnchor.MiddleRight), FakeScriptGuid);
			StringAssert.Contains("m_HorizontalAlignment: 4", block);
			StringAssert.Contains("m_VerticalAlignment: 512", block);
		}

		[Test]
		public void BuildTmpBlock_ContainsTextAlignmentLegacyField()
		{
			// m_textAlignment: 65535 must always be present (deprecated TMP field).
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(MakeData(), FakeScriptGuid);
			StringAssert.Contains("m_textAlignment: 65535", block);
		}

		// -----------------------------------------------------------------------
		// Word-wrap / overflow
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsWordWrap_WrapEnabled()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(hOverflow: HorizontalWrapMode.Wrap), FakeScriptGuid);
			StringAssert.Contains("m_enableWordWrapping: 1", block);
		}

		[Test]
		public void BuildTmpBlock_MapsWordWrap_WrapDisabled()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(hOverflow: HorizontalWrapMode.Overflow), FakeScriptGuid);
			StringAssert.Contains("m_enableWordWrapping: 0", block);
		}

		[Test]
		public void BuildTmpBlock_MapsVerticalOverflow_Truncate()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(vOverflow: VerticalWrapMode.Truncate), FakeScriptGuid);
			StringAssert.Contains("m_overflowMode: 0", block);
		}

		[Test]
		public void BuildTmpBlock_MapsVerticalOverflow_Overflow()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(vOverflow: VerticalWrapMode.Overflow), FakeScriptGuid);
			StringAssert.Contains("m_overflowMode: 1", block);
		}

		// -----------------------------------------------------------------------
		// RichText
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsRichTextOn()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(richText: true), FakeScriptGuid);
			StringAssert.Contains("m_isRichText: 1", block);
		}

		[Test]
		public void BuildTmpBlock_MapsRichTextOff()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(richText: false), FakeScriptGuid);
			StringAssert.Contains("m_isRichText: 0", block);
		}

		// -----------------------------------------------------------------------
		// RaycastTarget & Maskable
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsRaycastTargetOn()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(raycast: true), FakeScriptGuid);
			StringAssert.Contains("m_RaycastTarget: 1", block);
		}

		[Test]
		public void BuildTmpBlock_MapsRaycastTargetOff()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(raycast: false), FakeScriptGuid);
			StringAssert.Contains("m_RaycastTarget: 0", block);
		}

		[Test]
		public void BuildTmpBlock_MapsMaskableOn()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(maskable: true), FakeScriptGuid);
			StringAssert.Contains("m_Maskable: 1", block);
		}

		[Test]
		public void BuildTmpBlock_MapsMaskableOff()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(maskable: false), FakeScriptGuid);
			StringAssert.Contains("m_Maskable: 0", block);
		}

		// -----------------------------------------------------------------------
		// Enabled
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsEnabledOn()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(enabled: true), FakeScriptGuid);
			StringAssert.Contains("m_Enabled: 1", block);
		}

		[Test]
		public void BuildTmpBlock_MapsEnabledOff()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(enabled: false), FakeScriptGuid);
			StringAssert.Contains("m_Enabled: 0", block);
		}

		// -----------------------------------------------------------------------
		// Font style
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_MapsFontStyle_Normal()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontStyle: FontStyle.Normal), FakeScriptGuid);
			StringAssert.Contains("m_fontStyle: 0", block);
		}

		[Test]
		public void BuildTmpBlock_MapsFontStyle_Bold()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontStyle: FontStyle.Bold), FakeScriptGuid);
			StringAssert.Contains("m_fontStyle: 1", block);
		}

		[Test]
		public void BuildTmpBlock_MapsFontStyle_Italic()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontStyle: FontStyle.Italic), FakeScriptGuid);
			StringAssert.Contains("m_fontStyle: 2", block);
		}

		[Test]
		public void BuildTmpBlock_MapsFontStyle_BoldItalic()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontStyle: FontStyle.BoldAndItalic), FakeScriptGuid);
			StringAssert.Contains("m_fontStyle: 3", block);
		}

		// -----------------------------------------------------------------------
		// Font asset references
		// -----------------------------------------------------------------------

		[Test]
		public void BuildTmpBlock_NoFont_UsesFileId0()
		{
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontGuid: "", fontLocalId: 0), FakeScriptGuid);
			StringAssert.Contains("m_fontAsset: {fileID: 0}", block);
			StringAssert.Contains("m_sharedMaterial: {fileID: 0}", block);
		}

		[Test]
		public void BuildTmpBlock_WithFont_ContainsFontGuid()
		{
			const string fontGuid = "11223344556677889900aabbccddeeff";
			string block = LegacyTextToLocalizedTmpConverter.BuildTmpYamlBlock(
				MakeData(fontGuid: fontGuid, fontLocalId: 11400000, matGuid: fontGuid, matLocalId: 1234567L),
				FakeScriptGuid);
			StringAssert.Contains($"guid: {fontGuid}", block);
			StringAssert.Contains("fileID: 11400000", block);
		}

		// -----------------------------------------------------------------------
		// EscapeYamlString
		// -----------------------------------------------------------------------

		[Test]
		public void EscapeYamlString_Empty_ReturnsEmpty()
		{
			Assert.AreEqual("", LegacyTextToLocalizedTmpConverter.EscapeYamlString(""));
		}

		[Test]
		public void EscapeYamlString_PlainText_Unchanged()
		{
			Assert.AreEqual("Hello World", LegacyTextToLocalizedTmpConverter.EscapeYamlString("Hello World"));
		}

		[Test]
		public void EscapeYamlString_DoubleQuote_Escaped()
		{
			Assert.AreEqual("a\\\"b", LegacyTextToLocalizedTmpConverter.EscapeYamlString("a\"b"));
		}

		[Test]
		public void EscapeYamlString_Backslash_Escaped()
		{
			Assert.AreEqual("a\\\\b", LegacyTextToLocalizedTmpConverter.EscapeYamlString("a\\b"));
		}

		[Test]
		public void EscapeYamlString_Newline_Escaped()
		{
			Assert.AreEqual("a\\nb", LegacyTextToLocalizedTmpConverter.EscapeYamlString("a\nb"));
		}

		[Test]
		public void EscapeYamlString_Tab_Escaped()
		{
			Assert.AreEqual("a\\tb", LegacyTextToLocalizedTmpConverter.EscapeYamlString("a\tb"));
		}
	}
}
