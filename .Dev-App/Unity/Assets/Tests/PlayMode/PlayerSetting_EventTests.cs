using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Tests
{
	public class PlayerSetting_EventTests
	{
		private PlayerSetting CreateDummySetting()
		{
			return new PlayerSetting(
				_category: "Test",
				_group: "Test",
				_title: "TestSetting",
				_defaultValue: true,
				_options: new PlayerSettingOptions()
			);
		}

		[Test]
		public void KeyEvents_AreInvoked()
		{
			var setting = CreateDummySetting();

			int keyDownCount = 0;
			int keyUpCount = 0;
			int whileKeyCount = 0;
			int clickCount = 0;

			setting.OnKeyDown.AddListener(() => keyDownCount++);
			setting.OnKeyUp.AddListener(() => keyUpCount++);
			setting.WhileKey.AddListener(() => whileKeyCount++);
			setting.OnClick.AddListener(() => clickCount++);

			setting.OnKeyDown.Invoke();
			setting.OnKeyUp.Invoke();
			setting.WhileKey.Invoke();
			setting.OnClick.Invoke();

			Assert.AreEqual(1, keyDownCount);
			Assert.AreEqual(1, keyUpCount);
			Assert.AreEqual(1, whileKeyCount);
			Assert.AreEqual(1, clickCount);
		}

		[Test]
		public void DragEvents_AreInvoked()
		{
			var setting = CreateDummySetting();

			int beginDragCount = 0;
			int whileDragCount = 0;
			int endDragCount = 0;

			Vector3 start = Vector3.zero;
			Vector3 current = Vector3.one;

			setting.OnBeginDrag.AddListener((_, _, _) => beginDragCount++);
			setting.WhileDrag.AddListener((_, _, _) => whileDragCount++);
			setting.OnEndDrag.AddListener((_, _, _) => endDragCount++);

			setting.OnBeginDrag.Invoke(start, Vector3.zero, current);
			setting.WhileDrag.Invoke(start, Vector3.zero, current);
			setting.OnEndDrag.Invoke(start, Vector3.zero, current);

			Assert.AreEqual(1, beginDragCount);
			Assert.AreEqual(1, whileDragCount);
			Assert.AreEqual(1, endDragCount);
		}

		[Test]
		public void Clear_RemovesAllEventListeners()
		{
			var setting = CreateDummySetting();

			int callCount = 0;

			setting.OnKeyDown.AddListener(() => callCount++);
			setting.OnClick.AddListener(() => callCount++);
			setting.OnBeginDrag.AddListener((_, _, _) => callCount++);

			setting.Clear();

			setting.OnKeyDown.Invoke();
			setting.OnClick.Invoke();
			setting.OnBeginDrag.Invoke(Vector3.zero, Vector3.zero, Vector3.zero);

			Assert.AreEqual(0, callCount);
		}
	}
}
