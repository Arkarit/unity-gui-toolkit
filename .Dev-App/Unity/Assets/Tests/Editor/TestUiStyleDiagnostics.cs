using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for what the inspector SAYS when a style does not resolve, and for the value the style popup
	/// shows while it does not.
	///
	/// Both used to be silent in the same misleading way: an empty popup and "No Style assigned yet" for a
	/// style that was assigned all along and only had nowhere to resolve from. The texts live outside the
	/// drawers so they can be asked for here - an inspector that is not visible never repaints, so a drawer
	/// is the one place these cannot be checked.
	/// </summary>
	[EditorAware]
	public class TestUiStyleDiagnostics
	{
		private const string SkinDefault = "Default";
		private const string SkinExample = "Example";
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

		// ------------------------------------------------------------------ why nothing resolved

		/// <summary>
		/// Nothing assigned is a normal state, not a failure - so there is nothing to explain, and the plain
		/// "No Style assigned yet" that has always been there stays right.
		/// </summary>
		[Test]
		public void NothingAssigned_IsNotExplained()
		{
			var config = CreateConfig("Child", SkinDefault);

			Assert.IsNull(UiStyleDiagnostics.ExplainMissingStyle(config, Skin(config, SkinDefault), SkinDefault, null));
			Assert.IsNull(UiStyleDiagnostics.ExplainMissingStyle(config, Skin(config, SkinDefault), SkinDefault, ""));
		}

		[Test]
		public void WithoutAConfig_TheStyleNameIsStillNamed()
		{
			var message = UiStyleDiagnostics.ExplainMissingStyle(null, null, SkinDefault, StyleName);

			Assert.IsNotNull(message);
			StringAssert.Contains(StyleName, message);
		}

		/// <summary>
		/// A style is identified by name AND component type, so the same name can exist for a different
		/// component - naming only the name would then read as a lie to someone looking at the config.
		/// </summary>
		[Test]
		public void TheComponentType_IsNamedWhenItIsKnown()
		{
			var config = CreateConfig("Child", SkinDefault);

			var message = UiStyleDiagnostics.ExplainMissingStyle
				(config, Skin(config, SkinDefault), SkinDefault, StyleName, "Image");

			StringAssert.Contains("Image", message);
			StringAssert.Contains(StyleName, message);
		}

		[Test]
		public void AnUndeclaredSkin_PointsAtTheSkin()
		{
			var config = CreateConfig("Child", SkinDefault);

			var message = UiStyleDiagnostics.ExplainMissingStyle(config, null, SkinExample, StyleName);

			StringAssert.Contains(SkinExample, message);
			StringAssert.Contains("Child", message);
			StringAssert.Contains("Add that skin", message);
		}

		/// <summary>
		/// A config that inherits from nothing has nowhere else to look, and saying so keeps the reader from
		/// hunting for an inheritance problem that does not exist.
		/// </summary>
		[Test]
		public void WithoutAParent_ThereIsNowhereElseToLook()
		{
			var config = CreateConfig("Child", SkinDefault);

			var message = UiStyleDiagnostics.ExplainMissingStyle
				(config, Skin(config, SkinDefault), SkinDefault, StyleName);

			StringAssert.Contains(StyleName, message);
			StringAssert.Contains(SkinDefault, message);
			StringAssert.Contains("inherits from nothing", message);
		}

		/// <summary>
		/// The case that started all this: the config inherits, but this skin has no counterpart in the
		/// parent - so it stands alone inside a config that otherwise does not. The message has to name the
		/// one field that fixes it.
		/// </summary>
		[Test]
		public void AnUnmappedSkin_NamesTheFieldThatFixesIt()
		{
			var parent = CreateConfig("Parent", SkinDefault, "Light");
			var child = CreateConfig("Child", SkinDefault, SkinExample);
			child.Parent = parent;

			var message = UiStyleDiagnostics.ExplainMissingStyle
				(child, Skin(child, SkinExample), SkinExample, StyleName);

			StringAssert.Contains("inherits nothing", message);
			StringAssert.Contains("Parent", message);
			StringAssert.Contains(SkinExample, message);
			StringAssert.Contains("Inherits skin from", message);
		}

		/// <summary>
		/// Mapped, and the style is still nowhere - a different problem, and it must not read like the one
		/// above or the reader goes back to the mapping field that is already correct.
		/// </summary>
		[Test]
		public void AMappedSkinWithoutTheStyle_NamesTheSkinItLookedIn()
		{
			var parent = CreateConfig("Parent", SkinDefault, "Light");
			var child = CreateConfig("Child", SkinDefault, SkinExample);
			child.Parent = parent;
			Skin(child, SkinExample).InheritFromSkinName = "Light";

			var message = UiStyleDiagnostics.ExplainMissingStyle
				(child, Skin(child, SkinExample), SkinExample, StyleName);

			StringAssert.Contains("Light", message);
			StringAssert.DoesNotContain("inherits nothing", message);
			StringAssert.DoesNotContain("Inherits skin from", message);
		}

		/// <summary>
		/// A skin the config does not declare resolves through an ancestor as a whole, so everything in it
		/// belongs to that ancestor and no override is possible - the reason has to be stated, because the
		/// values look perfectly editable.
		/// </summary>
		[Test]
		public void AForeignSkin_SaysWhyNothingCanBeEdited()
		{
			var parent = CreateConfig("Parent", SkinDefault, "Light");
			var child = CreateConfig("Child", SkinDefault);
			child.Parent = parent;

			var message = UiStyleDiagnostics.ExplainForeignSkin(child, "Light", Skin(parent, "Light"));

			StringAssert.Contains("Child", message);
			StringAssert.Contains("Parent", message);
			StringAssert.Contains("Light", message);
			StringAssert.Contains("read-only", message);
		}

		// ------------------------------------------------------- what the style popup shows meanwhile

		[Test]
		public void AResolvedStyle_ShowsTheEntryTheListHasForIt()
		{
			var config = CreateConfig("Child", SkinDefault);
			var style = new UiStyleImage(config, StyleName);
			var names = new List<string> { StyleName };
			var aliases = new List<string> { "some alias" };

			Assert.AreEqual("some alias", UiStyleEditorUtility.ResolveDisplayAlias(StyleName, style, names, aliases));
			Assert.AreEqual(1, names.Count, "the entry was there, so nothing was added");
		}

		/// <summary>
		/// The popup lists the FIRST skin's styles, so one that exists only in another skin is not in the
		/// list although it resolves perfectly well. It gets an entry like any other - and must not be
		/// labelled missing, because it is not.
		/// </summary>
		[Test]
		public void AResolvedStyleTheListDoesNotHave_IsAddedWithoutBeingCalledMissing()
		{
			var config = CreateConfig("Child", SkinDefault);
			var style = new UiStyleImage(config, StyleName);
			var names = new List<string> { "Other" };
			var aliases = new List<string> { "Other alias" };

			var display = UiStyleEditorUtility.ResolveDisplayAlias(StyleName, style, names, aliases);

			Assert.AreEqual(style.Alias, display);
			Assert.AreEqual(display, aliases[0]);
			Assert.AreEqual(StyleName, names[0]);
			StringAssert.DoesNotContain("missing", display);
		}

		[Test]
		public void NoStoredName_ShowsNothing()
		{
			var names = new List<string> { StyleName };
			var aliases = new List<string> { StyleName };

			Assert.AreEqual(string.Empty, UiStyleEditorUtility.ResolveDisplayAlias(null, null, names, aliases));
			Assert.AreEqual(1, names.Count, "and adds no entry for a name that is not there");
		}

		/// <summary>
		/// The bug: the current skin cannot resolve the style, but the popup lists the effective set - so the
		/// name IS in the list and simply has to be shown, rather than leaving the field blank as if nothing
		/// were assigned.
		/// </summary>
		[Test]
		public void AnUnresolvedButKnownName_ShowsTheAliasFromTheList()
		{
			var names = new List<string> { "Other", StyleName };
			var aliases = new List<string> { "Other alias", "Background alias" };

			var display = UiStyleEditorUtility.ResolveDisplayAlias(StyleName, null, names, aliases);

			Assert.AreEqual("Background alias", display);
			Assert.AreEqual(2, names.Count, "nothing was added, the name was already there");
		}

		/// <summary>
		/// A name nobody knows any more still has to be visible, and the two lists have to stay index-aligned
		/// while it is - the popup reads the name list by the index the alias list produced.
		/// </summary>
		[Test]
		public void AnUnknownName_BecomesItsOwnEntryInBothLists()
		{
			var names = new List<string> { "Other" };
			var aliases = new List<string> { "Other alias" };

			var display = UiStyleEditorUtility.ResolveDisplayAlias(StyleName, null, names, aliases);

			Assert.AreEqual(display, aliases[0]);
			StringAssert.Contains(StyleName, display);
			StringAssert.Contains("missing", display);
			Assert.AreEqual(StyleName, names[0], "same index in both lists, or the popup picks another style");
			Assert.AreEqual("Other", names[1]);
			Assert.AreEqual("Other alias", aliases[1]);
		}

		// ------------------------------------------------------------------------------- fixtures

		private static UiSkin Skin( UiStyleConfig _config, string _skinName )
			=> _config.GetOwnSkinByNameOrAlias(_skinName, false);

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
	}
}
