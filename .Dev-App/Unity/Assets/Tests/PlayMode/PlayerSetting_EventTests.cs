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

			setting.OnKeyDown.AddListener((_) => keyDownCount++);
			setting.OnKeyUp.AddListener((_) => keyUpCount++);
			setting.WhileKey.AddListener((_) => whileKeyCount++);
			setting.OnClick.AddListener((_) => clickCount++);

			setting.OnKeyDown.Invoke(setting);
			setting.OnKeyUp.Invoke(setting);
			setting.WhileKey.Invoke(setting);
			setting.OnClick.Invoke(setting);

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

			setting.OnBeginDrag.AddListener((_, _, _, _) => beginDragCount++);
			setting.WhileDrag.AddListener((_, _, _, _) => whileDragCount++);
			setting.OnEndDrag.AddListener((_, _, _, _) => endDragCount++);

			setting.OnBeginDrag.Invoke(setting, start, Vector3.zero, current);
			setting.WhileDrag.Invoke(setting, start, Vector3.zero, current);
			setting.OnEndDrag.Invoke(setting, start, Vector3.zero, current);

			Assert.AreEqual(1, beginDragCount);
			Assert.AreEqual(1, whileDragCount);
			Assert.AreEqual(1, endDragCount);
		}

		[Test]
		public void Clear_RemovesAllEventListeners()
		{
			var setting = CreateDummySetting();

			int callCount = 0;

			setting.OnKeyDown.AddListener((_) => callCount++);
			setting.OnClick.AddListener((_) => callCount++);
			setting.OnBeginDrag.AddListener((_, _, _, _) => callCount++);

			setting.Clear();

			setting.OnKeyDown.Invoke(setting);
			setting.OnClick.Invoke(setting);
			setting.OnBeginDrag.Invoke(setting, Vector3.zero, Vector3.zero, Vector3.zero);

			Assert.AreEqual(0, callCount);
		}
	}
}
