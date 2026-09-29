using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the one thing in <see cref="UiCountIndicator"/> that carries a rule: the verdict.
	///
	/// Two halves. <see cref="UiCountIndicator.Derive"/> is static and side-effect free, so it can simply be
	/// asked. The override is the interesting half - the component's whole point is that it does NOT decide
	/// what counts as enough, so a caller who sets the verdict has to keep it across later value changes,
	/// and has to be able to hand it back.
	///
	/// The objects are created switched off on purpose: an enabled component runs Awake/OnEnable and would
	/// refresh before the test has set anything up. Everything under test is reachable without that.
	///
	/// OnStateChanged is NOT tested here. A CEvent stays silent while the editor is not playing - that is
	/// the whole point of the type - so its tests live in TestUiWidgetEventsPlayMode.
	/// </summary>
	[EditorAware]
	public class TestUiCountIndicator
	{
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
		public void Derive_ReadsTheTwoNumbers()
		{
			Assert.AreEqual(UiCountIndicator.EState.Below, UiCountIndicator.Derive(0, 5));
			Assert.AreEqual(UiCountIndicator.EState.Below, UiCountIndicator.Derive(4, 5));
			Assert.AreEqual(UiCountIndicator.EState.Ok, UiCountIndicator.Derive(5, 5));
			Assert.AreEqual(UiCountIndicator.EState.Above, UiCountIndicator.Derive(6, 5));
		}

		[Test]
		public void Derive_WithNothingRequired_IsOkAtZeroAndAboveBeyond()
		{
			// A maximum of zero is not a broken value, it is "none allowed" - so none is right and one is
			// too many. Worth pinning down, because a naive "current < max" alone would call zero Below.
			Assert.AreEqual(UiCountIndicator.EState.Ok, UiCountIndicator.Derive(0, 0));
			Assert.AreEqual(UiCountIndicator.EState.Above, UiCountIndicator.Derive(1, 0));
		}

		[Test]
		public void WithoutAnOverride_TheStateIsTheDerivation()
		{
			var indicator = Create();

			indicator.SetValues(3, 5);
			Assert.AreEqual(UiCountIndicator.EState.Below, indicator.State);

			indicator.SetValues(5, 5);
			Assert.AreEqual(UiCountIndicator.EState.Ok, indicator.State);

			indicator.SetValues(7, 5);
			Assert.AreEqual(UiCountIndicator.EState.Above, indicator.State);
		}

		[Test]
		public void SettingTheState_WinsOverTheNumbers()
		{
			var indicator = Create();
			indicator.SetValues(3, 5);

			indicator.State = UiCountIndicator.EState.Ok;

			Assert.AreEqual(UiCountIndicator.EState.Ok, indicator.State,
				"Three of five derives to Below, but the caller said Ok - the caller knows the rule.");
		}

		[Test]
		public void AnOverride_SurvivesLaterValueChanges()
		{
			var indicator = Create();
			indicator.State = UiCountIndicator.EState.Ok;

			indicator.SetValues(1, 9);
			Assert.AreEqual(UiCountIndicator.EState.Ok, indicator.State);

			indicator.Current = 0;
			Assert.AreEqual(UiCountIndicator.EState.Ok, indicator.State);

			indicator.Max = 100;
			Assert.AreEqual(UiCountIndicator.EState.Ok, indicator.State);
		}

		[Test]
		public void ClearingTheOverride_HandsTheVerdictBack()
		{
			var indicator = Create();
			indicator.SetValues(3, 5);
			indicator.State = UiCountIndicator.EState.Above;

			indicator.ClearStateOverride();

			Assert.AreEqual(UiCountIndicator.EState.Below, indicator.State);
		}

		[Test]
		public void ClearingAnOverrideThatIsNotSet_ChangesNothing()
		{
			var indicator = Create();
			indicator.SetValues(9, 5);

			indicator.ClearStateOverride();

			Assert.AreEqual(UiCountIndicator.EState.Above, indicator.State);
		}

		[Test]
		public void WithoutASecondCounter_TheSideConditionStaysOff()
		{
			// Both halves have to be true: somebody has to want it, and the prefab has to have a line to put
			// it in. The flag alone must not make ShowSecondary claim a line that is not there.
			var indicator = Create();

			indicator.ShowSecondary = true;

			Assert.IsFalse(indicator.ShowSecondary);
			Assert.IsFalse(indicator.HasSecondary);
		}

		[Test]
		public void TheDerivation_NeverYieldsNeutral()
		{
			// Neutral means "nothing to judge", and only the caller knows that.
			for (int max = 0; max <= 3; max++)
			{
				for (int current = 0; current <= 5; current++)
				{
					Assert.AreNotEqual(UiCountIndicator.EState.Neutral, UiCountIndicator.Derive(current, max));
				}
			}
		}

		[Test]
		public void ANeutralVerdict_SurvivesLaterValueChanges()
		{
			var indicator = Create();
			indicator.State = UiCountIndicator.EState.Neutral;

			indicator.SetValues(0, 5);
			Assert.AreEqual(UiCountIndicator.EState.Neutral, indicator.State, "short of the mark");

			indicator.SetValues(9, 5);
			Assert.AreEqual(UiCountIndicator.EState.Neutral, indicator.State, "past it");
		}

		[Test]
		public void ANeutralVerdict_WearsTheNeutralStyle_AndHidesAnIconWithoutSprite()
		{
			var indicator = Create();

			var applierGo = new GameObject("Value", typeof(RectTransform));
			applierGo.transform.SetParent(indicator.transform, false);
			applierGo.AddComponent<TMPro.TextMeshProUGUI>();
			var applier = applierGo.AddComponent<GuiToolkit.Style.UiApplyStyleTMP_Text>();

			var iconGo = new GameObject("Icon", typeof(RectTransform));
			iconGo.transform.SetParent(indicator.transform, false);
			var icon = iconGo.AddComponent<UnityEngine.UI.Image>();

			var texture = new Texture2D(2, 2);
			m_created.Add(texture);
			var okSprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.zero);
			m_created.Add(okSprite);

			var serialized = new UnityEditor.SerializedObject(indicator);
			var appliers = serialized.FindProperty("m_stateStyleAppliers");
			appliers.arraySize = 1;
			appliers.GetArrayElementAtIndex(0).objectReferenceValue = applier;
			serialized.FindProperty("m_stateIcon").objectReferenceValue = icon;
			serialized.FindProperty("m_iconOk").objectReferenceValue = okSprite;
			serialized.ApplyModifiedPropertiesWithoutUndo();

			indicator.State = UiCountIndicator.EState.Ok;
			Assert.AreEqual("CountIndicator/Ok", applier.Name);
			Assert.IsTrue(iconGo.activeSelf, "the Ok verdict has a sprite");

			indicator.State = UiCountIndicator.EState.Neutral;
			Assert.AreEqual("CountIndicator/Neutral", applier.Name);
			Assert.IsFalse(iconGo.activeSelf, "no neutral sprite, so no icon");
		}

		private UiCountIndicator Create()
		{
			var go = new GameObject("CountIndicatorUnderTest");
			go.SetActive(false);
			m_created.Add(go);
			return go.AddComponent<UiCountIndicator>();
		}
	}
}
