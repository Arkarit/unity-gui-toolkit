using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The event halves of <see cref="UiChip"/> and <see cref="UiCountIndicator"/>.
	///
	/// They are play mode tests for one reason: both events are a <see cref="CEvent"/>, and a CEvent stays
	/// silent while the editor is not playing. That is deliberate - components run in edit mode and must
	/// not fire game logic while an author is only looking at them - but it also means an edit mode test
	/// could never see an invocation, and would pass or fail for the wrong reason.
	///
	/// Everything that does NOT need an invocation is tested in edit mode, in TestUiChip and
	/// TestUiCountIndicator.
	/// </summary>
	public class TestUiWidgetEventsPlayMode
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
		public void AClickIsSwallowedWhileTheChipIsNotClickable()
		{
			var chip = Create<UiChip>();
			int calls = 0;
			chip.AddClickListener(() => calls++);

			chip.Clickable = false;
			chip.OnPointerClick(new PointerEventData(EventSystem.current));
			Assert.AreEqual(0, calls, "Something else inside the chip may still be raycasting - the flag "
				+ "has to be checked again on the way in.");

			chip.Clickable = true;
			chip.OnPointerClick(new PointerEventData(EventSystem.current));
			Assert.AreEqual(1, calls);
		}

		[Test]
		public void TheStateEvent_FiresOnTheFirstRefreshAndThenOnlyOnChange()
		{
			var indicator = Create<UiCountIndicator>();
			var seen = new List<UiCountIndicator.EState>();
			indicator.OnStateChanged.AddListener(state => seen.Add(state));

			indicator.SetValues(3, 5);
			indicator.SetValues(2, 5);
			indicator.SetValues(1, 5);

			Assert.AreEqual(new[] { UiCountIndicator.EState.Below }, seen,
				"All three are Below - the numbers changed, the verdict did not.");

			indicator.SetValues(5, 5);

			Assert.AreEqual(new[] { UiCountIndicator.EState.Below, UiCountIndicator.EState.Ok }, seen);
		}

		[Test]
		public void TheStateEvent_AlsoFiresForAnOverride()
		{
			var indicator = Create<UiCountIndicator>();
			indicator.SetValues(5, 5);

			var seen = new List<UiCountIndicator.EState>();
			indicator.OnStateChanged.AddListener(state => seen.Add(state));

			indicator.State = UiCountIndicator.EState.Below;

			Assert.AreEqual(new[] { UiCountIndicator.EState.Below }, seen);
		}

		/// <summary>
		/// Switched off before the component goes on, so Awake and OnEnable do not refresh before the test
		/// has said anything. Neither event needs the component to be enabled to fire.
		/// </summary>
		private T Create<T>() where T : Component
		{
			var go = new GameObject(typeof(T).Name + "UnderTest");
			go.SetActive(false);
			m_created.Add(go);
			return go.AddComponent<T>();
		}
	}
}
