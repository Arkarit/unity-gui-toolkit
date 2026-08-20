using System.Collections.Generic;
using GuiToolkit.Style;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Characterization tests for how a style is resolved: from an applier that stores nothing but a
	/// name, through the current skin, to the style instance. Written before the style config gains
	/// inheritance, so the change can be made against a description of the current behaviour instead
	/// of against an assumption about it.
	///
	/// Everything here is built in memory. A config is a ScriptableObject that never reaches disk, and
	/// appliers live on GameObjects created per test, so no project asset takes part and nothing has to
	/// be cleaned up on disk. The class is [EditorAware] because the applier's StyleConfig getter
	/// refuses callers that are not (see EditorCallerGate).
	/// </summary>
	[EditorAware]
	public class TestUiStyleResolution
	{
		private const string SkinA = "SkinA";
		private const string SkinB = "SkinB";
		private const string StyleName = "Test/Background";
		private const string OtherStyleName = "Test/Foreground";

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
		public void StyleByKey_ReturnsTheStyle_ForAKnownKey()
		{
			var config = CreateConfig(SkinA);
			var style = AddStyle(config, SkinA, StyleName);

			var resolved = config.GetSkinByName(SkinA).StyleByKey(style.Key);

			Assert.AreSame(style, resolved);
		}

		/// <summary>
		/// The one place a lookup can fail, and therefore the one place a parent fallback belongs.
		/// Returning null rather than throwing is the contract inheritance will build on.
		/// </summary>
		[Test]
		public void StyleByKey_ReturnsNull_ForAnUnknownKey()
		{
			var config = CreateConfig(SkinA);
			AddStyle(config, SkinA, StyleName);

			var unknownKey = UiStyleUtility.GetKey(typeof(Image), "Test/DoesNotExist");

			Assert.IsNull(config.GetSkinByName(SkinA).StyleByKey(unknownKey));
		}

		/// <summary>
		/// A style's identity is component type plus name — never a reference. This is what makes
		/// inheritance cheap: an applier stores only the name, so the same key can be resolved by a
		/// different config without touching any serialized data.
		/// </summary>
		[Test]
		public void Key_IsTheSame_ForTheSameTypeAndName_InDifferentConfigs()
		{
			var configA = CreateConfig(SkinA);
			var configB = CreateConfig(SkinA);

			var styleInA = AddStyle(configA, SkinA, StyleName);
			var styleInB = AddStyle(configB, SkinA, StyleName);

			Assert.AreNotSame(styleInA, styleInB);
			Assert.AreEqual(styleInA.Key, styleInB.Key);
			Assert.AreEqual(UiStyleUtility.GetKey(typeof(Image), StyleName), styleInA.Key);
		}

		[Test]
		public void Key_Differs_ForTheSameNameOnADifferentComponentType()
		{
			var imageKey = UiStyleUtility.GetKey(typeof(Image), StyleName);
			var textKey = UiStyleUtility.GetKey(typeof(Text), StyleName);

			Assert.AreNotEqual(imageKey, textKey);
		}

		/// <summary>
		/// The key dictionary is rebuilt on every lookup while not playing, so a style added after a
		/// first lookup is found immediately. That is the behaviour the editor depends on, and it is
		/// also what makes a lookup cost O(number of styles) instead of O(1) — measured at ~61 us for
		/// 70 styles. Whoever replaces the rebuild with proper invalidation has to keep this test green.
		/// </summary>
		[Test]
		public void StyleByKey_SeesAStyleAddedAfterAnEarlierLookup()
		{
			var config = CreateConfig(SkinA);
			var skin = config.GetSkinByName(SkinA);
			var first = AddStyle(config, SkinA, StyleName);

			Assert.AreSame(first, skin.StyleByKey(first.Key), "sanity: first style resolves");

			var second = AddStyle(config, SkinA, OtherStyleName);

			Assert.AreSame(second, skin.StyleByKey(second.Key));
		}

		[Test]
		public void StyleByName_ResolvesThroughTheComponentType()
		{
			var config = CreateConfig(SkinA);
			var style = AddStyle(config, SkinA, StyleName);
			var skin = config.GetSkinByName(SkinA);

			Assert.AreSame(style, skin.StyleByName<Image>(StyleName));
			Assert.IsNull(skin.StyleByName<Text>(StyleName), "a style is owned by one component type");
		}

		[Test]
		public void GetSkinByName_FindsTheSkin_AndAliasFallsBackToTheName()
		{
			var config = CreateConfig(SkinA, SkinB);

			Assert.IsNotNull(config.GetSkinByName(SkinB));
			Assert.IsNull(config.GetSkinByName("NoSuchSkin"));

			// An unset alias reads as the name, so display code never has to fall back itself.
			Assert.AreEqual(SkinB, config.GetSkinByName(SkinB).Alias);
			Assert.AreSame(config.GetSkinByName(SkinB), config.GetSkinByAlias(SkinB));
		}

		[Test]
		public void CurrentSkin_IsTheFirstSkin_UntilItIsSwitchedByName()
		{
			var config = CreateConfig(SkinA, SkinB);

			Assert.AreEqual(SkinA, config.CurrentSkinName, "the first skin is the default");

			Assert.IsTrue(config.SetCurrentSkinByNameOrAlias(SkinB, false, false));
			Assert.AreEqual(SkinB, config.CurrentSkinName);

			Assert.IsFalse(config.SetCurrentSkinByNameOrAlias("NoSuchSkin", false, false));
			Assert.AreEqual(SkinB, config.CurrentSkinName, "a failed switch leaves the skin alone");
		}

		/// <summary>
		/// End to end: an applier holding only a name resolves the style of the current skin out of the
		/// config it was pointed at.
		/// </summary>
		[Test]
		public void Applier_ResolvesTheStyleOfTheCurrentSkin()
		{
			var config = CreateConfig(SkinA, SkinB);
			var styleInA = AddStyle(config, SkinA, StyleName);
			var styleInB = AddStyle(config, SkinB, StyleName);

			var applier = CreateApplier(config, StyleName);

			Assert.AreSame(styleInA, applier.FindStyle());

			config.SetCurrentSkinByNameOrAlias(SkinB, false, false);

			Assert.AreSame(styleInB, applier.FindStyle(), "the same name resolves per skin");
		}

		[Test]
		public void Applier_ResolvesNull_WhenTheCurrentSkinHasNoSuchStyle()
		{
			var config = CreateConfig(SkinA, SkinB);
			AddStyle(config, SkinA, StyleName);

			var applier = CreateApplier(config, StyleName);
			config.SetCurrentSkinByNameOrAlias(SkinB, false, false);

			// This is exactly the miss that inheritance turns into a parent lookup.
			Assert.IsNull(applier.FindStyle());
		}

		/// <summary>
		/// A fixed skin ignores the config's current skin. Inheritance has to match skins by name for
		/// the same reason this does: the name is the identity, the index is not.
		/// </summary>
		[Test]
		public void Applier_WithAFixedSkin_IgnoresTheCurrentSkin()
		{
			var config = CreateConfig(SkinA, SkinB);
			AddStyle(config, SkinA, StyleName);
			var styleInB = AddStyle(config, SkinB, StyleName);

			var applier = CreateApplier(config, StyleName);
			applier.FixedSkinName = SkinB;

			Assert.IsTrue(applier.SkinIsFixed);
			Assert.AreSame(styleInB, applier.FindStyle());

			config.SetCurrentSkinByNameOrAlias(SkinB, false, false);
			Assert.AreSame(styleInB, applier.FindStyle(), "still the fixed skin, not the current one");
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

		/// <summary>
		/// The per-component config override is serialized and private, so it is assigned the same way
		/// the inspector assigns it. Doing it through SerializedObject keeps the test on the supported
		/// path instead of reaching into the field by reflection.
		/// </summary>
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
