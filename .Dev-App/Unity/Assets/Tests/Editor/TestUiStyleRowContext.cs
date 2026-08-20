using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the context a style row is drawn in.
	///
	/// It exists because a row cannot answer "whose style is this?" from its own SerializedProperty. In a
	/// config inspector an inherited row belongs to the parent asset, and in an applier inspector the style
	/// is drawn through a throwaway helper object, so the property names neither the right config nor the
	/// right skin. Both editors state the two facts instead, and everything else follows.
	/// </summary>
	[EditorAware]
	public class TestUiStyleRowContext
	{
		private const string SkinDefault = "Default";
		private const string InheritedStyle = "Test/Inherited";
		private const string OwnStyle = "Test/Own";

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
		/// The safe default: with no context, nothing is inherited and nothing is an override - so a style
		/// drawn somewhere nobody thought about stays editable rather than being silently locked.
		/// </summary>
		[Test]
		public void WithoutAContext_NothingIsInherited()
		{
			var parent = CreateConfig();
			var style = AddStyle(parent, InheritedStyle);

			Assert.IsNull(UiStyleRowContext.Config);
			Assert.IsNull(UiStyleRowContext.Skin);
			Assert.IsFalse(UiStyleRowContext.IsInherited(style));
			Assert.IsFalse(UiStyleRowContext.IsOverride(style));
			Assert.IsNull(UiStyleRowContext.OwnerOf(style));
		}

		[Test]
		public void AStyleOfTheEditedConfig_IsNotInherited()
		{
			var (parent, child) = CreatePair();
			AddStyle(parent, InheritedStyle);
			var own = AddStyle(child, OwnStyle);

			using (UiStyleRowContext.Use(child, Skin(child)))
			{
				Assert.IsFalse(UiStyleRowContext.IsInherited(own));
				Assert.AreSame(child, UiStyleRowContext.OwnerOf(own));
				Assert.IsFalse(UiStyleRowContext.IsOverride(own), "nothing above it has this style");
			}
		}

		[Test]
		public void AStyleOfTheParent_IsInherited_AndNamesItsOwner()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, InheritedStyle);

			using (UiStyleRowContext.Use(child, Skin(child)))
			{
				Assert.IsTrue(UiStyleRowContext.IsInherited(inherited));
				Assert.AreSame(parent, UiStyleRowContext.OwnerOf(inherited));
				Assert.IsFalse(UiStyleRowContext.IsOverride(inherited), "it is not overridden, it IS the source");
			}
		}

		[Test]
		public void AnOverride_IsOwnAndInheritedAtOnce()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, InheritedStyle);
			var skin = Skin(child);
			skin.MaterializeStyle(inherited.Key);

			using (UiStyleRowContext.Use(child, skin))
			{
				var own = skin.StyleByKey(inherited.Key);
				Assert.IsFalse(UiStyleRowContext.IsInherited(own), "it belongs to the edited config now");
				Assert.IsTrue(UiStyleRowContext.IsOverride(own), "and it still has something to fall back to");
			}
		}

		/// <summary>
		/// Seen from the parent's own inspector, the same style is neither inherited nor an override - which
		/// is why the context has to be restored rather than left standing.
		/// </summary>
		[Test]
		public void TheContextIsRestored_WhenItsScopeEnds()
		{
			var (parent, child) = CreatePair();
			var inherited = AddStyle(parent, InheritedStyle);

			using (UiStyleRowContext.Use(parent, Skin(parent)))
			{
				Assert.IsFalse(UiStyleRowContext.IsInherited(inherited));

				using (UiStyleRowContext.Use(child, Skin(child)))
				{
					Assert.IsTrue(UiStyleRowContext.IsInherited(inherited));
					Assert.AreSame(child, UiStyleRowContext.Config);
				}

				Assert.AreSame(parent, UiStyleRowContext.Config, "the inner scope put the outer one back");
				Assert.IsFalse(UiStyleRowContext.IsInherited(inherited));
			}

			Assert.IsNull(UiStyleRowContext.Config, "and the outer scope left nothing behind");
		}

		/// <summary>
		/// The applier case: the config is known, the skin is the one the applier resolves through, and the
		/// style's own back-reference - null for most package styles - is never consulted.
		/// </summary>
		[Test]
		public void AStyleWithoutABackReference_IsStillPlacedCorrectly()
		{
			var (parent, child) = CreatePair();

			// Exactly the state 64 of 70 styles in the shipped config are in.
			var orphan = new UiStyleImage(null, InheritedStyle);
			Skin(parent).Styles.Add(orphan);

			using (UiStyleRowContext.Use(child, Skin(child)))
			{
				Assert.IsNull(orphan.StyleConfig, "sanity: it does not know its own config");
				Assert.IsTrue(UiStyleRowContext.IsInherited(orphan), "and it does not need to");
				Assert.AreSame(parent, UiStyleRowContext.OwnerOf(orphan));
			}
		}

		private static UiSkin Skin( UiStyleConfig _config ) => _config.GetOwnSkinByNameOrAlias(SkinDefault, false);

		private (UiStyleConfig parent, UiStyleConfig child) CreatePair()
		{
			var parent = CreateConfig();
			var child = CreateConfig();
			child.Parent = parent;
			return (parent, child);
		}

		private UiStyleConfig CreateConfig()
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = "TestStyleConfig";
			m_created.Add(config);
			config.Skins = new List<UiSkin> { new UiSkin(config, SkinDefault) };
			return config;
		}

		private static UiAbstractStyleBase AddStyle( UiStyleConfig _config, string _styleName )
		{
			var style = new UiStyleImage(_config, _styleName);
			Skin(_config).Styles.Add(style);
			return style;
		}
	}
}
