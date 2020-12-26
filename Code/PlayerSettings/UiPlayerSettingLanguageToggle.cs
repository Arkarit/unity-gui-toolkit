using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiPlayerSettingLanguageToggle : UiPlayerSettingToggle
	{
		[SerializeField]
		private Image m_flagImage;

		private string Language => (string) m_playerSetting.AdditionalData;

		public override void ApplyIcon(string _assetPath)
		{
			m_flagImage.sprite = Resources.Load<Sprite>(_assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{_assetPath}' not found!");
		}

		public override void ApplyAdditionalData(object _additionalData)
		{
			if (m_playerSetting.HasIcon)
				return;

			string assetPath = "Flags/" + (string) _additionalData;
			m_flagImage.sprite = Resources.Load<Sprite>(assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{assetPath}' not found!");
		}

		protected override void OnValueChanged( bool _active )
		{
			base.OnValueChanged(_active);
			if (_active && Initialized)
				LocaManager.Instance.ChangeLanguage(Language);
		}

		public override void SetData(string _gameObjectNamePrefix, string _title, PlayerSetting _playerSetting)
		{
			base.SetData(_gameObjectNamePrefix, _title, _playerSetting);
			SetToggleByLanguage();
		}

		private void SetToggleByLanguage()
		{
			m_toggle.IsOn = LocaManager.Instance.Language == Language;
		}
	}
}