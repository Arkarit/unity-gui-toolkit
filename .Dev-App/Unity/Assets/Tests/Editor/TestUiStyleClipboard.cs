using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Copying one style's values and pasting them into another.
	///
	/// The clipboard is the real system one, because that is what lets a copy survive a script recompile
	/// and cross into another project - so these tests put back whatever was in it, or a test run would
	/// quietly take away what someone had copied a moment before.
	/// </summary>
	[EditorAware]
	public class TestUiStyleClipboard
	{
		private const string SkinDefault = "Default";

		private readonly List<Object> m_created = new();
		private string m_clipboardBefore;

		[SetUp]
		public void SetUp() => m_clipboardBefore = GUIUtility.systemCopyBuffer;

		[TearDown]
		public void TearDown()
		{
			GUIUtility.systemCopyBuffer = m_clipboardBefore;

			foreach (var obj in m_created)
			{
				if (obj != null)
					Object.DestroyImmediate(obj);
			}

			m_created.Clear();
		}

		[Test]
		public void ValuesSurviveTheRoundTrip()
		{
			var config = CreateConfig(out var source, out var target);

			// Something to actually transfer - an empty style pasting cleanly onto an empty one would pass
			// this test without proving anything.
			source.Color.IsApplicable = true;
			source.Color.Value = new Color(0.25f, 0.5f, 0.75f, 0.5f);
			Flush(config);

			Assert.That(Snapshot(config, 1), Is.Not.EqualTo(Snapshot(config, 0)), "Nothing to copy.");

			UiStyleClipboard.Copy(StyleProperty(config, 0), source);
			int written = UiStyleClipboard.Paste(StyleProperty(config, 1), target);

			Assert.That(written, Is.GreaterThan(0));
			Assert.That(Snapshot(config, 1), Is.EqualTo(Snapshot(config, 0)));
		}

		[Test]
		public void AnAssetReferenceSurvivesAsAGuid()
		{
			var material = FirstAsset<Material>();
			if (material == null)
				Assert.Ignore("No material in this project to reference.");

			var config = CreateConfig(out var source, out var target);
			source.Material.IsApplicable = true;
			source.Material.Value = material;
			Flush(config);

			UiStyleClipboard.Copy(StyleProperty(config, 0), source);

			// The text really is text - it cannot be carrying the object itself.
			Assert.That(GUIUtility.systemCopyBuffer, Does.Contain(AssetDatabase.AssetPathToGUID(
				AssetDatabase.GetAssetPath(material))));

			UiStyleClipboard.Paste(StyleProperty(config, 1), target);
			Assert.That(target.Material.Value, Is.EqualTo(material));
		}

		[Test]
		public void ADifferentStyleTypeIsRefusedWithAReason()
		{
			var config = CreateConfig(out var source, out _);
			var otherType = new UiStyleText(config, "Test/Text");
			config.GetSkinByName(SkinDefault).Styles.Add(otherType);
			Flush(config);

			UiStyleClipboard.Copy(StyleProperty(config, 0), source);

			Assert.That(UiStyleClipboard.CanPasteInto(otherType, out string reason), Is.False);
			Assert.That(reason, Is.Not.Null.And.Not.Empty);
		}

		/// <summary>
		/// A clipboard holds whatever was last copied anywhere. None of it may throw, and none of it may
		/// read as a style.
		/// </summary>
		[Test]
		public void SomethingElseInTheClipboardIsNotAStyle()
		{
			foreach (string junk in new[] { "", "   ", "C:/some/path.txt", "{", "{\"a\":1}", "not json at all" })
			{
				GUIUtility.systemCopyBuffer = junk;
				Assert.DoesNotThrow(() => UiStyleClipboard.Peek(), $"on '{junk}'");
				Assert.That(UiStyleClipboard.Peek().IsValid, Is.False, $"on '{junk}'");
			}
		}

		/// <summary>
		/// The case that made name matching necessary: a style type that gained a value since the copy was
		/// taken keeps it, rather than having it cleared by a document that never knew about it.
		/// </summary>
		[Test]
		public void AValueTheClipboardDoesNotKnowIsLeftAlone()
		{
			var config = CreateConfig(out var source, out var target);
			source.Color.IsApplicable = true;
			source.Color.Value = Color.red;
			target.Radius.IsApplicable = true;
			target.Radius.Value = 42f;
			Flush(config);

			UiStyleClipboard.Copy(StyleProperty(config, 0), source);

			// Same effect as a value that did not exist when the copy was made.
			GUIUtility.systemCopyBuffer = RemoveEntry(GUIUtility.systemCopyBuffer, "m_Radius");

			UiStyleClipboard.Paste(StyleProperty(config, 1), target);

			Assert.That(target.Color.Value, Is.EqualTo(Color.red), "The known value should have arrived.");
			Assert.That(target.Radius.Value, Is.EqualTo(42f), "The unknown one should have been left alone.");
			Assert.That(target.Radius.IsApplicable, Is.True);
		}

		[Test]
		public void APackageOwnedConfigIsNotWritable()
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			m_created.Add(config);

			// An in-memory config has no asset path at all, which is the "not in a package" case.
			Assert.That(UiStyleEditorUtility.IsWritable(config, out _), Is.True);
			Assert.That(UiStyleEditorUtility.IsWritable(null, out string reason), Is.False);
			Assert.That(reason, Is.Not.Null.And.Not.Empty);
		}


		/// <summary>
		/// The row a menu entry works on must be the one in the CONFIG, not the property it happened to be
		/// drawn through.
		///
		/// An applier inspector draws its style through a shared throwaway ScriptableObject, and going by
		/// the drawn property there reads back a fraction of the values and writes them somewhere the config
		/// never sees - a paste that logs success and changes nothing. This reaches into a private type on
		/// purpose: the resolution order is exactly where the bug lived, and testing it anywhere else would
		/// test something that never broke.
		/// </summary>
		[Test]
		public void ARowResolvesThroughItsConfigNotThroughTheDrawnProperty()
		{
			var config = CreateConfig(out var source, out var target);
			source.Color.IsApplicable = true;
			source.Color.Value = Color.red;
			Flush(config);

			UiStyleClipboard.Copy(
				UiStyleEditorUtility.StylePropertiesByKey(config, SkinDefault)[source.Key], source);

			// No fallback at all, so only the config route can work.
			var rowTargetType = typeof(UiAbstractStyleBaseDrawer).GetNestedType("RowTarget",
				BindingFlags.NonPublic);
			Assert.That(rowTargetType, Is.Not.Null, "RowTarget was renamed - update this test.");

			var rowTarget = System.Activator.CreateInstance(rowTargetType,
				new object[] { config, SkinDefault, target.Key, null, null });

			var menu = (GenericMenu)typeof(UiAbstractStyleBaseDrawer)
				.GetMethod("BuildRowMenu", BindingFlags.Static | BindingFlags.NonPublic)
				.Invoke(null, new object[] { target, config, rowTarget, false, false });

			Assert.That(Invoke(menu, "Paste Values"), Is.True, "Paste was not offered or was disabled.");
			Assert.That(target.Color.Value, Is.EqualTo(Color.red));
		}

		/// <summary>Fires the named entry of a menu. False when it is absent or greyed out.</summary>
		private static bool Invoke( GenericMenu _menu, string _label )
		{
			var items = (IEnumerable)typeof(GenericMenu)
				.GetField("m_MenuItems", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_menu);

			foreach (var item in items)
			{
				var type = item.GetType();
				const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
				var content = (GUIContent)type.GetField("content", any).GetValue(item);
				if (content.text != _label)
					continue;

				if (type.GetField("func", any).GetValue(item) is not GenericMenu.MenuFunction func)
					return false;

				func.Invoke();
				return true;
			}

			return false;
		}

		#region Helpers

		private UiStyleConfig CreateConfig( out UiStyleUiRoundedImage _source, out UiStyleUiRoundedImage _target )
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = "TestStyleConfig";
			m_created.Add(config);
			config.Skins = new List<UiSkin> { new UiSkin(config, SkinDefault) };

			_source = new UiStyleUiRoundedImage(config, "Test/Source");
			_target = new UiStyleUiRoundedImage(config, "Test/Target");
			config.GetSkinByName(SkinDefault).Styles.Add(_source);
			config.GetSkinByName(SkinDefault).Styles.Add(_target);
			return config;
		}

		/// <summary>
		/// Pushes what was set on the objects into their serialised form. The clipboard reads through
		/// SerializedProperty, so a value only set on the object would not be there yet.
		/// </summary>
		private static void Flush( UiStyleConfig _config )
		{
			var serializedObject = new SerializedObject(_config);
			serializedObject.Update();
			serializedObject.ApplyModifiedPropertiesWithoutUndo();
		}

		private static SerializedProperty StyleProperty( UiStyleConfig _config, int _index )
			=> new SerializedObject(_config)
				.FindProperty("m_skins")
				.GetArrayElementAtIndex(0)
				.FindPropertyRelative("m_styles")
				.GetArrayElementAtIndex(_index);

		private static string Snapshot( UiStyleConfig _config, int _index )
		{
			var style = _config.Skins[0].Styles[_index];
			var sb = new StringBuilder();

			foreach (var value in style.Values)
			{
				sb.Append(value.IsApplicable).Append(':')
				  .Append(value.RawValueObj?.ToString() ?? "null").Append(';');
			}

			return sb.ToString();
		}

		private static T FirstAsset<T>() where T : Object
		{
			foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
			{
				var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null)
					return asset;
			}

			return null;
		}

		/// <summary>
		/// Cuts one value out of the copied document, the crude way - the point is to produce a document
		/// that does not mention a name, not to be a JSON editor.
		/// </summary>
		private static string RemoveEntry( string _json, string _name )
		{
			var root = Newtonsoft.Json.Linq.JObject.Parse(_json);
			((Newtonsoft.Json.Linq.JObject)root["values"]).Remove(_name);
			return root.ToString();
		}

		#endregion
	}
}
