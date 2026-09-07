using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the skin list following the parent field.
	///
	/// A config created by hand and then given a parent had no skins, and that is not the harmless empty
	/// state it looks like: styles are matched skin by skin, so a child that declares no skin has nothing to
	/// override into and resolves WHOLE skins from the parent instead - read-only, with no way in. The
	/// Inherit button in the configuration window never showed it, because it clones the parent and empties
	/// the styles, so the skins come along.
	/// </summary>
	[EditorAware]
	public class TestUiStyleSkinMirroring
	{
		private const string SkinDefault = "Default";
		private const string SkinLight = "Light";
		private const string StyleName = "Test/Style";

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
		public void AConfigWithoutSkins_GetsOnePerSkinOfItsParent()
		{
			var parent = CreateConfig(SkinDefault, SkinLight);
			var child = CreateConfig();
			child.Parent = parent;

			var added = UiStyleEditorUtility.MirrorParentSkins(child);

			Assert.AreEqual(new[] { SkinDefault, SkinLight }, added);
			Assert.AreEqual(new[] { SkinDefault, SkinLight }, child.SkinNames.ToArray());
			Assert.AreEqual(0, child.Skins[0].Styles.Count, "empty: everything still comes from the parent");
		}

		/// <summary>
		/// The mirrored skins are what makes the parent usable: before them a style resolves but belongs to
		/// a skin this config does not declare, so there is nothing to override into.
		/// </summary>
		[Test]
		public void OnlyAfterMirroring_IsThereASkinToOverrideInto()
		{
			var parent = CreateConfig(SkinDefault);
			var inParent = AddStyle(parent, SkinDefault, StyleName);
			var child = CreateConfig();
			child.Parent = parent;

			Assert.IsNull(child.GetOwnSkinByNameOrAlias(SkinDefault, false), "sanity: no own skin yet");
			Assert.AreSame(inParent, child.GetSkinByName(SkinDefault).StyleByKey(inParent.Key),
				"it resolves - through a skin that belongs to the parent");

			UiStyleEditorUtility.MirrorParentSkins(child);

			var ownSkin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);
			Assert.IsNotNull(ownSkin);
			Assert.AreSame(inParent, ownSkin.StyleByKey(inParent.Key), "still resolves, now through its own skin");

			var own = ownSkin.MaterializeStyle(inParent.Key);
			Assert.IsNotNull(own);
			Assert.AreNotSame(inParent, own, "and can be overridden, which is what was impossible before");
		}

		/// <summary>
		/// The limit that matters more than the feature: a project's own skin is routinely the only one it
		/// has, mapped to a differently named skin of the parent. Topping that up to the parent's set would
		/// add skins nobody asked for - two of them, measured on the client.
		/// </summary>
		[Test]
		public void AConfigThatHasSkins_IsLeftAlone()
		{
			var parent = CreateConfig(SkinDefault, SkinLight);
			var child = CreateConfig("BOTW");
			child.Parent = parent;
			child.Skins[0].InheritFromSkinName = SkinDefault;

			var added = UiStyleEditorUtility.MirrorParentSkins(child);

			Assert.IsEmpty(added);
			Assert.AreEqual(new[] { "BOTW" }, child.SkinNames.ToArray());
		}

		[Test]
		public void WithoutAParent_NothingIsMirrored()
		{
			var config = CreateConfig();

			Assert.IsEmpty(UiStyleEditorUtility.MirrorParentSkins(config));
			Assert.AreEqual(0, config.NumSkins);
		}

		/// <summary>
		/// An aspect-ratio-dependent skin is selected BY its threshold, so a mirrored one has to carry the
		/// same - and its constructor refuses the plain-config combination outright, which is why the two
		/// cases cannot share a value.
		/// </summary>
		[Test]
		public void AnAspectRatioDependentConfig_MirrorsTheThresholds()
		{
			var parent = CreateAspectConfig("ProbeAspectParent");
			parent.Skins = new List<UiSkin>
			{
				new UiSkin(parent, "Landscape", 1.5f),
				new UiSkin(parent, "Portrait", 0f),
			};

			var child = CreateAspectConfig("ProbeAspectChild");
			child.Skins = new List<UiSkin>();   // as if its skins had been deleted by hand
			child.Parent = parent;

			UiStyleEditorUtility.MirrorParentSkins(child);

			Assert.AreEqual(2, child.NumSkins);
			Assert.AreEqual(1.5f, child.Skins[0].AspectRatioGreaterEqual);
			Assert.AreEqual(0f, child.Skins[1].AspectRatioGreaterEqual);
		}

		/// <summary>
		/// A new aspect-ratio-dependent config gives itself its two skins - which it did not: the branch
		/// meant to do it went through a static field that is never assigned, so it threw a
		/// NullReferenceException out of OnEnable, where Unity logs it and carries on. The config then
		/// stayed empty, and an empty aspect-ratio config resolves nothing at all.
		/// </summary>
		[Test]
		public void AFreshAspectRatioConfig_GivesItselfItsSkins()
		{
			var config = CreateAspectConfig("ProbeAspectFresh");

			Assert.AreEqual(new[] { UiAspectRatioDependentStyleConfig.Landscape,
			                        UiAspectRatioDependentStyleConfig.Portrait },
			                config.SkinNames.ToArray());
			Assert.AreEqual(1f, config.Skins[0].AspectRatioGreaterEqual);
			Assert.AreEqual(0f, config.Skins[1].AspectRatioGreaterEqual);
		}

		// ------------------------------------------------------------------ and back out again

		[Test]
		public void DroppingTakesTheSkinsNothingWasPutInto()
		{
			var parent = CreateConfig(SkinDefault, SkinLight);
			var child = CreateConfig();
			child.Parent = parent;
			UiStyleEditorUtility.MirrorParentSkins(child);

			var dropped = UiStyleEditorUtility.DropSkinsWithoutOwnContent(child);

			Assert.AreEqual(new[] { SkinDefault, SkinLight }, dropped);
			Assert.AreEqual(0, child.NumSkins);
		}

		[Test]
		public void ASkinWithAStyleOfItsOwn_Survives()
		{
			var parent = CreateConfig(SkinDefault, SkinLight);
			var child = CreateConfig();
			child.Parent = parent;
			UiStyleEditorUtility.MirrorParentSkins(child);
			AddStyle(child, SkinDefault, StyleName);

			var dropped = UiStyleEditorUtility.DropSkinsWithoutOwnContent(child);

			Assert.AreEqual(new[] { SkinLight }, dropped);
			Assert.AreEqual(new[] { SkinDefault }, child.SkinNames.ToArray());
		}

		/// <summary>
		/// A removal is an entry as much as a style is - it is the whole point of the state that a skin can
		/// hold nothing but removals.
		/// </summary>
		[Test]
		public void ASkinThatOnlyRemovedSomething_Survives()
		{
			var parent = CreateConfig(SkinDefault, SkinLight);
			var inParent = AddStyle(parent, SkinDefault, StyleName);
			var child = CreateConfig();
			child.Parent = parent;
			UiStyleEditorUtility.MirrorParentSkins(child);

			Assert.IsTrue(child.GetOwnSkinByNameOrAlias(SkinDefault, false).SuppressStyle(inParent.Key));

			var dropped = UiStyleEditorUtility.DropSkinsWithoutOwnContent(child);

			Assert.AreEqual(new[] { SkinLight }, dropped);
			Assert.AreEqual(new[] { SkinDefault }, child.SkinNames.ToArray());
		}

		[Test]
		public void ARenamedSkin_Survives()
		{
			var parent = CreateConfig(SkinDefault);
			var child = CreateConfig();
			child.Parent = parent;
			UiStyleEditorUtility.MirrorParentSkins(child);
			child.Skins[0].Alias = "Renamed by hand";

			Assert.IsEmpty(UiStyleEditorUtility.DropSkinsWithoutOwnContent(child));
			Assert.AreEqual(1, child.NumSkins);
		}

		[Test]
		public void ASkinMappedToAnotherOne_Survives()
		{
			var parent = CreateConfig(SkinDefault, SkinLight);
			var child = CreateConfig();
			child.Parent = parent;
			UiStyleEditorUtility.MirrorParentSkins(child);
			child.Skins[1].InheritFromSkinName = SkinDefault;

			Assert.AreEqual(new[] { SkinDefault }, UiStyleEditorUtility.DropSkinsWithoutOwnContent(child));
			Assert.AreEqual(new[] { SkinLight }, child.SkinNames.ToArray());
		}

		/// <summary>
		/// The threshold is how an aspect-ratio skin is SELECTED, not something put into it - counting it as
		/// content would make such a skin impossible to clean up again.
		/// </summary>
		[Test]
		public void AnAspectRatioThreshold_IsNotOwnContent()
		{
			var config = CreateAspectConfig("ProbeAspect");

			Assert.AreEqual(new[] { UiAspectRatioDependentStyleConfig.Landscape,
			                        UiAspectRatioDependentStyleConfig.Portrait },
			                UiStyleEditorUtility.DropSkinsWithoutOwnContent(config));
			Assert.AreEqual(0, config.NumSkins);
		}

		private UiAspectRatioDependentStyleConfig CreateAspectConfig( string _name )
		{
			var config = ScriptableObject.CreateInstance<UiAspectRatioDependentStyleConfig>();
			config.name = _name;
			m_created.Add(config);
			return config;
		}

		private UiStyleConfig CreateConfig( params string[] _skinNames )
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = "ProbeStyleConfig";
			m_created.Add(config);

			var skins = new List<UiSkin>();
			foreach (var skinName in _skinNames)
				skins.Add(new UiSkin(config, skinName));

			config.Skins = skins;
			return config;
		}

		private static UiAbstractStyleBase AddStyle( UiStyleConfig _config, string _skinName, string _styleName )
		{
			var style = new UiStyleImage(_config, _styleName);
			_config.GetOwnSkinByNameOrAlias(_skinName, false).Styles.Add(style);
			return style;
		}
	}
}
