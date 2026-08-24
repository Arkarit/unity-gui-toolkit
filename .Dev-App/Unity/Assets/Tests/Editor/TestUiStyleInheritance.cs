using System.Collections.Generic;
using GuiToolkit.Style;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for an inheriting style config: a child names another config as its parent and stores only
	/// what it overrides, everything else resolves through the parent.
	///
	/// The point of inheritance is that a style added to the library reaches every project that consumes
	/// it, instead of a one-time full copy that silently drifts. Written before the implementation, so
	/// these describe what it should do rather than what it happens to do.
	/// </summary>
	[EditorAware]
	public class TestUiStyleInheritance
	{
		private const string SkinDefault = "Default";
		private const string SkinExtra = "Extra";
		private const string SharedStyle = "Test/Shared";
		private const string ParentOnlyStyle = "Test/ParentOnly";
		private const string ChildOnlyStyle = "Test/ChildOnly";

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
		public void WithoutAParent_NothingChanges()
		{
			var config = CreateConfig(SkinDefault);
			var style = AddStyle(config, SkinDefault, SharedStyle);

			Assert.IsNull(config.Parent);
			Assert.AreSame(style, Resolve(config, SkinDefault, SharedStyle));
			Assert.IsNull(Resolve(config, SkinDefault, ParentOnlyStyle));
		}

		/// <summary>
		/// The whole point: a style the child never heard of resolves through the parent's skin of the
		/// same name.
		/// </summary>
		[Test]
		public void AStyleOnlyInTheParent_IsResolvedThroughIt()
		{
			var parent = CreateConfig(SkinDefault);
			var parentStyle = AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			Assert.AreSame(parentStyle, Resolve(child, SkinDefault, ParentOnlyStyle));
		}

		[Test]
		public void AnOwnStyle_WinsOverTheParents()
		{
			var parent = CreateConfig(SkinDefault);
			var parentStyle = AddStyle(parent, SkinDefault, SharedStyle);

			var child = CreateConfig(SkinDefault);
			var childStyle = AddStyle(child, SkinDefault, SharedStyle);
			child.Parent = parent;

			var resolved = Resolve(child, SkinDefault, SharedStyle);

			Assert.AreSame(childStyle, resolved);
			Assert.AreNotSame(parentStyle, resolved, "an override is the point of storing it in the child");
		}

		[Test]
		public void AStyleNeitherKnows_IsStillNull()
		{
			var parent = CreateConfig(SkinDefault);
			AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			Assert.IsNull(Resolve(child, SkinDefault, "Test/NobodyHasThis"));
		}

		/// <summary>
		/// Skins are matched by name, never by index - two configs are not required to list their skins in
		/// the same order, and the parent may well have more of them.
		/// </summary>
		[Test]
		public void SkinsAreMatchedByName_NotByPosition()
		{
			var parent = CreateConfig(SkinExtra, SkinDefault);       // deliberately the other way round
			var inParentDefault = AddStyle(parent, SkinDefault, ParentOnlyStyle);
			var inParentExtra = AddStyle(parent, SkinExtra, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault, SkinExtra);
			child.Parent = parent;

			Assert.AreSame(inParentDefault, Resolve(child, SkinDefault, ParentOnlyStyle));
			Assert.AreSame(inParentExtra, Resolve(child, SkinExtra, ParentOnlyStyle));
		}

		/// <summary>
		/// A skin the child does not declare at all is served by the parent's, so an applier pinned to a
		/// fixed skin keeps working when a project only overrides some of the skins.
		/// </summary>
		[Test]
		public void ASkinOnlyInTheParent_IsFoundByName()
		{
			var parent = CreateConfig(SkinDefault, SkinExtra);
			var parentStyle = AddStyle(parent, SkinExtra, SharedStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			var skin = child.GetSkinByName(SkinExtra);
			Assert.IsNotNull(skin, "the child has no such skin, so the parent's stands in");
			Assert.AreSame(parentStyle, skin.StyleByKey(KeyOf(SharedStyle)));
		}

		[Test]
		public void ChildOnlyStyles_AreUnaffected()
		{
			var parent = CreateConfig(SkinDefault);
			AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			var childStyle = AddStyle(child, SkinDefault, ChildOnlyStyle);
			child.Parent = parent;

			Assert.AreSame(childStyle, Resolve(child, SkinDefault, ChildOnlyStyle));
			Assert.IsNull(Resolve(parent, SkinDefault, ChildOnlyStyle), "inheritance points one way");
		}

		/// <summary>
		/// Inheritance is not limited to one level: a project could sit on a house config that sits on the
		/// package. Depth is capped, and that cap is tested separately once the guard exists.
		/// </summary>
		[Test]
		public void AStyleIsFound_TwoLevelsUp()
		{
			var grandparent = CreateConfig(SkinDefault);
			var grandparentStyle = AddStyle(grandparent, SkinDefault, ParentOnlyStyle);

			var parent = CreateConfig(SkinDefault);
			parent.Parent = grandparent;

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			Assert.AreSame(grandparentStyle, Resolve(child, SkinDefault, ParentOnlyStyle));
		}

		/// <summary>
		/// The effective set is what a skin resolves to as a whole: its own styles plus everything
		/// inherited, with its own winning. Everything that used to read the raw style list of the first
		/// skin - the style name dropdown on an applier, the AI catalog - has to go through this, or
		/// inherited styles are invisible in the editor while working perfectly at runtime.
		/// </summary>
		[Test]
		public void TheEffectiveStyleSet_IsTheUnion_WithOwnWinning()
		{
			var parent = CreateConfig(SkinDefault);
			AddStyle(parent, SkinDefault, SharedStyle);
			var parentOnly = AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			var ownShared = AddStyle(child, SkinDefault, SharedStyle);
			var childOnly = AddStyle(child, SkinDefault, ChildOnlyStyle);
			child.Parent = parent;

			var effective = child.GetSkinByName(SkinDefault).EffectiveStyles;

			Assert.AreEqual(3, effective.Count, "shared counts once");
			Assert.Contains(ownShared, (System.Collections.ICollection)effective);
			Assert.Contains(parentOnly, (System.Collections.ICollection)effective);
			Assert.Contains(childOnly, (System.Collections.ICollection)effective);
		}

		[Test]
		public void TheEffectiveStyleNames_IncludeInheritedOnes()
		{
			var parent = CreateConfig(SkinDefault);
			AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			AddStyle(child, SkinDefault, ChildOnlyStyle);
			child.Parent = parent;

			var names = child.EffectiveStyleNames;

			Assert.Contains(ChildOnlyStyle, names);
			Assert.Contains(ParentOnlyStyle, names, "otherwise the style dropdown cannot offer it");
		}

		[Test]
		public void StyleExists_SeesInheritedStyles()
		{
			var parent = CreateConfig(SkinDefault);
			AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			Assert.IsTrue(child.StyleExists(typeof(UiStyleImage), ParentOnlyStyle));
		}

		/// <summary>
		/// The effective set has to follow a change to either config, the parent included - it is a view,
		/// not a snapshot taken once.
		/// </summary>
		[Test]
		public void TheEffectiveStyleSet_FollowsALaterChangeInTheParent()
		{
			var parent = CreateConfig(SkinDefault);
			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			var skin = child.GetSkinByName(SkinDefault);
			Assert.IsEmpty(skin.EffectiveStyles);

			var added = AddStyle(parent, SkinDefault, ParentOnlyStyle);

			Assert.AreEqual(1, skin.EffectiveStyles.Count);
			Assert.AreSame(added, skin.StyleByKey(added.Key));
		}

		/// <summary>
		/// Two configs naming each other as parent. Written only after the guard existed: without one this
		/// is not a failing test but a StackOverflowException, and that takes the editor with it.
		///
		/// The error message is deliberately not asserted. It is logged through LogErrorOnce, so whether it
		/// appears depends on what ran before in the same session - asserting it would make this test pass
		/// or fail by test order.
		/// </summary>
		[Test]
		public void ACycleInTheParentChain_TerminatesAndResolvesToNull()
		{
			LogAssert.ignoreFailingMessages = true;

			var a = CreateConfig(SkinDefault);
			var b = CreateConfig(SkinDefault);
			a.Parent = b;
			b.Parent = a;

			var inA = AddStyle(a, SkinDefault, ChildOnlyStyle);

			Assert.AreSame(inA, Resolve(a, SkinDefault, ChildOnlyStyle), "its own style is unaffected");
			Assert.IsNull(Resolve(a, SkinDefault, "Test/NobodyHasThis"), "and the cycle just ends");
			Assert.AreSame(inA, Resolve(b, SkinDefault, ChildOnlyStyle), "b inherits from a, once");
		}

		/// <summary>
		/// The chain is walked at most MaxInheritanceDepth configs deep, this one included. A chain that
		/// long is not a real setup - the cap exists so a mistake stays a missing style instead of a hang.
		/// </summary>
		[Test]
		public void AParentChainStops_AtTheDepthLimit()
		{
			LogAssert.ignoreFailingMessages = true;

			var chain = new List<UiStyleConfig>();
			for (int i = 0; i < UiStyleConfig.MaxInheritanceDepth + 2; i++)
			{
				var config = CreateConfig(SkinDefault);
				if (i > 0)
					chain[i - 1].Parent = config;

				chain.Add(config);
			}

			// The last config still within reach, counting the starting one.
			var lastReachable = AddStyle(chain[UiStyleConfig.MaxInheritanceDepth - 1], SkinDefault, SharedStyle);
			var outOfReach = AddStyle(chain[UiStyleConfig.MaxInheritanceDepth], SkinDefault, ParentOnlyStyle);

			Assert.AreSame(lastReachable, Resolve(chain[0], SkinDefault, SharedStyle),
				$"{UiStyleConfig.MaxInheritanceDepth} configs deep must still resolve");
			Assert.IsNull(Resolve(chain[0], SkinDefault, ParentOnlyStyle),
				"one config beyond the limit is ignored rather than followed");
			Assert.IsNotNull(outOfReach, "sanity: the unreachable style does exist");
		}

		/// <summary>
		/// Skin tweening used to pair the outgoing and the incoming skin by POSITION, with an assert that
		/// both hold the same number of styles - which an inheriting config breaks by design. Pairing by
		/// key is the fix, and it is a pure function so it can be tested here rather than in play mode,
		/// where the tween actually runs.
		/// </summary>
		[Test]
		public void SkinTweenPairing_MatchesByKey_RegardlessOfOrder()
		{
			var config = CreateConfig(SkinDefault, SkinExtra);
			var fromA = AddStyle(config, SkinDefault, SharedStyle);
			var fromB = AddStyle(config, SkinDefault, ChildOnlyStyle);

			// The other skin lists the same two styles the other way round.
			var toB = AddStyle(config, SkinExtra, ChildOnlyStyle);
			var toA = AddStyle(config, SkinExtra, SharedStyle);

			var pairs = UiStyleManager.PairStylesByKey(
				config.GetSkinByName(SkinDefault).EffectiveStyles,
				config.GetSkinByName(SkinExtra).EffectiveStyles);

			Assert.AreEqual(2, pairs.Count);
			foreach (var pair in pairs)
			{
				Assert.AreEqual(pair.From.Key, pair.To.Key, "a pair is one style continued, not two neighbours");
			}

			Assert.IsTrue(pairs.Exists(p => p.From == fromA && p.To == toA));
			Assert.IsTrue(pairs.Exists(p => p.From == fromB && p.To == toB));
		}

		/// <summary>
		/// A style only one of the two skins has cannot be tweened - it has nothing to come from, so it
		/// takes its new value directly. What must NOT happen is the whole skin change being refused,
		/// which is what the old count assert did.
		/// </summary>
		[Test]
		public void SkinTweenPairing_SkipsStylesWithoutACounterpart()
		{
			var config = CreateConfig(SkinDefault, SkinExtra);
			var inBoth = AddStyle(config, SkinDefault, SharedStyle);
			AddStyle(config, SkinDefault, ChildOnlyStyle);

			var inBothOther = AddStyle(config, SkinExtra, SharedStyle);
			AddStyle(config, SkinExtra, ParentOnlyStyle);

			var pairs = UiStyleManager.PairStylesByKey(
				config.GetSkinByName(SkinDefault).EffectiveStyles,
				config.GetSkinByName(SkinExtra).EffectiveStyles);

			Assert.AreEqual(1, pairs.Count, "only the style both skins have is a pair");
			Assert.AreSame(inBoth, pairs[0].From);
			Assert.AreSame(inBothOther, pairs[0].To);
		}

		[Test]
		public void SkinTweenPairing_PairsInheritedStylesToo()
		{
			var parent = CreateConfig(SkinDefault, SkinExtra);
			var inParentDefault = AddStyle(parent, SkinDefault, ParentOnlyStyle);
			var inParentExtra = AddStyle(parent, SkinExtra, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault, SkinExtra);
			child.Parent = parent;

			var pairs = UiStyleManager.PairStylesByKey(
				child.GetSkinByName(SkinDefault).EffectiveStyles,
				child.GetSkinByName(SkinExtra).EffectiveStyles);

			Assert.AreEqual(1, pairs.Count, "the child overrides nothing, so both sides are inherited");
			Assert.AreSame(inParentDefault, pairs[0].From);
			Assert.AreSame(inParentExtra, pairs[0].To);
		}

		/// <summary>
		/// The case this exists for, taken from the real projects: the client config has the skins Default
		/// and BOTW, the package config has Default and Light. Matching by name alone leaves BOTW with
		/// nothing to inherit - half the config still a full copy. Naming a parent skin fixes that.
		/// </summary>
		[Test]
		public void ASkinCanNameTheParentSkinItBuildsOn()
		{
			var package = CreateConfig("Default", "Light");
			var inPackageDefault = AddStyle(package, "Default", ParentOnlyStyle);

			var client = CreateConfig("Default", "BOTW");
			client.Parent = package;

			var botw = client.GetOwnSkinByNameOrAlias("BOTW", false);

			// By name only: no counterpart, nothing inherited.
			Assert.IsNull(botw.ParentSkin);
			Assert.IsNull(botw.StyleByKey(inPackageDefault.Key));

			botw.InheritFromSkinName = "Default";

			Assert.AreSame(package.GetOwnSkinByNameOrAlias("Default", false), botw.ParentSkin);
			Assert.AreSame(inPackageDefault, botw.StyleByKey(inPackageDefault.Key));
			Assert.AreEqual(1, botw.EffectiveStyles.Count);
		}

		[Test]
		public void AnEmptyMapping_MeansTheSameName()
		{
			var parent = CreateConfig(SkinDefault);
			var inParent = AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);

			Assert.IsEmpty(skin.InheritFromSkinName ?? string.Empty);
			Assert.AreEqual(SkinDefault, skin.EffectiveInheritFromSkinName);
			Assert.AreSame(inParent, skin.StyleByKey(inParent.Key));

			// Setting it back to nothing returns to matching by name.
			skin.InheritFromSkinName = SkinExtra;
			Assert.IsNull(skin.StyleByKey(inParent.Key));
			skin.InheritFromSkinName = null;
			Assert.AreSame(inParent, skin.StyleByKey(inParent.Key));
		}

		/// <summary>
		/// Every level maps on its own, which is why the chain is walked skin by skin rather than
		/// config by config.
		/// </summary>
		[Test]
		public void EachLevelOfTheChain_MapsSeparately()
		{
			var grandparent = CreateConfig("Base");
			var inGrandparent = AddStyle(grandparent, "Base", ParentOnlyStyle);

			var parent = CreateConfig("House");
			parent.Parent = grandparent;
			parent.GetOwnSkinByNameOrAlias("House", false).InheritFromSkinName = "Base";

			var child = CreateConfig("Project");
			child.Parent = parent;
			var projectSkin = child.GetOwnSkinByNameOrAlias("Project", false);
			projectSkin.InheritFromSkinName = "House";

			Assert.AreSame(inGrandparent, projectSkin.StyleByKey(inGrandparent.Key));
			Assert.AreEqual(3, projectSkin.SelfAndInheritedSkins().Count);
			Assert.AreSame(grandparent, projectSkin.ConfigOwning(inGrandparent.Key));
		}

		[Test]
		public void AMappingToASkinTheParentDoesNotHave_InheritsNothing()
		{
			var parent = CreateConfig(SkinDefault);
			var inParent = AddStyle(parent, SkinDefault, ParentOnlyStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);
			skin.InheritFromSkinName = "NoSuchSkin";

			Assert.IsNull(skin.ParentSkin);
			Assert.IsNull(skin.StyleByKey(inParent.Key));
			Assert.IsEmpty(skin.EffectiveStyles, "and nothing is silently inherited from elsewhere");
		}

		[Test]
		public void OverridingWorksOnAMappedSkin()
		{
			var package = CreateConfig("Default");
			var inPackage = AddStyle(package, "Default", ParentOnlyStyle);

			var client = CreateConfig("BOTW");
			client.Parent = package;
			var botw = client.GetOwnSkinByNameOrAlias("BOTW", false);
			botw.InheritFromSkinName = "Default";

			var own = botw.MaterializeStyle(inPackage.Key);

			Assert.IsNotNull(own);
			Assert.AreNotSame(inPackage, own);
			Assert.IsTrue(botw.OwnsStyle(inPackage.Key));

			botw.RevertStyleToInherited(inPackage.Key);
			Assert.AreSame(inPackage, botw.StyleByKey(inPackage.Key));
		}

		private static int KeyOf( string _styleName ) => UiStyleUtility.GetKey(typeof(Image), _styleName);

		private static UiAbstractStyleBase Resolve( UiStyleConfig _config, string _skinName, string _styleName )
		{
			var skin = _config.GetSkinByName(_skinName);
			return skin?.StyleByKey(KeyOf(_styleName));
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

		private static UiStyleImage AddStyle( UiStyleConfig _config, string _skinName, string _styleName )
		{
			var style = new UiStyleImage(_config, _styleName);
			_config.GetSkinByName(_skinName).Styles.Add(style);
			return style;
		}
	}
}
