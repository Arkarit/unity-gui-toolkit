using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for removing an inherited style from one skin - the pendant to a prefab instance's removed
	/// component, and the one thing a child config could not say before: "the config we build on has this,
	/// we do not".
	///
	/// The reason it is not simply an override with every property switched off, which is what one reaches
	/// for otherwise: a style resolves as a WHOLE from the nearest skin that owns it, so such an override
	/// keeps the inherited values from ever being asked for while still filling every style popup - and
	/// IsApplicable is synchronised across the skins of a config, so switching a property off in one skin
	/// switches it off in its siblings too. What a removal does instead is pinned below.
	/// </summary>
	[EditorAware]
	public class TestUiStyleSuppression
	{
		private const string SkinDefault = "Default";
		private const string SkinExtra = "Extra";
		private const string InheritedStyle = "Test/Inherited";

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
		public void RemovingAnInheritedStyle_StopsItFromResolving()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);

			Assert.AreSame(inherited, skin.StyleByKey(inherited.Key), "sanity: it resolves first");

			Assert.IsTrue(skin.SuppressStyle(inherited.Key));

			Assert.IsTrue(skin.SuppressesStyle(inherited.Key));
			Assert.IsNull(skin.StyleByKey(inherited.Key), "the walk up the chain stops here now");
			Assert.IsEmpty(skin.Styles, "and nothing was added to do it");
		}

		/// <summary>
		/// The whole point of a removal over a deletion: it is local. What it was inherited from keeps it,
		/// including for every other skin that builds on the same thing.
		/// </summary>
		[Test]
		public void TheConfigItComesFrom_IsLeftAlone()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var parentSkin = Skin(parent, SkinDefault);

			Skin(child, SkinDefault).SuppressStyle(inherited.Key);

			Assert.AreSame(inherited, parentSkin.StyleByKey(inherited.Key));
			Assert.IsFalse(parentSkin.SuppressesStyle(inherited.Key));
			Assert.AreEqual(1, parentSkin.Styles.Count);
		}

		/// <summary>
		/// A style nothing else provides cannot be removed - taking the only copy out is a deletion, and
		/// that has its own command and its own dialog.
		/// </summary>
		[Test]
		public void RemovingWhatIsNotInherited_IsRefused()
		{
			LogAssert.ignoreFailingMessages = true;

			var (parent, child) = CreatePair();
			var childOnly = AddStyle(child, SkinDefault, "Test/ChildOnly");
			var skin = Skin(child, SkinDefault);

			Assert.IsFalse(skin.SuppressStyle(childOnly.Key));

			Assert.IsFalse(skin.SuppressesStyle(childOnly.Key));
			Assert.AreSame(childOnly, skin.StyleByKey(childOnly.Key), "it is still there");
			Assert.IsNotNull(parent, "sanity");
		}

		[Test]
		public void RemovingSomethingUnknown_IsRefused()
		{
			LogAssert.ignoreFailingMessages = true;

			var (parent, child) = CreatePair();
			var skin = Skin(child, SkinDefault);

			Assert.IsFalse(skin.SuppressStyle(UiStyleUtility.GetKey(typeof(Image), "Test/NobodyHasThis")));
			Assert.IsNotNull(parent, "sanity");
		}

		/// <summary>
		/// One decision, one step: an override that gets removed loses its own copy on the way, because a
		/// skin that both overrode a style and removed it would be saying two opposite things.
		/// </summary>
		[Test]
		public void RemovingAnOverride_DropsTheOwnCopyToo()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.MaterializeStyle(inherited.Key);

			Assert.IsTrue(skin.OwnsStyle(inherited.Key), "sanity: overridden first");

			Assert.IsTrue(skin.SuppressStyle(inherited.Key));

			Assert.IsFalse(skin.OwnsStyle(inherited.Key), "the copy is gone, not just shadowed");
			Assert.IsEmpty(skin.Styles);
			Assert.IsNull(skin.StyleByKey(inherited.Key));
		}

		[Test]
		public void Restoring_InheritsItAgain()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.SuppressStyle(inherited.Key);

			Assert.IsTrue(skin.RestoreSuppressedStyle(inherited.Key));

			Assert.IsFalse(skin.SuppressesStyle(inherited.Key));
			Assert.AreSame(inherited, skin.StyleByKey(inherited.Key));
			Assert.AreEqual(0, skin.SuppressedCount);
		}

		[Test]
		public void RestoringWhatWasNeverRemoved_ChangesNothing()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);

			Assert.IsFalse(skin.RestoreSuppressedStyle(inherited.Key));
			Assert.AreSame(inherited, skin.StyleByKey(inherited.Key));
		}

		/// <summary>
		/// The payoff an all-properties-off override cannot give: the style leaves the VOCABULARY, so it
		/// stops being offered in every style popup and to the AI catalog.
		/// </summary>
		[Test]
		public void ARemovedStyle_IsNotInTheEffectiveSet()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);

			Assert.Contains(InheritedStyle, child.EffectiveStyleNames, "sanity: offered first");

			skin.SuppressStyle(inherited.Key);

			Assert.IsEmpty(skin.EffectiveStyles);
			Assert.IsEmpty(child.EffectiveStyleNames);
			Assert.IsFalse(child.StyleExists(typeof(UiStyleImage), InheritedStyle));
			Assert.Contains(InheritedStyle, parent.EffectiveStyleNames, "and the parent still offers it");
		}

		/// <summary>
		/// A removal halfway up the chain hides the style from everything below it, rather than merely
		/// dropping the nearest copy and letting the next one through.
		/// </summary>
		[Test]
		public void ARemovalHalfwayUpTheChain_HidesItFromBelow()
		{
			var grandparent = CreateConfig(SkinDefault);
			var inGrandparent = AddStyle(grandparent, SkinDefault, InheritedStyle);

			var parent = CreateConfig(SkinDefault);
			parent.Parent = grandparent;

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			Skin(parent, SkinDefault).SuppressStyle(inGrandparent.Key);

			var childSkin = Skin(child, SkinDefault);
			Assert.IsNull(childSkin.StyleByKey(inGrandparent.Key));
			Assert.IsEmpty(childSkin.EffectiveStyles);
			Assert.IsFalse(childSkin.SuppressesStyle(inGrandparent.Key), "it was not this skin that removed it");
		}

		[Test]
		public void RemovingInOneSkin_LeavesTheOtherSkinAlone()
		{
			var parent = CreateConfig(SkinDefault, SkinExtra);
			var inDefault = AddStyle(parent, SkinDefault, InheritedStyle);
			var inExtra = AddStyle(parent, SkinExtra, InheritedStyle);

			var child = CreateConfig(SkinDefault, SkinExtra);
			child.Parent = parent;

			Skin(child, SkinDefault).SuppressStyle(inDefault.Key);

			Assert.IsNull(Skin(child, SkinDefault).StyleByKey(inDefault.Key));
			Assert.AreSame(inExtra, Skin(child, SkinExtra).StyleByKey(inExtra.Key), "the other skin keeps it");
		}

		/// <summary>
		/// The case the workaround cannot do at all: a skin that builds on a SIBLING of the same config -
		/// which is how the client is set up - removing something the sibling has.
		/// </summary>
		[Test]
		public void ASkinCanRemoveWhatItInheritsFromItsSibling()
		{
			var config = CreateConfig(SkinDefault, SkinExtra);
			var inDefault = AddStyle(config, SkinDefault, InheritedStyle);

			var variant = Skin(config, SkinExtra);
			variant.InheritFromSameConfig = true;
			variant.InheritFromSkinName = SkinDefault;

			Assert.AreSame(inDefault, variant.StyleByKey(inDefault.Key), "sanity: it comes from the sibling");

			Assert.IsTrue(variant.SuppressStyle(inDefault.Key));

			Assert.IsNull(variant.StyleByKey(inDefault.Key));
			Assert.AreSame(inDefault, Skin(config, SkinDefault).StyleByKey(inDefault.Key),
				"and the sibling is untouched - which switching properties off would not have managed");
		}

		/// <summary>
		/// Wanting an own value here supersedes having removed the style. Matters for callers that
		/// materialise without a row in front of them - the AI style writer does exactly that.
		/// </summary>
		[Test]
		public void MaterializingARemovedStyle_TakesTheRemovalBack()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.SuppressStyle(inherited.Key);

			var own = skin.MaterializeStyle(inherited.Key);

			Assert.IsNotNull(own);
			Assert.AreNotSame(inherited, own);
			Assert.IsFalse(skin.SuppressesStyle(inherited.Key));
			Assert.AreSame(own, skin.StyleByKey(inherited.Key));
		}

		/// <summary>
		/// A removal of something nothing offers any more hides nothing and only accumulates, so it goes on
		/// the next Init.
		/// </summary>
		[Test]
		public void ARemovalOfSomethingGone_IsPrunedOnInit()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.SuppressStyle(inherited.Key);

			Skin(parent, SkinDefault).DeleteStyle(inherited);
			skin.Init(child);

			Assert.AreEqual(0, skin.SuppressedCount);
		}

		/// <summary>
		/// And the guard that makes the pruning safe: while assets are still loading, a parent that is not
		/// reachable yet must not be read as "nothing is inherited any more". Nothing is pruned without a
		/// parent skin to ask.
		/// </summary>
		[Test]
		public void WithoutAReachableParentSkin_NothingIsPruned()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.SuppressStyle(inherited.Key);

			child.Parent = null;
			skin.Init(child);

			Assert.AreEqual(1, skin.SuppressedCount, "the removal survives, and comes back with the parent");

			child.Parent = parent;
			Assert.IsNull(skin.StyleByKey(inherited.Key));
		}

		/// <summary>
		/// The editor has to keep drawing a removed row - a removal that cannot be seen cannot be taken
		/// back - so where the style comes from stays answerable, and the row context reports the removal
		/// as a state of its own rather than as an absence.
		/// </summary>
		[Test]
		public void ARemovedRow_StillKnowsWhereItCameFrom()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.SuppressStyle(inherited.Key);

			Assert.AreSame(Skin(parent, SkinDefault), skin.SkinOwning(inherited.Key));
			Assert.AreSame(parent, skin.ConfigOwning(inherited.Key));

			using (UiStyleRowContext.Use(child, skin))
			{
				Assert.IsTrue(UiStyleRowContext.IsSuppressed(inherited));
				Assert.IsTrue(UiStyleRowContext.IsInherited(inherited), "it does still come from there");
				Assert.IsFalse(UiStyleRowContext.IsOverride(inherited), "but there is no own copy of it");
			}
		}

		/// <summary>
		/// An applier pointing at a removed style must not be told the style does not exist: nothing is
		/// wrong with the config, and what to do about it is somewhere else entirely.
		/// </summary>
		[Test]
		public void TheDiagnosticSaysRemoved_NotMissing()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = Skin(child, SkinDefault);
			skin.SuppressStyle(inherited.Key);

			var message = UiStyleDiagnostics.ExplainMissingStyle
				(child, skin, SkinDefault, InheritedStyle, typeof(Image));

			StringAssert.Contains("removed", message);
			StringAssert.Contains(InheritedStyle, message);
			StringAssert.DoesNotContain("does not exist", message);
			Assert.IsNotNull(parent, "sanity");
		}

		private (UiStyleConfig parent, UiStyleConfig child) CreatePair()
		{
			var parent = CreateConfig(SkinDefault);
			var child = CreateConfig(SkinDefault);
			child.Parent = parent;
			return (parent, child);
		}

		private UiStyleConfig CreateConfig( params string[] _skinNames )
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = "TestStyleConfig";
			m_created.Add(config);

			var skins = new List<UiSkin>();
			foreach (var skinName in _skinNames)
				skins.Add(new UiSkin(config, skinName));

			config.Skins = skins;
			return config;
		}

		private static UiSkin Skin( UiStyleConfig _config, string _skinName )
			=> _config.GetOwnSkinByNameOrAlias(_skinName, false);

		private static UiAbstractStyleBase AddStyle( UiStyleConfig _config, string _skinName, string _styleName )
		{
			var style = new UiStyleImage(_config, _styleName);
			Skin(_config, _skinName).Styles.Add(style);
			return style;
		}
	}
}
