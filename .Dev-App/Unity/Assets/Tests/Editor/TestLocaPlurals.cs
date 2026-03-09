using NUnit.Framework;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <see cref="LocaPlurals.GetPluralIdx"/>.
	/// Covers the null-guard in the non-generated part and the default-case
	/// (English fallback) in the generated part.
	/// </summary>
	public class TestLocaPlurals
	{
		// -------------------------------------------------------------------------
		// Null / empty language → non-generated guard in LocaPlurals.cs
		// -------------------------------------------------------------------------

		[Test]
		public void NullLanguage_ReturnsEnglishFallback()
		{
			var result = LocaPlurals.GetPluralIdx(null, 1);
			Assert.AreEqual(2, result.numPluralForms,
				"null language must fall back to English (2 plural forms)");
		}

		[Test]
		public void EmptyLanguage_ReturnsEnglishFallback()
		{
			var result = LocaPlurals.GetPluralIdx(string.Empty, 1);
			Assert.AreEqual(2, result.numPluralForms,
				"empty language must fall back to English (2 plural forms)");
		}

		// -------------------------------------------------------------------------
		// Unknown language → default case in generated switch
		// -------------------------------------------------------------------------

		[Test]
		public void UnknownLanguage_ReturnsEnglishFallback()
		{
			// "fr" is not present in the generated switch → default case applies
			var result = LocaPlurals.GetPluralIdx("fr", 2);
			Assert.AreEqual(2, result.numPluralForms,
				"unknown language must fall back to English (2 plural forms)");
		}

		[Test]
		public void UnknownLanguage_DoesNotReturnZeroForms()
		{
			// (0, 0) must never be returned for any language
			var result = LocaPlurals.GetPluralIdx("fr", 1);
			Assert.AreNotEqual(0, result.numPluralForms,
				"numPluralForms must never be 0 — (0,0) is an invalid fallback");
		}

		// -------------------------------------------------------------------------
		// Known languages
		// -------------------------------------------------------------------------

		[Test]
		public void KnownLanguage_De_ReturnsTwoForms()
		{
			var result = LocaPlurals.GetPluralIdx("de", 1);
			Assert.AreEqual(2, result.numPluralForms,
				"German has exactly 2 plural forms");
		}

		[Test]
		public void KnownLanguage_Ru_ReturnsThreeForms()
		{
			var result = LocaPlurals.GetPluralIdx("ru", 1);
			Assert.AreEqual(3, result.numPluralForms,
				"Russian has exactly 3 plural forms");
		}

		// -------------------------------------------------------------------------
		// Singular / plural index for the null-language English fallback
		// -------------------------------------------------------------------------

		[Test]
		public void EnglishFallback_SingularIsZero()
		{
			// n=1 → singular form → index 0
			var result = LocaPlurals.GetPluralIdx(null, 1);
			Assert.AreEqual(0, result.pluralIdx,
				"n=1 must return plural index 0 (singular form) for English fallback");
		}

		[Test]
		public void EnglishFallback_PluralIsOne()
		{
			// n=2 → plural form → index 1
			var result = LocaPlurals.GetPluralIdx(null, 2);
			Assert.AreEqual(1, result.pluralIdx,
				"n!=1 must return plural index 1 (plural form) for English fallback");
		}
	}
}
