using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Predefined Player setting for Quality
	/// </summary>
	public class PlayerSettingQuality : PlayerSetting
	{
		public PlayerSettingQuality(string _category = null, string _group = null, string _title = null)
		{
			m_options = new PlayerSettingOptions()
			{
				StringValues = GetQualities(),
				OnChanged = OnQualityChanged
			};
			
			Type type = typeof(string);
			m_category = string.IsNullOrEmpty(_category) ? __("Graphics"): _category;
			m_group = string.IsNullOrEmpty(_group) ? __("Quality"): _group;
			m_title = string.IsNullOrEmpty(_title) ? string.Empty: _title;
			m_isRadio = true;
			m_isLanguage = false;
			m_key = "GraphicsQuality";
			m_defaultValue = GetCurrentQuality();
			m_icons = m_options.Icons;
			m_type = type;
			m_isLocalized = true;

			InitValue(type);
		}

		private void OnQualityChanged(PlayerSetting _playerSetting)
		{
			var targetQuality = _playerSetting.GetValue<string>();
			int index = Array.IndexOf(QualitySettings.names, targetQuality);
			if (index >= 0)
			{
				QualitySettings.SetQualityLevel(index, true);
				return;
			}

			Debug.LogError("Quality Level '" + targetQuality + "' not found!");
		}

		protected List<string> GetQualities() => QualitySettings.names.ToList();
		protected string GetCurrentQuality() => QualitySettings.names[QualitySettings.GetQualityLevel()];
	}
}