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

		public override void ApplyIcon(string _assetPath, bool _isPlayerSettingIcon)
		{
			if (!_isPlayerSettingIcon)
				_assetPath = "Flags/" + _assetPath;
			m_flagImage.sprite = Resources.Load<Sprite>(_assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{_assetPath}' not found!");
		}

		protected override void OnValueChanged( bool _active )
		{
			if (_active && Initialized)
				LocaManager.Instance.ChangeLanguage(GetValue<string>());
		}

		public override void SetData(string _gameObjectNamePrefix, string _title, PlayerSetting _playerSetting)
		{
			base.SetData(_gameObjectNamePrefix, _title, _playerSetting);
			SetToggleByLanguage();
		}

		private void SetToggleByLanguage()
		{
			m_toggle.IsOn = LocaManager.Instance.Language == GetValue<string>();
		}
	}
}