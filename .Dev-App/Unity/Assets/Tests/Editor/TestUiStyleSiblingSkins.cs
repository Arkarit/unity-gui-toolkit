using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for a skin that builds on another skin of the SAME config.
	///
	/// It exists because that is what a skin usually is. Measured on the client: its BOTW skin shares 50 of
	/// 80 styles with its own Default and only 44 with the package's, and the two own skins hold exactly the
	/// same set of styles - which the package's do not. Overrides against the sibling then also say something
	/// true: "differs from our own look", rather than "differs from the library's".
	///
	/// The dangerous part is not the resolution but the writing: an inherited style now belongs to the very
	/// config being edited, so anything that decides "own or inherited" by comparing CONFIGS would call it
	/// own and write into the sibling - changing the look everybody sees instead of the one being edited.
	/// </summary>
	[EditorAware]
	public class TestUiStyleSiblingSkins
	{
		private const string SkinBase = "Default";
		private const string SkinVariant = "Variant";
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
		public void ASkinBuildingOnASibling_ResolvesItsStyles()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			var style = AddImage(config, SkinBase, StyleName, Color.red);
			MakeVariantBuildOnBase(config);

			var variant = Skin(config, SkinVariant);

			Assert.AreSame(Skin(config, SkinBase), variant.ParentSkin);
			Assert.AreSame(style, variant.StyleByKey(style.Key));
			Assert.IsFalse(variant.OwnsStyle(style.Key), "it resolves it, it does not hold it");
			Assert.AreEqual(1, variant.EffectiveStyles.Count);
		}

		/// <summary>
		/// The default has to keep meaning what it meant: same name, in the PARENT config. Otherwise every
		/// existing skin would suddenly start building on a sibling of the same name - or on itself.
		/// </summary>
		[Test]
		public void WithoutBeingTold_ASkinStillLooksInTheParentConfig()
		{
			var parent = CreateConfig("Parent", SkinBase);
			var child = CreateConfig("Child", SkinBase, SkinVariant);
			child.Parent = parent;

			Assert.AreSame(Skin(parent, SkinBase), Skin(child, SkinBase).ParentSkin);
			Assert.IsNull(Skin(child, SkinVariant).ParentSkin, "the parent has no skin of that name");
		}

		[Test]
		public void ASkinCannotBuildOnItself()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			var variant = Skin(config, SkinVariant);

			variant.InheritFromSameConfig = true;
			variant.InheritFromSkinName = SkinVariant;
			Assert.IsNull(variant.ParentSkin);

			// And the implicit same-name fallback must not sneak it in through the back door either.
			variant.InheritFromSkinName = null;
			Assert.IsNull(variant.ParentSkin);
		}

		/// <summary>
		/// The editor asks this before offering a choice, so a circle is never created in the first place.
		/// </summary>
		[Test]
		public void ACircleIsRecognisedBeforeItIsMade()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant, "Third");
			MakeVariantBuildOnBase(config);

			var baseSkin = Skin(config, SkinBase);
			var variant = Skin(config, SkinVariant);
			var third = Skin(config, "Third");

			Assert.IsTrue(baseSkin.WouldInheritingFromCreateACycle(variant), "variant already builds on base");
			Assert.IsTrue(baseSkin.WouldInheritingFromCreateACycle(baseSkin), "and it is not itself either");
			Assert.IsFalse(baseSkin.WouldInheritingFromCreateACycle(third), "this one is free");
			Assert.IsFalse(variant.WouldInheritingFromCreateACycle(third));
		}

		/// <summary>
		/// And should one be made anyway - the properties are public - resolution has to end rather than run
		/// forever. It reports the situation and gives up, which is what the depth cap is for.
		/// </summary>
		[Test]
		public void ACircleDoesNotHang()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			MakeVariantBuildOnBase(config);

			var baseSkin = Skin(config, SkinBase);
			baseSkin.InheritFromSameConfig = true;
			baseSkin.InheritFromSkinName = SkinVariant;

			LogAssert.ignoreFailingMessages = true;
			try
			{
				Assert.IsNull(baseSkin.StyleByKey(12345), "no such style anywhere, and the walk has to end");
				Assert.LessOrEqual(baseSkin.SelfAndInheritedSkins().Count, UiStyleConfig.MaxInheritanceDepth);
			}
			finally
			{
				LogAssert.ignoreFailingMessages = false;
			}
		}

		[Test]
		public void TheOwningSkinIsNamed_NotJustTheConfig()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			var style = AddImage(config, SkinBase, StyleName, Color.red);
			MakeVariantBuildOnBase(config);

			var variant = Skin(config, SkinVariant);

			Assert.AreSame(Skin(config, SkinBase), variant.SkinOwning(style.Key));
			Assert.AreSame(config, variant.ConfigOwning(style.Key), "same config - which is exactly the point");
		}

		/// <summary>
		/// The guard this whole feature could break: deciding by config would call a sibling's style own.
		/// </summary>
		[Test]
		public void AStyleFromASibling_CountsAsInherited()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			var style = AddImage(config, SkinBase, StyleName, Color.red);
			MakeVariantBuildOnBase(config);

			using (UiStyleRowContext.Use(config, Skin(config, SkinVariant)))
			{
				Assert.IsTrue(UiStyleRowContext.IsInherited(style), "it belongs to the other skin");
				Assert.AreSame(Skin(config, SkinBase), UiStyleRowContext.SkinOwnerOf(style));
				Assert.AreSame(config, UiStyleRowContext.OwnerOf(style));
			}

			using (UiStyleRowContext.Use(config, Skin(config, SkinBase)))
			{
				Assert.IsFalse(UiStyleRowContext.IsInherited(style), "seen from its own skin it is own");
			}
		}

		/// <summary>
		/// And the write path: overriding a sibling's style must copy it, not hand out the sibling's instance.
		/// Writing to that would change the skin next door, which nobody asked for.
		/// </summary>
		[Test]
		public void OverridingASiblingsStyle_LeavesTheSiblingAlone()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			var original = AddImage(config, SkinBase, StyleName, Color.red);
			MakeVariantBuildOnBase(config);

			var variant = Skin(config, SkinVariant);
			var materialized = variant.MaterializeStyle(original.Key);

			Assert.AreNotSame(original, materialized, "a copy, not the sibling's own instance");
			Assert.IsTrue(variant.OwnsStyle(original.Key));

			((UiStyleImage) materialized).Color.RawValue = Color.blue;

			Assert.AreEqual(Color.red, ((UiStyleImage) original).Color.RawValue, "the sibling did not move");
			Assert.AreEqual(Color.blue, ((UiStyleImage) variant.StyleByKey(original.Key)).Color.RawValue);

			// And back: reverting drops the copy and follows the sibling again.
			variant.RevertStyleToInherited(original.Key);
			Assert.AreSame(original, variant.StyleByKey(original.Key));
		}

		/// <summary>
		/// The report has to follow a sibling mapping too, or it would answer a question nobody asked - and
		/// the numbers are what the conversion decision is made on.
		/// </summary>
		[Test]
		public void TheDriftReport_FollowsASiblingMapping()
		{
			var config = CreateConfig("Config", SkinBase, SkinVariant);
			var other = CreateConfig("Other", SkinBase);
			AddImage(config, SkinBase, StyleName, Color.red);
			AddImage(config, SkinVariant, StyleName, Color.red);
			MakeVariantBuildOnBase(config);

			var drift = UiStyleDriftAnalyzer.Analyze(config, other);
			var variantDrift = drift.Skins[1];

			Assert.AreEqual(SkinBase, variantDrift.OtherSkinName);
			Assert.IsTrue(variantDrift.OtherIsSibling);
			StringAssert.Contains("this config", variantDrift.OtherDescription);
			Assert.AreEqual(1, variantDrift.Count(EStyleDriftState.Identical),
				"same values as the sibling, so it carries nothing of its own");
		}

		// ------------------------------------------------------------------------------- fixtures

		private static UiSkin Skin( UiStyleConfig _config, string _skinName )
			=> _config.GetOwnSkinByNameOrAlias(_skinName, false);

		private static void MakeVariantBuildOnBase( UiStyleConfig _config )
		{
			var variant = Skin(_config, SkinVariant);
			variant.InheritFromSameConfig = true;
			variant.InheritFromSkinName = SkinBase;
		}

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
			Skin(_config, _skinName).Styles.Add(style);
			return style;
		}
	}
}
