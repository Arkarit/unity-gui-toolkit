using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the drift analysis - the half of the conversion tool that only looks.
	///
	/// Its whole worth is that its numbers can be trusted: they are what the decision to convert an existing
	/// clone is made on. A comparison that quietly counts a copy as a difference would recommend keeping
	/// dozens of overrides that carry nothing, and one that counts a difference as a copy would throw away
	/// somebody's work.
	/// </summary>
	[EditorAware]
	public class TestUiStyleDrift
	{
		private const string SkinDefault = "Default";
		private const string SkinLight = "Light";
		private const string SkinProject = "BOTW";
		private const string StyleName = "Buttons/Standard/Background";

		private readonly List<Object> m_created = new();

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_created)
			{
				if (obj != null)
					Object.DestroyImmediate(obj);
			}

			m_created.Clear();
		}

		[Test]
		public void AStyleWithTheSameValues_IsIdentical()
		{
			var (child, parent) = CreatePair();
			AddImage(child, SkinDefault, StyleName, Color.red);
			AddImage(parent, SkinDefault, StyleName, Color.red);

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);

			Assert.AreEqual(1, drift.Count(EStyleDriftState.Identical));
			Assert.AreEqual(0, drift.Count(EStyleDriftState.Differs));
			Assert.IsEmpty(drift.Skins[0].Styles[0].Values);
		}

		/// <summary>
		/// A difference has to name the value, not its index: "value 1 differs" is not something anybody can
		/// act on, and the names only exist on the fields holding them.
		/// </summary>
		[Test]
		public void ADifferingValue_IsNamedWithBothSides()
		{
			var (child, parent) = CreatePair();
			AddImage(child, SkinDefault, StyleName, Color.red);
			AddImage(parent, SkinDefault, StyleName, Color.green);

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);
			var style = drift.Skins[0].Styles[0];

			Assert.AreEqual(EStyleDriftState.Differs, style.State);
			Assert.AreEqual(1, style.Values.Count);
			Assert.AreEqual("Color", style.Values[0].Name);
			StringAssert.Contains("FF0000", style.Values[0].Here.ToUpperInvariant());
			StringAssert.Contains("00FF00", style.Values[0].There.ToUpperInvariant());
		}

		/// <summary>
		/// A value that is switched off is not part of the style, so whatever it still holds underneath is not
		/// a difference - otherwise every leftover from an earlier decision would become a fake override.
		/// </summary>
		[Test]
		public void AnUnusedValue_IsNotADifference()
		{
			var (child, parent) = CreatePair();
			var here = AddImage(child, SkinDefault, StyleName, Color.red);
			var there = AddImage(parent, SkinDefault, StyleName, Color.green);
			here.Color.IsApplicable = false;
			there.Color.IsApplicable = false;

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);

			Assert.AreEqual(EStyleDriftState.Identical, drift.Skins[0].Styles[0].State);
		}

		/// <summary>
		/// Being used on one side and not on the other IS a difference, and a bigger one than a value - the
		/// style applies something the other does not touch at all.
		/// </summary>
		[Test]
		public void AValueUsedOnOneSideOnly_IsADifference()
		{
			var (child, parent) = CreatePair();
			var here = AddImage(child, SkinDefault, StyleName, Color.red);
			AddImage(parent, SkinDefault, StyleName, Color.red);
			here.Enabled.IsApplicable = true;

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);
			var style = drift.Skins[0].Styles[0];

			Assert.AreEqual(EStyleDriftState.Differs, style.State);
			Assert.AreEqual("Enabled", style.Values[0].Name);
			Assert.AreEqual("used", style.Values[0].Here);
			Assert.AreEqual("unused", style.Values[0].There);
		}

		[Test]
		public void AStyleOnOneSideOnly_IsCountedOnThatSide()
		{
			var (child, parent) = CreatePair();
			AddImage(child, SkinDefault, "Only/Here", Color.red);
			AddImage(parent, SkinDefault, "Only/There", Color.red);

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);

			Assert.AreEqual(1, drift.Count(EStyleDriftState.OnlyHere));
			Assert.AreEqual(1, drift.Count(EStyleDriftState.OnlyThere));
			Assert.AreEqual(0, drift.ComparedStyles);
		}

		/// <summary>
		/// The case the client is actually in: its skins are Default and BOTW, the package's are Default and
		/// Light. The unmatched skin must be reported as inheriting nothing rather than silently compared
		/// against the wrong one - that number is the whole point of running this before converting.
		/// </summary>
		[Test]
		public void AnUnmatchedSkin_IsReportedAsInheritingNothing()
		{
			var child = CreateConfig("Client", SkinDefault, SkinProject);
			var parent = CreateConfig("Package", SkinDefault, SkinLight);
			AddImage(child, SkinProject, StyleName, Color.red);
			AddImage(parent, SkinLight, StyleName, Color.red);

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);
			var projectSkin = drift.Skins[1];

			Assert.AreEqual(SkinProject, projectSkin.SkinName);
			Assert.IsNull(projectSkin.OtherSkinName, "nothing to inherit from, and that has to show");
			Assert.AreEqual(1, projectSkin.Count(EStyleDriftState.OnlyHere));
			Assert.Contains(SkinLight, drift.UnusedOtherSkins, "and the skin nothing maps to is named");
		}

		/// <summary>
		/// With a mapping set, the comparison has to follow it - the report is meant to show what a conversion
		/// would really do, and a conversion follows the mapping.
		/// </summary>
		[Test]
		public void AMappedSkin_IsComparedAgainstWhatItMapsTo()
		{
			var child = CreateConfig("Client", SkinDefault, SkinProject);
			var parent = CreateConfig("Package", SkinDefault, SkinLight);
			child.GetOwnSkinByNameOrAlias(SkinProject, false).InheritFromSkinName = SkinLight;
			AddImage(child, SkinProject, StyleName, Color.red);
			AddImage(parent, SkinLight, StyleName, Color.red);

			var drift = UiStyleDriftAnalyzer.Analyze(child, parent);

			Assert.AreEqual(SkinLight, drift.Skins[1].OtherSkinName);
			Assert.AreEqual(1, drift.Skins[1].Count(EStyleDriftState.Identical));
			Assert.IsEmpty(drift.UnusedOtherSkins);
		}

		/// <summary>
		/// The same report answers two different questions, so it has to know which one it is answering: for a
		/// config that already inherits, an identical style is a pointless override, not a copy to be dropped.
		/// </summary>
		[Test]
		public void AnExistingInheritance_IsRecognised()
		{
			var (child, parent) = CreatePair();
			Assert.IsFalse(UiStyleDriftAnalyzer.Analyze(child, parent).AlreadyInherits);

			child.Parent = parent;
			Assert.IsTrue(UiStyleDriftAnalyzer.Analyze(child, parent).AlreadyInherits);
		}

		[Test]
		public void WithoutBothSides_NothingIsClaimed()
		{
			var config = CreateConfig("Child", SkinDefault);

			Assert.AreEqual(0, UiStyleDriftAnalyzer.Analyze(config, null).Skins.Count);
			Assert.AreEqual(0, UiStyleDriftAnalyzer.Analyze(null, config).Skins.Count);
		}

		/// <summary>
		/// The text is what a person reads, so the two states that carry no decision stay out of the detail:
		/// with 70 inherited styles, listing them buries the three lines that matter.
		/// </summary>
		[Test]
		public void TheReportText_ListsOnlyWhatCarriesADecision()
		{
			var (child, parent) = CreatePair();
			AddImage(child, SkinDefault, "Same/Everywhere", Color.red);
			AddImage(parent, SkinDefault, "Same/Everywhere", Color.red);
			AddImage(parent, SkinDefault, "Only/There", Color.red);
			AddImage(child, SkinDefault, "Only/Here", Color.red);

			var text = UiStyleDriftAnalyzer.Analyze(child, parent).ToText();

			StringAssert.Contains("Only/Here", text);
			StringAssert.DoesNotContain("Only/There", text);
			StringAssert.DoesNotContain("Same/Everywhere", text);
			StringAssert.Contains("1 identical", text, "counted, though");
		}

		// ------------------------------------------------------- telling the two Unity nulls apart

		/// <summary>
		/// There are two kinds of Unity null in a style value, and they mean opposite things: nothing was
		/// ever assigned, or something was and is gone. The instance id is what tells them apart, and getting
		/// it wrong sent me hunting for a broken asset that never existed - a value serialized as
		/// {fileID: 0} inside a [SerializeReference] block comes back as a wrapper, not as plain null.
		/// </summary>
		[Test]
		public void AnEmptyValue_IsNotCalledMissing()
		{
			Assert.AreEqual("<none>", UiStyleDriftAnalyzer.DescribeValue(null));
		}

		[Test]
		public void AReferenceWhoseTargetIsGone_IsCalledMissing()
		{
			var doomed = ScriptableObject.CreateInstance<UiStyleConfig>();
			doomed.name = "Doomed";
			object reference = doomed;

			Assert.AreEqual("Doomed (UiStyleConfig)", UiStyleDriftAnalyzer.DescribeValue(reference));

			Object.DestroyImmediate(doomed);

			Assert.AreEqual("<missing>", UiStyleDriftAnalyzer.DescribeValue(reference),
				"it kept its instance id, so something WAS there");
		}

		// ------------------------------------------------------------------------------- fixtures

		private (UiStyleConfig child, UiStyleConfig parent) CreatePair()
			=> (CreateConfig("Child", SkinDefault), CreateConfig("Parent", SkinDefault));

		private UiStyleConfig CreateConfig( string _name, params string[] _skinNames )
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = _name;
			m_created.Add(config);

			var skins = new List<UiSkin>();
			foreach (var skinName in _skinNames)
				skins.Add(new UiSkin(config, skinName));

			config.Skins = skins;
			return config;
		}

		private static UiStyleImage AddImage( UiStyleConfig _config, string _skinName, string _styleName, Color _color )
		{
			var style = new UiStyleImage(_config, _styleName);
			style.Color.IsApplicable = true;
			style.Color.RawValue = _color;
			_config.GetOwnSkinByNameOrAlias(_skinName, false).Styles.Add(style);
			return style;
		}
	}
}
