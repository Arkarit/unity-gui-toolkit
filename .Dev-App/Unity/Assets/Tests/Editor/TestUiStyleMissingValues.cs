using System.Collections.Generic;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Guards the case where a style type gains a value and every config already on disk is left without it.
	///
	/// Style values are [SerializeReference] fields, and Unity does NOT run a newly added field's
	/// initialiser for data it loads - so the moment the generator adds one, every serialised config holds
	/// a managed reference pointing at nothing. The object side hides this well: its generated getters
	/// create what they need on access, so everything that goes through the style object works. Only the
	/// inspector, which walks the serialised data instead, ever sees the null.
	///
	/// It cost a day, because the way it showed was not an exception: the throw happened inside a foldout's
	/// content, which skipped the line that stores the foldout's new state, and the skin drawer swallowed it
	/// whole. The visible symptom was style definitions that would not open. Nothing in the console.
	///
	/// Three things are pinned here, in the order they matter: no config in this project carries a missing
	/// value, a missing value cannot take the inspector down again, and the repair puts one back.
	/// </summary>
	[EditorAware]
	public class TestUiStyleMissingValues
	{
		private const string SkinDefault = "Default";
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

		/// <summary>
		/// The one that would have caught it: every config shipped in this project has every value it
		/// declares. A config that fails this needs opening once - the editor repairs it - and saving.
		/// </summary>
		[Test]
		public void NoConfigInThisProjectHasAMissingValue()
		{
			var guids = AssetDatabase.FindAssets("t:UiStyleConfig");
			Assert.That(guids, Is.Not.Empty, "No UiStyleConfig in the project - this test would pass blindly.");

			var complaints = new List<string>();

			foreach (var guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var config = AssetDatabase.LoadAssetAtPath<UiStyleConfig>(path);
				if (config == null)
					continue;

				foreach (string missing in MissingValuePaths(config))
					complaints.Add($"{path}: {missing}");
			}

			Assert.That(complaints, Is.Empty,
				"Style values that were never created:\n" + string.Join("\n", complaints));
		}

		/// <summary>
		/// Asking a missing value for its height used to throw a NullReferenceException, which is what took
		/// the surrounding inspector with it. It has to answer instead, whatever it answers.
		/// </summary>
		[Test]
		public void AMissingValueDoesNotThrowWhenMeasured()
		{
			var config = CreateConfigWithStyle(out var valueProp);
			valueProp.managedReferenceValue = null;
			valueProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

			float height = 0;
			Assert.DoesNotThrow(() => height = EditorGUI.GetPropertyHeight(valueProp, GUIContent.none, true));
			Assert.That(height, Is.GreaterThan(0), "A missing value still occupies a row.");
		}

		[Test]
		public void RepairFillsInAMissingValue()
		{
			var config = CreateConfigWithStyle(out var valueProp);
			valueProp.managedReferenceValue = null;
			valueProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

			Assert.That(MissingValuePaths(config), Is.Not.Empty, "The value was supposed to be missing now.");
			Assert.That(UiStyleEditorUtility.RepairMissingStyleValues(config), Is.True,
				"The repair should report that it had something to do.");
			Assert.That(MissingValuePaths(config), Is.Empty);
		}

		/// <summary>Second call has nothing to do, and says so - or every config anyone opens is dirtied.</summary>
		[Test]
		public void RepairDoesNothingWhenNothingIsMissing()
		{
			var config = CreateConfigWithStyle(out _);
			UiStyleEditorUtility.RepairMissingStyleValues(config);

			Assert.That(UiStyleEditorUtility.RepairMissingStyleValues(config), Is.False);
		}

		private UiStyleConfig CreateConfigWithStyle( out SerializedProperty valueProp )
		{
			var config = ScriptableObject.CreateInstance<UiStyleConfig>();
			config.name = "TestStyleConfig";
			m_created.Add(config);
			config.Skins = new List<UiSkin> { new UiSkin(config, SkinDefault) };

			var style = new UiStyleImage(config, StyleName);
			config.GetSkinByName(SkinDefault).Styles.Add(style);

			var serializedObject = new SerializedObject(config);
			var styleProp = serializedObject
				.FindProperty("m_skins")
				.GetArrayElementAtIndex(0)
				.FindPropertyRelative("m_styles")
				.GetArrayElementAtIndex(0);

			valueProp = FirstValueProperty(styleProp);
			Assert.That(valueProp, Is.Not.Null, "The test style has no value to take away.");
			return config;
		}

		private static SerializedProperty FirstValueProperty( SerializedProperty _styleProp )
		{
			foreach (var child in OwnChildren(_styleProp))
			{
				if (child.propertyType == SerializedPropertyType.ManagedReference)
					return child;
			}

			return null;
		}

		/// <summary>Every value of every style of every skin that points at nothing, named for a message.</summary>
		private static List<string> MissingValuePaths( UiStyleConfig _config )
		{
			var result = new List<string>();
			var serializedObject = new SerializedObject(_config);
			var skinsProp = serializedObject.FindProperty("m_skins");

			for (int i = 0; i < skinsProp.arraySize; i++)
			{
				var skinProp = skinsProp.GetArrayElementAtIndex(i);
				var stylesProp = skinProp.FindPropertyRelative("m_styles");
				if (stylesProp == null)
					continue;

				for (int j = 0; j < stylesProp.arraySize; j++)
				{
					var styleProp = stylesProp.GetArrayElementAtIndex(j);
					if (styleProp.managedReferenceValue is not UiAbstractStyleBase style)
						continue;

					foreach (var child in OwnChildren(styleProp))
					{
						if (child.propertyType == SerializedPropertyType.ManagedReference
						 && child.managedReferenceValue == null)
						{
							result.Add($"skin '{skinProp.FindPropertyRelative("m_name").stringValue}', "
								+ $"style '{style.Alias}', value '{child.name}'");
						}
					}
				}
			}

			return result;
		}

		/// <summary>The property's own fields - not what is inside them.</summary>
		private static IEnumerable<SerializedProperty> OwnChildren( SerializedProperty _property )
		{
			var iterator = _property.Copy();
			var end = _property.GetEndProperty();
			int depth = _property.depth;

			while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
			{
				if (iterator.depth == depth + 1)
					yield return iterator.Copy();
			}
		}
	}
}
