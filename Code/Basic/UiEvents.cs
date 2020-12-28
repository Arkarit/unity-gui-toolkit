using System;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public static class UiEvents
	{
		public class EventLanguageChanged : UnityEvent<string> {}
		public static EventLanguageChanged OnLanguageChanged = new EventLanguageChanged();

		public class EventPlayerSettingChanged : UnityEvent<PlayerSetting> {}
		public static EventPlayerSettingChanged OnPlayerSettingChanged = new EventPlayerSettingChanged();
	}
}