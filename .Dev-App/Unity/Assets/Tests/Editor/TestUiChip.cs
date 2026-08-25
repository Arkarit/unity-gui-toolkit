using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for the one promise <see cref="UiChip"/> makes beyond showing a label: a chip that takes no
	/// clicks does not raycast either, so the tap reaches the row underneath instead of dying on the chip.
	///
	/// Run against the shipped prefab rather than a hand-built object. The rule is about the background
	/// image and the icon slot, and those are wiring - a test that wired them itself would prove that the
	/// test can wire a chip, not that the chip everyone actually instantiates is wired.
	///
	/// What a click DOES is not tested here: OnClick is a CEvent and stays silent while the editor is not
	/// playing, so that half lives in TestUiWidgetEventsPlayMode.
	/// </summary>
	[EditorAware]
	public class TestUiChip
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
		public void TheShippedPrefab_HasItsBackgroundWired()
		{
			var chip = Create();

			Assert.IsNotNull(chip.Background, "Without a background the chip has no click target and no "
				+ "surface for its style to colour.");
		}

		[Test]
		public void AChipThatTakesNoClicks_DoesNotRaycast()
		{
			var chip = Create();

			chip.Clickable = false;
			Assert.IsFalse(chip.Background.raycastTarget);

			chip.Clickable = true;
			Assert.IsTrue(chip.Background.raycastTarget);
		}

		[Test]
		public void AddingAListener_MakesTheChipClickable()
		{
			var chip = Create();
			chip.Clickable = false;

			chip.AddClickListener(() => { });

			Assert.IsTrue(chip.Clickable, "A listener on a chip that cannot be clicked would never be "
				+ "called, and the cause would be invisible.");
			Assert.IsTrue(chip.Background.raycastTarget);
		}

		[Test]
		public void TheIconSlot_FollowsItsSprite()
		{
			var chip = Create();
			var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
			m_created.Add(sprite);

			chip.Icon = sprite;
			Assert.AreEqual(sprite, chip.Icon);

			chip.Icon = null;
			Assert.IsNull(chip.Icon, "Clearing the icon switches its object off rather than leaving an "
				+ "empty slot for a layout group to reserve space for.");
		}

		private UiChip Create()
		{
			// By search, not by path: the package is mounted differently in the dev app than in a client,
			// and a hard-coded Packages/... path would only pass in one of the two.
			var guids = AssetDatabase.FindAssets("StandardChip t:prefab");
			Assert.IsNotEmpty(guids, "StandardChip.prefab was not found in this project.");

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
			Assert.IsNotNull(prefab);

			var instance = (GameObject) PrefabUtility.InstantiatePrefab(prefab);
			m_created.Add(instance);

			var chip = instance.GetComponent<UiChip>();
			Assert.IsNotNull(chip, "StandardChip.prefab has no UiChip on its root.");
			return chip;
		}
	}
}
