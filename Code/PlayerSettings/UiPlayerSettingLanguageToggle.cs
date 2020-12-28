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
	public class UiPlayerSettingLanguageToggle : UiPlayerSettingBase
	{
		[SerializeField]
		protected UiToggle m_toggle;

		[SerializeField]
		private Image m_flagImage;

		public UiToggle UiToggle => m_toggle;
		public override Toggle Toggle => m_toggle.Toggle;
		protected override bool NeedsLanguageChangeCallback => true;

		private string Language => m_subKey;

		public override object Value
		{
			get
			{
				if (m_playerSetting == null)
					return false;
				return LocaManager.Instance.Language == Language;
			}
			set
			{
				base.Value = value;
				if ((string) value == Language)
					LocaManager.Instance.ChangeLanguage(Language);
				SetToggleByLanguage();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_toggle.OnValueChanged.AddListener(OnValueChanged);
		}

		protected override void OnDisable()
		{
			m_toggle.OnValueChanged.RemoveListener(OnValueChanged);
			base.OnDisable();
		}

		public override void ApplyIcon(string _assetPath)
		{
			m_flagImage.sprite = Resources.Load<Sprite>(_assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{_assetPath}' not found!");
		}

		protected void OnValueChanged( bool _active )
		{
			if (_active && Initialized)
				Value = Language;
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
			SetToggleByLanguage();
		}

		private void SetToggleByLanguage()
		{
			m_toggle.IsOn = (bool) Value;
		}

		private void SetBuiltinFlagIfNecessary()
		{
			if (m_playerSetting.HasIcon)
				return;

			string assetPath = "Flags/" + Language;
			m_flagImage.sprite = Resources.Load<Sprite>(assetPath);
			if (m_flagImage.sprite == null)
				Debug.LogError($"Sprite '{assetPath}' not found!");
		}

	}
}