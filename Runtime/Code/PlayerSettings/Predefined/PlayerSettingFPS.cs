using System;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Predefined Player setting for FPS
	/// </summary>
	public class PlayerSettingFPS : PlayerSetting
	{
		private const int BaseValue = 30;
		private const int MaxValue = 121;
		private const int Range = MaxValue - BaseValue;
		private const float DefaultValue = 1; //unlimited
		
		public PlayerSettingFPS(string _customPrefab = null, string _category = null, string _group = null, string _title = null)
		{
			_customPrefab ??= "PlayerSettingFPS";
			m_options = new PlayerSettingOptions()
			{
				ValueToStringFn = GetText,
				OnChanged = OnFpsChanged,
				CustomPrefab = Resources.Load<GameObject>(_customPrefab)
			};
			
			Type type = typeof(float);
			m_category = string.IsNullOrEmpty(_category) ? __("Graphics"): _category;
			m_group = string.IsNullOrEmpty(_group) ? __("Details"): _group;
			m_title = string.IsNullOrEmpty(_title) ? _("Max FPS"): _title;
			m_isRadio = false;
			m_isLanguage = false;
			m_key = "GraphicsFPS";
			m_defaultValue = DefaultValue;
			m_type = type;
			m_isLocalized = false;

			InitValue(type);
		}

		private void OnFpsChanged(PlayerSetting _playerSetting)
		{
			var fps = (int) Mathf.Round(BaseValue + _playerSetting.GetValue<float>() * Range);
			if (fps == MaxValue)
				fps = -1;

			Application.targetFrameRate = fps;
		}

		private string GetText(float _normalizedValue)
		{
			var val = BaseValue + _normalizedValue * Range;
			int intVal = (int) Mathf.Round(val);
			
			if (intVal == MaxValue)
				return _("Unlimited");
			
			return intVal.ToString();
		}
	}
}