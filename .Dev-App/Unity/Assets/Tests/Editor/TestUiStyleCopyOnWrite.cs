using System.Collections.Generic;
using GuiToolkit.Style;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for copy-on-write: writing to an inherited style has to copy it into the config doing the
	/// writing first.
	///
	/// This is not a nicety. A resolved inherited style IS the parent's instance - styles are
	/// [SerializeReference] objects inside their config asset, and resolution hands out the real one. So a
	/// write reaches the parent, and if the parent is the copy shipped inside the package, the save is
	/// dropped without an error and the change is gone after the next reload.
	/// </summary>
	[EditorAware]
	public class TestUiStyleCopyOnWrite
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
		public void AnInheritedStyle_IsNotOwned_UntilItIsMaterialized()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);

			Assert.IsFalse(skin.OwnsStyle(inherited.Key), "it resolves, but it belongs to the parent");
			Assert.AreSame(inherited, skin.StyleByKey(inherited.Key));

			var own = skin.MaterializeStyle(inherited.Key);

			Assert.IsTrue(skin.OwnsStyle(inherited.Key));
			Assert.AreNotSame(inherited, own, "a copy, not the parent's instance");
			Assert.AreSame(own, skin.StyleByKey(inherited.Key), "and resolution finds the copy from now on");
		}

		[Test]
		public void MaterializingCarriesTheValuesOver_AndLeavesTheParentAlone()
		{
			var (parent, child) = CreatePair();
			var inherited = (UiStyleImage) AddStyle(parent, SkinDefault, InheritedStyle);
			inherited.Color.RawValue = Color.red;
			inherited.Color.IsApplicable = true;
			inherited.Enabled.IsApplicable = false;

			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);
			var own = (UiStyleImage) skin.MaterializeStyle(inherited.Key);

			Assert.AreEqual(Color.red, own.Color.RawValue, "the copy starts where the inherited style was");
			Assert.IsTrue(own.Color.IsApplicable, "including which values are switched on");
			Assert.IsFalse(own.Enabled.IsApplicable);

			// The point of the copy: writing to it must not reach the parent.
			own.Color.RawValue = Color.green;

			Assert.AreEqual(Color.green, own.Color.RawValue);
			Assert.AreEqual(Color.red, inherited.Color.RawValue, "the parent is untouched");
		}

		[Test]
		public void MaterializingTwice_KeepsOneCopy()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);

			var first = skin.MaterializeStyle(inherited.Key);
			var second = skin.MaterializeStyle(inherited.Key);

			Assert.AreSame(first, second, "the second call finds the style it already owns");
			Assert.AreEqual(1, skin.Styles.Count);
		}

		[Test]
		public void MaterializingAnUnknownStyle_YieldsNothing()
		{
			var (parent, child) = CreatePair();
			AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);

			Assert.IsNull(skin.MaterializeStyle(UiStyleUtility.GetKey(typeof(Image), "Test/NobodyHasThis")));
			Assert.IsEmpty(skin.Styles);
		}

		/// <summary>
		/// An override is per skin - that is the whole point of storing only what differs. Overriding a
		/// style in one skin must not quietly pin it in the others.
		/// </summary>
		[Test]
		public void MaterializingInOneSkin_LeavesTheOtherSkinsInheriting()
		{
			var parent = CreateConfig(SkinDefault, SkinExtra);
			var inDefault = AddStyle(parent, SkinDefault, InheritedStyle);
			var inExtra = AddStyle(parent, SkinExtra, InheritedStyle);

			var child = CreateConfig(SkinDefault, SkinExtra);
			child.Parent = parent;

			var defaultSkin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);
			var extraSkin = child.GetOwnSkinByNameOrAlias(SkinExtra, false);

			var own = defaultSkin.MaterializeStyle(inDefault.Key);

			Assert.IsTrue(defaultSkin.OwnsStyle(inDefault.Key));
			Assert.IsFalse(extraSkin.OwnsStyle(inExtra.Key), "the other skin still inherits");
			Assert.AreSame(own, defaultSkin.StyleByKey(inDefault.Key));
			Assert.AreSame(inExtra, extraSkin.StyleByKey(inExtra.Key));
		}

		[Test]
		public void TheCopyKeepsTheNameAndTheComponentType()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);

			var own = skin.MaterializeStyle(inherited.Key);

			Assert.AreEqual(inherited.Name, own.Name);
			Assert.AreEqual(inherited.Key, own.Key, "same identity, or nothing would resolve to it");
			Assert.AreEqual(inherited.SupportedComponentType, own.SupportedComponentType);
			Assert.AreEqual(inherited.GetType(), own.GetType());
			Assert.AreSame(child, own.StyleConfig, "and it belongs to the child now");
		}

		/// <summary>
		/// What an applier does when its style is recorded, without the recording itself - Record saves the
		/// whole project, which a test must not do.
		/// </summary>
		[Test]
		public void AnApplier_MaterializesItsStyle_BeforeItIsWrittenTo()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, SkinDefault, InheritedStyle);

			var applier = CreateApplier(child, InheritedStyle);
			Assert.AreSame(inherited, applier.FindStyle(), "it starts out resolving the parent's style");

			var own = applier.MaterializeStyleForOverride();

			Assert.IsNotNull(own);
			Assert.AreNotSame(inherited, own);
			Assert.AreSame(own, applier.Style, "and the applier points at the copy afterwards");
			Assert.IsTrue(child.GetOwnSkinByNameOrAlias(SkinDefault, false).OwnsStyle(inherited.Key));
		}

		[Test]
		public void AnApplier_WithAnOwnStyle_ChangesNothing()
		{
			var (parent, child) = CreatePair();
			AddStyle(parent, SkinDefault, InheritedStyle);
			var ownBefore = AddStyle(child, SkinDefault, InheritedStyle);

			var applier = CreateApplier(child, InheritedStyle);
			var own = applier.MaterializeStyleForOverride();

			Assert.AreSame(ownBefore, own);
			Assert.AreEqual(1, child.GetOwnSkinByNameOrAlias(SkinDefault, false).Styles.Count);
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

		private static UiAbstractStyleBase AddStyle( UiStyleConfig _config, string _skinName, string _styleName )
		{
			var style = new UiStyleImage(_config, _styleName);
			_config.GetOwnSkinByNameOrAlias(_skinName, false).Styles.Add(style);
			return style;
		}

		private UiAbstractApplyStyleBase CreateApplier( UiStyleConfig _config, string _styleName )
		{
			var gameObject = new GameObject("StyleApplierUnderTest");
			m_created.Add(gameObject);

			gameObject.AddComponent<Image>();
			var applier = gameObject.AddComponent<UiApplyStyleImage>();

			var serializedApplier = new SerializedObject(applier);
			serializedApplier.FindProperty("m_optionalStyleConfig").objectReferenceValue = _config;
			serializedApplier.ApplyModifiedPropertiesWithoutUndo();

			applier.Name = _styleName;
			return applier;
		}
	}
}
