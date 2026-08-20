using System.Collections.Generic;
using GuiToolkit.Style;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Characterization tests for the write side of the style config: the event handlers that rename and
	/// delete, and the identity contract they depend on. This is the half that inheritance changes most,
	/// because a write to an inherited style will have to materialise it in the child config first.
	///
	/// Deliberately not covered here: UiSkin.Validate's repair branch and
	/// UiAbstractApplyStyleBase.Record both call AssetDatabase.SaveAssets(), i.e. a project-wide save.
	/// A test must not do that, so the repair branch is described by the test below that pins the
	/// harmless case instead. That those two write paths save the whole project is a finding about the
	/// production code, not a gap in the tests.
	/// </summary>
	[EditorAware]
	public class TestUiStyleWritePaths
	{
		private const string SkinA = "SkinA";
		private const string SkinB = "SkinB";
		private const string StyleName = "Test/Background";
		private const string NewAlias = "Renamed For Display";

		private readonly List<Object> m_created = new();

		[SetUp]
		public void SetUp()
		{
			// A config registers its event listeners from OnEnable, deferred through the AssetReadyGate.
			// If the gate happens to be closed the handlers under test are simply not subscribed yet, and
			// every assertion here would fail for a reason that has nothing to do with the write paths.
			if (!AssetReadyGate.Ready)
				Assert.Ignore("AssetReadyGate is closed (editor importing or compiling)");
		}

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
		/// A style alias is a display name for one style across all skins, so renaming it in one skin
		/// renames it everywhere. The handler matches by key, which is component type plus name.
		/// </summary>
		[Test]
		public void SetStyleAlias_Event_RenamesTheStyleInEverySkin()
		{
			var config = CreateConfig(SkinA, SkinB);
			var styleInA = AddStyle(config, SkinA, StyleName);
			var styleInB = AddStyle(config, SkinB, StyleName);

			UiEventDefinitions.EvSetStyleAlias.InvokeAlways(config, styleInA, NewAlias);

			Assert.AreEqual(NewAlias, styleInA.Alias);
			Assert.AreEqual(NewAlias, styleInB.Alias, "the alias belongs to the style, not to the skin");
		}

		[Test]
		public void SetStyleAlias_Event_IgnoresAnotherConfig()
		{
			var config = CreateConfig(SkinA);
			var otherConfig = CreateConfig(SkinA);
			var style = AddStyle(config, SkinA, StyleName);

			UiEventDefinitions.EvSetStyleAlias.InvokeAlways(otherConfig, style, NewAlias);

			Assert.AreEqual(StyleName, style.Alias, "an unset alias reads as the name");
		}

		[Test]
		public void SetSkinAlias_Event_RenamesTheSkin()
		{
			var config = CreateConfig(SkinA, SkinB);

			UiEventDefinitions.EvSetSkinAlias.InvokeAlways(config, config.GetSkinByName(SkinB), NewAlias);

			Assert.AreEqual(NewAlias, config.GetSkinByName(SkinB).Alias);
			Assert.AreEqual(SkinA, config.GetSkinByName(SkinA).Alias, "only the named skin is renamed");
			Assert.AreEqual(SkinB, config.GetSkinByName(SkinB).Name, "the identifier never changes");
		}

		/// <summary>
		/// The handler matches skins by name rather than by instance, and it has to: a UiSkin is a plain
		/// [Serializable] class, so SerializedProperty.boxedValue hands out a fresh copy of it on every
		/// access, and a property drawer editing the skin list holds exactly such a copy. Passing that
		/// copy must still rename the skin in the config. Pinning this keeps the name matching from being
		/// "simplified" into a reference comparison later.
		/// </summary>
		[Test]
		public void SetSkinAlias_Event_AcceptsADetachedCopyOfTheSkin()
		{
			var config = CreateConfig(SkinA, SkinB);

			var serializedConfig = new SerializedObject(config);
			var element = serializedConfig.FindProperty("m_skins").GetArrayElementAtIndex(1);
			var detachedCopy = element.boxedValue as UiSkin;

			Assert.IsNotNull(detachedCopy);
			Assert.AreNotSame(config.Skins[1], detachedCopy, "boxedValue copies a plain serializable class");
			Assert.AreEqual(SkinB, detachedCopy.Name);

			UiEventDefinitions.EvSetSkinAlias.InvokeAlways(config, detachedCopy, NewAlias);

			Assert.AreEqual(NewAlias, config.GetSkinByName(SkinB).Alias);
		}

		[Test]
		public void DeleteStyle_Event_RemovesTheStyleFromEverySkin()
		{
			var config = CreateConfig(SkinA, SkinB);
			var styleInA = AddStyle(config, SkinA, StyleName);
			AddStyle(config, SkinB, StyleName);

			UiEventDefinitions.EvDeleteStyle.InvokeAlways(config, styleInA);

			Assert.IsEmpty(config.GetSkinByName(SkinA).Styles);
			Assert.IsEmpty(config.GetSkinByName(SkinB).Styles, "a style is deleted from the whole config");
		}

		[Test]
		public void DeleteStyle_Event_LeavesOtherStylesAlone()
		{
			var config = CreateConfig(SkinA);
			var doomed = AddStyle(config, SkinA, StyleName);
			var survivor = AddStyle(config, SkinA, "Test/Survivor");

			UiEventDefinitions.EvDeleteStyle.InvokeAlways(config, doomed);

			var remaining = config.GetSkinByName(SkinA).Styles;
			Assert.AreEqual(1, remaining.Count);
			Assert.AreSame(survivor, remaining[0]);
			Assert.AreSame(survivor, config.GetSkinByName(SkinA).StyleByKey(survivor.Key),
				"the key dictionary follows the deletion");
		}

		[Test]
		public void AddSkin_Event_AppendsTheSkin_AndRefusesADuplicate()
		{
			var config = CreateConfig(SkinA);
			var newSkin = new UiSkin(config, SkinB);

			UiEventDefinitions.EvAddSkin.InvokeAlways(config, newSkin);
			Assert.AreEqual(2, config.NumSkins);
			Assert.AreSame(newSkin, config.GetSkinByName(SkinB));

			UiEventDefinitions.EvAddSkin.InvokeAlways(config, newSkin);
			Assert.AreEqual(2, config.NumSkins, "the same skin instance is not added twice");
		}

		/// <summary>
		/// Init repairs a skin whose back-reference points at the wrong config, and pays for it with
		/// SetDirty plus a project-wide AssetDatabase.SaveAssets(). This test pins the case that must
		/// stay quiet — a skin that is already consistent — so that a future change cannot start saving
		/// the project on every asset load without a test noticing.
		/// </summary>
		[Test]
		public void SkinInit_DoesNotDirtyTheConfig_WhenTheBackReferenceIsAlreadyCorrect()
		{
			var config = CreateConfig(SkinA);
			AddStyle(config, SkinA, StyleName);
			EditorUtility.ClearDirty(config);

			config.GetSkinByName(SkinA).Init(config);

			Assert.IsFalse(EditorUtility.IsDirty(config));
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
