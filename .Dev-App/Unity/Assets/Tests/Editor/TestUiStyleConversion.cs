using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the half of the conversion tool that writes - turning a clone into a child by dropping the
	/// copies that carry no difference.
	///
	/// This is the only part of the whole feature that REMOVES data, so the tests are mostly about what it
	/// must not do: never drop something that differs, never drop something with nothing to fall back to,
	/// never drop something the user pinned. A style lost here is somebody's work lost.
	/// </summary>
	[EditorAware]
	public class TestUiStyleConversion
	{
		private const string SkinDefault = "Default";
		private const string SkinVariant = "Variant";
		private const string SkinLight = "Light";

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

		/// <summary>
		/// Only identical copies are on the list. A difference is a decision somebody made, and a style that
		/// exists on one side only has nothing to be inherited from.
		/// </summary>
		[Test]
		public void OnlyCopiesThatCarryNothing_AreOfferedForDropping()
		{
			var (child, parent) = CreatePair();
			AddImage(child, SkinDefault, "Same", Color.red);
			AddImage(parent, SkinDefault, "Same", Color.red);
			AddImage(child, SkinDefault, "Different", Color.red);
			AddImage(parent, SkinDefault, "Different", Color.green);
			AddImage(child, SkinDefault, "OnlyHere", Color.red);

			var plan = UiStyleConversion.Plan(child, parent);

			Assert.AreEqual(1, plan.Entries.Count);
			Assert.AreEqual("Same", plan.Entries[0].Alias);
			Assert.AreSame(parent, plan.ParentToSet, "not related yet, so this is part of the conversion");
		}

		[Test]
		public void AnExistingParent_IsNotSetAgain()
		{
			var (child, parent) = CreatePair();
			child.Parent = parent;
			AddImage(child, SkinDefault, "Same", Color.red);
			AddImage(parent, SkinDefault, "Same", Color.red);

			Assert.IsNull(UiStyleConversion.Plan(child, parent).ParentToSet);
		}

		/// <summary>
		/// The whole point, end to end: afterwards the style is gone from the child and still resolves.
		/// </summary>
		[Test]
		public void Applying_DropsTheCopyAndKeepsTheStyle()
		{
			var (child, parent) = CreatePair();
			var copy = AddImage(child, SkinDefault, "Same", Color.red);
			var original = AddImage(parent, SkinDefault, "Same", Color.red);

			UiStyleConversion.Apply(UiStyleConversion.Plan(child, parent));

			var skin = Skin(child, SkinDefault);
			Assert.AreSame(parent, child.Parent);
			Assert.IsFalse(skin.OwnsStyle(copy.Key), "the copy is gone");
			Assert.AreSame(original, skin.StyleByKey(copy.Key), "and the style resolves from the parent");
		}

		/// <summary>
		/// Pinning: identical today, and deliberately kept so it stops following the other config tomorrow.
		/// </summary>
		[Test]
		public void APinnedCopy_IsKept()
		{
			var (child, parent) = CreatePair();
			var copy = AddImage(child, SkinDefault, "Same", Color.red);
			AddImage(parent, SkinDefault, "Same", Color.red);

			var plan = UiStyleConversion.Plan(child, parent);
			plan.Entries[0].Drop = false;

			Assert.AreEqual(0, plan.DropCount);
			Assert.AreEqual(1, plan.PinnedCount);

			UiStyleConversion.Apply(plan);

			Assert.IsTrue(Skin(child, SkinDefault).OwnsStyle(copy.Key), "kept, and it is the child's own now");
			Assert.AreSame(parent, child.Parent);
		}

		/// <summary>
		/// A skin with no counterpart inherits nothing, so nothing of it may be dropped - dropping there would
		/// not be a conversion but a deletion.
		/// </summary>
		[Test]
		public void AnUnmappedSkin_IsLeftAlone()
		{
			var child = CreateConfig("Child", SkinDefault, SkinVariant);
			var parent = CreateConfig("Parent", SkinDefault, SkinLight);
			var lonely = AddImage(child, SkinVariant, "Same", Color.red);
			AddImage(parent, SkinLight, "Same", Color.red);

			var plan = UiStyleConversion.Plan(child, parent);
			Assert.AreEqual(0, plan.Entries.Count);

			UiStyleConversion.Apply(plan);
			Assert.IsTrue(Skin(child, SkinVariant).OwnsStyle(lonely.Key), "still there, still the only copy");
		}

		/// <summary>
		/// And if the ground moves between planning and applying - the style disappears from the parent - the
		/// drop is refused rather than carried out. The plan is not the authority on what can be removed;
		/// UiSkin.RevertStyleToInherited is, and it only ever removes what something else still provides.
		/// </summary>
		[Test]
		public void APlanThatHasGoneStale_LosesNothing()
		{
			var (child, parent) = CreatePair();
			var copy = AddImage(child, SkinDefault, "Same", Color.red);
			var original = AddImage(parent, SkinDefault, "Same", Color.red);

			var plan = UiStyleConversion.Plan(child, parent);
			Skin(parent, SkinDefault).Styles.Remove(original);
			Skin(parent, SkinDefault).InvalidateStyleLookup();

			LogAssert.ignoreFailingMessages = true;
			try
			{
				var result = UiStyleConversion.Apply(plan);
				StringAssert.Contains("could not be dropped", result);
			}
			finally
			{
				LogAssert.ignoreFailingMessages = false;
			}

			Assert.IsTrue(Skin(child, SkinDefault).OwnsStyle(copy.Key), "the only copy left, and it stayed");
		}

		/// <summary>
		/// The same works within one config, since a skin may build on a sibling: a variant that repeats its
		/// base skin's values verbatim is just as much a copy.
		/// </summary>
		[Test]
		public void ACopyOfASiblingSkin_IsDroppedToo()
		{
			var child = CreateConfig("Child", SkinDefault, SkinVariant);
			var parent = CreateConfig("Parent", SkinDefault);
			var baseStyle = AddImage(child, SkinDefault, "Same", Color.red);
			AddImage(child, SkinVariant, "Same", Color.red);

			var variant = Skin(child, SkinVariant);
			variant.InheritFromSameConfig = true;
			variant.InheritFromSkinName = SkinDefault;

			var plan = UiStyleConversion.Plan(child, parent);
			UiStyleConversion.Apply(plan);

			Assert.IsFalse(variant.OwnsStyle(baseStyle.Key), "the repetition is gone");
			Assert.AreSame(baseStyle, variant.StyleByKey(baseStyle.Key), "and it follows the skin next door");
			Assert.IsTrue(Skin(child, SkinDefault).OwnsStyle(baseStyle.Key), "which still holds it");
		}

		// ------------------------------------------------------------------------------- fixtures

		private static UiSkin Skin( UiStyleConfig _config, string _skinName )
			=> _config.GetOwnSkinByNameOrAlias(_skinName, false);

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
			Skin(_config, _skinName).Styles.Add(style);
			return style;
		}
	}
}
