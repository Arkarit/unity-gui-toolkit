using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <see cref="LocaManager.GetSupportedIsoCode"/> and <see cref="LocaManager.GetBaseLanguage"/>.
	///
	/// All resolution tests use the explicit-list overload of <c>GetSupportedIsoCode</c>, so they exercise the
	/// pure resolution logic without touching the generated <c>uitk_available_languages</c> resource or any
	/// global state.
	/// </summary>
	public class TestLocaSystemLanguage
	{
		// -------------------------------------------------------------------------
		// GetBaseLanguage
		// -------------------------------------------------------------------------

		[Test]
		public void GetBaseLanguage_StripsRegionSubtag()
		{
			Assert.AreEqual("pt", LocaManager.GetBaseLanguage("pt-br"));
			Assert.AreEqual("zh", LocaManager.GetBaseLanguage("zh-cn"));
			Assert.AreEqual("en", LocaManager.GetBaseLanguage("en"));
		}

		// -------------------------------------------------------------------------
		// Basic mapping and the "single shipped variant" case
		// -------------------------------------------------------------------------

		[Test]
		public void SingleVariant_ReturnsThatVariant()
		{
			// We ship only pt-br, no plain "pt": Portuguese devices must land on pt-br, not English.
			string result = LocaManager.GetSupportedIsoCode(
				SystemLanguage.Portuguese, new[] { "en", "de", "pt-br" });

			Assert.AreEqual("pt-br", result);
		}

		[Test]
		public void MainLanguageOnly_ReturnsMain()
		{
			Assert.AreEqual("de",
				LocaManager.GetSupportedIsoCode(SystemLanguage.German, new[] { "en", "de" }));
		}

		[Test]
		public void Norwegian_MapsToNo()
		{
			Assert.AreEqual("no",
				LocaManager.GetSupportedIsoCode(SystemLanguage.Norwegian, new[] { "en", "no", "sv" }));
		}

		[Test]
		public void English_ResolvesToEnglish()
		{
			Assert.AreEqual("en",
				LocaManager.GetSupportedIsoCode(SystemLanguage.English, new[] { "en", "de" }));
		}

		// -------------------------------------------------------------------------
		// Main + variants -> main
		// -------------------------------------------------------------------------

		[Test]
		public void MainPlusVariant_ReturnsMain()
		{
			Assert.AreEqual("pt",
				LocaManager.GetSupportedIsoCode(SystemLanguage.Portuguese, new[] { "pt", "pt-br" }));
		}

		// -------------------------------------------------------------------------
		// Region-specific system language (e.g. ChineseSimplified -> zh-cn)
		// -------------------------------------------------------------------------

		[Test]
		public void RegionSpecificSystemLanguage_PrefersExactMatch()
		{
			// ChineseSimplified maps to "zh-cn"; when that exact variant is shipped it must win over "zh".
			Assert.AreEqual("zh-cn",
				LocaManager.GetSupportedIsoCode(SystemLanguage.ChineseSimplified, new[] { "zh", "zh-cn" }));
		}

		[Test]
		public void RegionSpecificSystemLanguage_FallsBackToMain()
		{
			// Only the generic "zh" is shipped: ChineseSimplified falls back to it.
			Assert.AreEqual("zh",
				LocaManager.GetSupportedIsoCode(SystemLanguage.ChineseSimplified, new[] { "zh" }));
		}

		[Test]
		public void GenericChineseWithMainAndVariants_ReturnsMain()
		{
			Assert.AreEqual("zh",
				LocaManager.GetSupportedIsoCode(SystemLanguage.Chinese, new[] { "zh", "zh-cn", "zh-tw" }));
		}

		// -------------------------------------------------------------------------
		// Ambiguous: several variants, no main language
		// -------------------------------------------------------------------------

		[Test]
		public void AmbiguousVariants_ReturnsOneOfThem()
		{
			// Generic Chinese ("zh") with only region variants available and no main "zh".
			var available = new[] { "zh-cn", "zh-tw" };
			string result = LocaManager.GetSupportedIsoCode(SystemLanguage.Chinese, available);

			CollectionAssert.Contains(available, result,
				"An ambiguous resolution must still return one of the available variants.");
		}

		[Test]
		public void AmbiguousVariants_WithThrow_Throws()
		{
			Assert.Throws<InvalidOperationException>(() =>
				LocaManager.GetSupportedIsoCode(
					SystemLanguage.Chinese, new[] { "zh-cn", "zh-tw" }, EErrorHandling.Throw));
		}

		[Test]
		public void AmbiguousVariants_ResolvedByPreferredSubtype()
		{
			// Providing a preferred variant removes the ambiguity and makes the result deterministic.
			var preferred = new Dictionary<string, string> { { "zh", "zh-tw" } };
			string result = LocaManager.GetSupportedIsoCode(
				SystemLanguage.Chinese, new[] { "zh-cn", "zh-tw" }, EErrorHandling.Throw, preferred);

			Assert.AreEqual("zh-tw", result);
		}

		// -------------------------------------------------------------------------
		// Preferred subtype
		// -------------------------------------------------------------------------

		[Test]
		public void PreferredSubtype_WinsOverMainLanguage()
		{
			var preferred = new Dictionary<string, string> { { "pt", "pt-br" } };
			Assert.AreEqual("pt-br",
				LocaManager.GetSupportedIsoCode(
					SystemLanguage.Portuguese, new[] { "pt", "pt-br" }, EErrorHandling.None, preferred));
		}

		[Test]
		public void PreferredSubtype_Unavailable_FallsThroughToNormalResolution()
		{
			// pt-pt is preferred but not shipped -> falls through and the main language wins.
			var preferred = new Dictionary<string, string> { { "pt", "pt-pt" } };
			Assert.AreEqual("pt",
				LocaManager.GetSupportedIsoCode(
					SystemLanguage.Portuguese, new[] { "pt", "pt-br" }, EErrorHandling.None, preferred));
		}

		// -------------------------------------------------------------------------
		// Not shipped / no mapping -> "en"
		// -------------------------------------------------------------------------

		[Test]
		public void LanguageNotShipped_FallsBackToEnglish()
		{
			Assert.AreEqual("en",
				LocaManager.GetSupportedIsoCode(SystemLanguage.Portuguese, new[] { "en", "de" }));
		}

		[Test]
		public void LanguageNotShipped_WithThrow_Throws()
		{
			Assert.Throws<InvalidOperationException>(() =>
				LocaManager.GetSupportedIsoCode(
					SystemLanguage.Portuguese, new[] { "en", "de" }, EErrorHandling.Throw));
		}

		[Test]
		public void UnmappedSystemLanguage_FallsBackToEnglish()
		{
			Assert.AreEqual("en",
				LocaManager.GetSupportedIsoCode(SystemLanguage.Unknown, new[] { "en", "de" }));
		}

		[Test]
		public void UnmappedSystemLanguage_WithThrow_Throws()
		{
			Assert.Throws<InvalidOperationException>(() =>
				LocaManager.GetSupportedIsoCode(
					SystemLanguage.Unknown, new[] { "en", "de" }, EErrorHandling.Throw));
		}

		// -------------------------------------------------------------------------
		// Normalization of the available-language input
		// -------------------------------------------------------------------------

		[Test]
		public void AvailableLanguages_AreNormalizedBeforeMatching()
		{
			// Non-canonical input ("PT_BR") must still be recognized as pt-br.
			Assert.AreEqual("pt-br",
				LocaManager.GetSupportedIsoCode(SystemLanguage.Portuguese, new[] { "PT_BR" }));
		}

		[Test]
		public void EmptyAvailableList_FallsBackToEnglish()
		{
			Assert.AreEqual("en",
				LocaManager.GetSupportedIsoCode(SystemLanguage.German, Array.Empty<string>()));
		}
	}
}
