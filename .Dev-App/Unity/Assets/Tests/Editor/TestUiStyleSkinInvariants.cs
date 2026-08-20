using System.Collections.Generic;
using GuiToolkit.Style;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Pins the unwritten invariant of the style config: every skin carries the same set of styles.
	/// Nothing enforces it, but two places rely on it — the config reads the whole style vocabulary off
	/// skins[0] alone, and UiStyleManager pairs the outgoing and incoming skin by index when tweening.
	///
	/// These tests exist because inheritance breaks the invariant on purpose: a child config that stores
	/// only its overrides no longer has every style in every skin. They are the failing-first tests for
	/// that work, and until then they describe what the code currently assumes.
	/// </summary>
	[EditorAware]
	public class TestUiStyleSkinInvariants
	{
		private const string SkinA = "SkinA";
		private const string SkinB = "SkinB";
		private const string SharedStyle = "Test/Shared";
		private const string OnlyInSecondSkin = "Test/OnlyInSecondSkin";

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
		/// The style vocabulary comes from the first skin only, so a style that exists in a later skin
		/// is invisible to every caller that asks the config what styles there are — the style name
		/// dropdowns among them.
		/// </summary>
		[Test]
		public void StyleNames_ComeFromTheFirstSkinOnly()
		{
			var config = CreateConfig(SkinA, SkinB);
			AddStyle(config, SkinA, SharedStyle);
			AddStyle(config, SkinB, SharedStyle);
			AddStyle(config, SkinB, OnlyInSecondSkin);

			var names = config.StyleNames;

			Assert.Contains(SharedStyle, names);
			Assert.IsFalse(names.Contains(OnlyInSecondSkin), "documented limitation, not a wish");
		}

		[Test]
		public void StyleExists_IgnoresStylesOnlyInLaterSkins()
		{
			var config = CreateConfig(SkinA, SkinB);
			AddStyle(config, SkinA, SharedStyle);
			AddStyle(config, SkinB, OnlyInSecondSkin);

			Assert.IsTrue(config.StyleExists(typeof(UiStyleImage), SharedStyle));
			Assert.IsFalse(config.StyleExists(typeof(UiStyleImage), OnlyInSecondSkin));
		}

		/// <summary>
		/// In contrast to the vocabulary methods, this one honours the skin it is asked about — so the
		/// same config answers "which styles exist" and "give me this style" from different sources.
		/// </summary>
		[Test]
		public void GetStyleByName_LooksInTheNamedSkin_NotInTheFirstOne()
		{
			var config = CreateConfig(SkinA, SkinB);
			var styleInB = AddStyle(config, SkinB, OnlyInSecondSkin);

			Assert.AreSame(styleInB, config.GetStyleByName(typeof(Image), SkinB, OnlyInSecondSkin));
			Assert.IsNull(config.GetStyleByName(typeof(Image), SkinA, OnlyInSecondSkin));
		}

		/// <summary>
		/// The invariant itself, checked against every style config in the project rather than against a
		/// fixture — a fixture could only prove that the test builds configs that way. UiStyleManager
		/// pairs skins by index (styles[i] against previousStyles[i]) and asserts equal counts before
		/// tweening, which is only reachable in play mode, so this edit-mode test guards the structural
		/// precondition rather than the tween itself.
		/// </summary>
		[Test]
		public void EverySkin_OfEveryProjectConfig_CarriesTheSameStyleSet()
		{
			var guids = AssetDatabase.FindAssets($"t:{nameof(UiStyleConfig)}");
			Assert.IsNotEmpty(guids, "no style config in the project - this test would prove nothing");

			var checkedConfigs = 0;
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var config = AssetDatabase.LoadAssetAtPath<UiStyleConfig>(path);
				if (config == null || config.NumSkins < 2)
					continue;

				checkedConfigs++;
				var reference = KeysOf(config.Skins[0]);
				for (int i = 1; i < config.NumSkins; i++)
				{
					var keys = KeysOf(config.Skins[i]);
					Assert.AreEqual(reference.Count, keys.Count,
						$"'{path}': skin '{config.Skins[i].Name}' holds a different number of styles " +
						$"than skin '{config.Skins[0].Name}'");
					CollectionAssert.AreEquivalent(reference, keys,
						$"'{path}': skin '{config.Skins[i].Name}' holds different styles than skin " +
						$"'{config.Skins[0].Name}'");
				}
			}

			Assert.Greater(checkedConfigs, 0, "no config with more than one skin was available");
		}

		private static List<int> KeysOf( UiSkin _skin )
		{
			var keys = new List<int>();
			foreach (var style in _skin.Styles)
				keys.Add(style.Key);

			return keys;
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
