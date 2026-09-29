using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the inspector error about style appliers that write the same property of the same
	/// component.
	///
	/// The case it exists for: a prefab carried two identical Image appliers, and nothing showed it. The
	/// case it must stay quiet for is just as important - two appliers on one component that set different
	/// properties, e.g. one the colour and one the sprite, are a normal setup.
	/// </summary>
	[EditorAware]
	public class TestUiStyleApplierOverlap
	{
		private const string SkinDefault = "Default";

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
		public void TwoAppliersWithTheSameStyle_Overlap()
		{
			var config = CreateConfig();
			var style = AddStyle(config, "Test/Same");
			style.Color.IsApplicable = true;
			style.Sprite.IsApplicable = true;

			var go = CreateGameObject();
			var first = AddApplier(go, config, "Test/Same");
			var second = AddApplier(go, config, "Test/Same");

			var overlaps = UiStyleApplierOverlap.Find(first);

			Assert.AreEqual(1, overlaps.Count);
			Assert.AreSame(second, overlaps[0].Other);
			CollectionAssert.AreEquivalent(new[] { "Color", "Sprite" }, overlaps[0].PropertyNames);
		}

		[Test]
		public void AppliersSettingDifferentProperties_DoNotOverlap()
		{
			var config = CreateConfig();
			AddStyle(config, "Test/ColorOnly").Color.IsApplicable = true;
			AddStyle(config, "Test/SpriteOnly").Sprite.IsApplicable = true;

			var go = CreateGameObject();
			var colorApplier = AddApplier(go, config, "Test/ColorOnly");
			AddApplier(go, config, "Test/SpriteOnly");

			Assert.IsEmpty(UiStyleApplierOverlap.Find(colorApplier));
		}

		[Test]
		public void APartialOverlap_NamesOnlyTheSharedProperties()
		{
			var config = CreateConfig();
			var a = AddStyle(config, "Test/A");
			a.Color.IsApplicable = true;
			a.Sprite.IsApplicable = true;
			var b = AddStyle(config, "Test/B");
			b.Color.IsApplicable = true;
			b.Enabled.IsApplicable = true;

			var go = CreateGameObject();
			var applierA = AddApplier(go, config, "Test/A");
			AddApplier(go, config, "Test/B");

			var overlaps = UiStyleApplierOverlap.Find(applierA);

			Assert.AreEqual(1, overlaps.Count);
			CollectionAssert.AreEquivalent(new[] { "Color" }, overlaps[0].PropertyNames);
		}

		[Test]
		public void ADisabledApplier_DoesNotCount()
		{
			var config = CreateConfig();
			AddStyle(config, "Test/Same").Color.IsApplicable = true;

			var go = CreateGameObject();
			var first = AddApplier(go, config, "Test/Same");
			var second = AddApplier(go, config, "Test/Same");

			// Swapping between two appliers by enabling one of them is legitimate, and a disabled one
			// applies nothing.
			second.enabled = false;
			Assert.IsEmpty(UiStyleApplierOverlap.Find(first), "the other one is disabled");
			Assert.IsEmpty(UiStyleApplierOverlap.Find(second), "this one is disabled");
		}

		[Test]
		public void ASingleApplier_HasNoOverlap()
		{
			var config = CreateConfig();
			AddStyle(config, "Test/Same").Color.IsApplicable = true;

			var go = CreateGameObject();
			var applier = AddApplier(go, config, "Test/Same");

			Assert.IsEmpty(UiStyleApplierOverlap.Find(applier));
		}

		private UiStyleConfig CreateConfig()
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = "TestStyleConfig";
			m_created.Add(config);
			config.Skins = new List<UiSkin> { new UiSkin(config, SkinDefault) };
			return config;
		}

		private static UiStyleImage AddStyle( UiStyleConfig _config, string _styleName )
		{
			var style = new UiStyleImage(_config, _styleName);
			_config.GetOwnSkinByNameOrAlias(SkinDefault, false).Styles.Add(style);
			return style;
		}

		private GameObject CreateGameObject()
		{
			var gameObject = new GameObject("StyleApplierOverlapUnderTest");
			m_created.Add(gameObject);
			gameObject.AddComponent<Image>();
			return gameObject;
		}

		private static UiApplyStyleImage AddApplier( GameObject _gameObject, UiStyleConfig _config, string _styleName )
		{
			var applier = _gameObject.AddComponent<UiApplyStyleImage>();

			var serializedApplier = new SerializedObject(applier);
			serializedApplier.FindProperty("m_optionalStyleConfig").objectReferenceValue = _config;
			serializedApplier.ApplyModifiedPropertiesWithoutUndo();

			applier.Name = _styleName;
			return applier;
		}
	}
}
