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
	public class UiPlayerSettingLanguageToggle : UiPlayerSettingRadio
	{
		[SerializeField]
		private Image m_flagImage;

		protected override bool NeedsLanguageChangeCallback => true;

		public override object Value
		{
			get
			{
				return LocaManager.Instance.Language == SubKey;
			}
			set
			{
				if ((string) value == SubKey)
					LocaManager.Instance.ChangeLanguage(SubKey);
				base.Value = value;
			}
		}

		public override void ApplyIcon(string _assetPath)
		{
			m_flagImage.sprite = Resources.Load<Sprite>(_assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{_assetPath}' not found!");
		}

		protected override void OnLanguageChanged( string _languageId )
		{
			m_playerSetting.Value = _languageId;
		}

		public override void SetData(string _gameObjectNamePrefix, PlayerSetting _playerSetting, string _subKey)
		{
			base.SetData(_gameObjectNamePrefix, _playerSetting, _subKey);
			if (!_playerSetting.HasIcon)
				SetBuiltinFlagIfNecessary();
			SetToggleByMatchingSubkey();
		}

		private void SetBuiltinFlagIfNecessary()
		{
			if (m_playerSetting.HasIcon)
				return;

			string assetPath = "Flags/" + SubKey;
			m_flagImage.sprite = Resources.Load<Sprite>(assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{assetPath}' not found!");
		}

	}
}