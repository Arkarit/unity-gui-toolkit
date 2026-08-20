using System.Collections.Generic;
using System.Linq;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the rows the inspector shows for inherited styles.
	///
	/// The interesting part is where those rows come from: an inherited style lives in ANOTHER asset, so
	/// its SerializedProperty belongs to that asset's SerializedObject, not to the one being edited. That is
	/// what lets the same drawers render it - and it is also why writing to it has to be blocked, which the
	/// style drawer does by disabling the row.
	/// </summary>
	[EditorAware]
	public class TestUiStyleInheritedRows
	{
		private const string SkinDefault = "Default";
		private const string SkinExtra = "Extra";
		private const string ParentStyle = "Test/FromParent";
		private const string SharedStyle = "Test/Shared";
		private const string ChildStyle = "Test/FromChild";

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
		public void WithoutAParent_ThereAreNoInheritedRows()
		{
			var config = CreateConfig(SkinDefault);
			AddStyle(config, SkinDefault, ChildStyle);

			var rows = UiStyleEditorUtility.InheritedStyleProperties(config.GetOwnSkinByNameOrAlias(SkinDefault, false));

			Assert.IsEmpty(rows);
		}

		[Test]
		public void EveryInheritedStyle_GetsARow_FromTheParentsObject()
		{
			var parent = CreateConfig(SkinDefault);
			var first = AddStyle(parent, SkinDefault, ParentStyle);
			var second = AddStyle(parent, SkinDefault, SharedStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			var rows = UiStyleEditorUtility.InheritedStyleProperties(child.GetOwnSkinByNameOrAlias(SkinDefault, false));

			Assert.AreEqual(2, rows.Count);
			foreach (var row in rows)
			{
				Assert.AreSame(parent, row.serializedObject.targetObject,
					"the row belongs to the asset the style lives in - that is what makes it read-only");
			}

			var styles = rows.Select(_r => _r.boxedValue as UiAbstractStyleBase).ToList();
			Assert.Contains(first, styles);
			Assert.Contains(second, styles);
		}

		/// <summary>
		/// An overridden style must appear once, as the child's own row - not twice, and not as the parent's.
		/// </summary>
		[Test]
		public void AnOverriddenStyle_HasNoInheritedRow()
		{
			var parent = CreateConfig(SkinDefault);
			var inherited = AddStyle(parent, SkinDefault, SharedStyle);
			AddStyle(parent, SkinDefault, ParentStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);
			skin.MaterializeStyle(inherited.Key);

			var rows = UiStyleEditorUtility.InheritedStyleProperties(skin);

			Assert.AreEqual(1, rows.Count, "only the style that is still inherited");
			Assert.AreEqual(ParentStyle, (rows[0].boxedValue as UiAbstractStyleBase)?.Name);
		}

		[Test]
		public void RowsFollowTheSkin_ByName()
		{
			// The parent lists its skins the other way round, so an index-based match would cross them.
			var parent = CreateConfig(SkinExtra, SkinDefault);
			var inDefault = AddStyle(parent, SkinDefault, ParentStyle);
			var inExtra = AddStyle(parent, SkinExtra, ParentStyle);

			var child = CreateConfig(SkinDefault, SkinExtra);
			child.Parent = parent;

			var defaultRows = UiStyleEditorUtility.InheritedStyleProperties(child.GetOwnSkinByNameOrAlias(SkinDefault, false));
			var extraRows = UiStyleEditorUtility.InheritedStyleProperties(child.GetOwnSkinByNameOrAlias(SkinExtra, false));

			Assert.AreEqual(1, defaultRows.Count);
			Assert.AreEqual(1, extraRows.Count);
			Assert.AreSame(inDefault, defaultRows[0].boxedValue as UiAbstractStyleBase);
			Assert.AreSame(inExtra, extraRows[0].boxedValue as UiAbstractStyleBase);
		}

		/// <summary>
		/// Rows come from wherever the style actually lives, which is not necessarily the immediate parent.
		/// </summary>
		[Test]
		public void RowsComeFromTheConfigThatActuallyHoldsTheStyle()
		{
			var grandparent = CreateConfig(SkinDefault);
			var inGrandparent = AddStyle(grandparent, SkinDefault, ParentStyle);

			var parent = CreateConfig(SkinDefault);
			parent.Parent = grandparent;
			var inParent = AddStyle(parent, SkinDefault, SharedStyle);

			var child = CreateConfig(SkinDefault);
			child.Parent = parent;

			var rows = UiStyleEditorUtility.InheritedStyleProperties(child.GetOwnSkinByNameOrAlias(SkinDefault, false));

			Assert.AreEqual(2, rows.Count);
			var fromGrandparent = rows.Single(_r => ReferenceEquals(_r.boxedValue, inGrandparent));
			var fromParent = rows.Single(_r => ReferenceEquals(_r.boxedValue, inParent));
			Assert.AreSame(grandparent, fromGrandparent.serializedObject.targetObject);
			Assert.AreSame(parent, fromParent.serializedObject.targetObject);
		}

		/// <summary>
		/// The rows have to follow the parent: a style added there afterwards shows up, one that is
		/// overridden in the meantime disappears from the inherited list.
		/// </summary>
		[Test]
		public void RowsFollowLaterChanges()
		{
			var parent = CreateConfig(SkinDefault);
			var child = CreateConfig(SkinDefault);
			child.Parent = parent;
			var skin = child.GetOwnSkinByNameOrAlias(SkinDefault, false);

			Assert.IsEmpty(UiStyleEditorUtility.InheritedStyleProperties(skin));

			var added = AddStyle(parent, SkinDefault, ParentStyle);
			var rows = UiStyleEditorUtility.InheritedStyleProperties(skin);
			Assert.AreEqual(1, rows.Count);
			Assert.AreSame(added, rows[0].boxedValue as UiAbstractStyleBase);

			skin.MaterializeStyle(added.Key);
			Assert.IsEmpty(UiStyleEditorUtility.InheritedStyleProperties(skin), "now it is the child's own row");
		}

		[Test]
		public void ThePropertyMapCoversEveryStyleOfTheNamedSkin()
		{
			var config = CreateConfig(SkinDefault, SkinExtra);
			var a = AddStyle(config, SkinDefault, ParentStyle);
			var b = AddStyle(config, SkinDefault, SharedStyle);
			AddStyle(config, SkinExtra, ChildStyle);

			var map = UiStyleEditorUtility.StylePropertiesByKey(config, SkinDefault);

			Assert.AreEqual(2, map.Count, "the named skin only");
			Assert.AreSame(a, map[a.Key].boxedValue as UiAbstractStyleBase);
			Assert.AreSame(b, map[b.Key].boxedValue as UiAbstractStyleBase);
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
	}
}
