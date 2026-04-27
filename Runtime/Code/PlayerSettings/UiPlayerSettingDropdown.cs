using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Player-setting UI element that presents all string options in a single <see cref="UiDropdown"/>.
	///
	/// Expects a <see cref="UiDropdown"/> on the same GameObject (auto-discovered via
	/// <c>GetComponent</c> if not explicitly assigned). The dropdown is populated from
	/// <c>Options.Titles</c> (or <c>Options.StringValues</c> as a fallback) and the current
	/// player-setting value is kept in sync via the silent <see cref="UiDropdown.SelectedIndex"/>
	/// setter to avoid circular event loops.
	/// </summary>
	public class UiPlayerSettingDropdown : UiPlayerSettingBase
	{
		[SerializeField][Optional] protected UiDropdown m_dropdown;

		protected override void Awake()
		{
			base.Awake();
			if (m_dropdown == null)
				m_dropdown = GetComponent<UiDropdown>();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_dropdown != null)
				m_dropdown.EvOnDropdownValueChanged.AddListener(OnDropdownValueChanged);
		}

		protected override void OnDisable()
		{
			if (m_dropdown != null)
				m_dropdown.EvOnDropdownValueChanged.RemoveListener(OnDropdownValueChanged);
			base.OnDisable();
		}

		public override void SetData( string _gameObjectNamePrefix, PlayerSetting _playerSetting, string _subKey )
		{
			// Re-implement base SetData with null-safe m_text (title label is optional on this prefab).
			m_subKey = _subKey;
			m_playerSetting = _playerSetting;
			m_isLocalized = _playerSetting.IsLocalized;

			string key = _playerSetting.Title;
			if (m_text != null)
				m_text.text = key;

			gameObject.name = _gameObjectNamePrefix + key;

			if (_playerSetting.HasIcons)
				ApplyIcons(_playerSetting.Icons);

			if (m_dropdown != null)
			{
				var opts = _playerSetting.Options;
				List<string> items = opts.Titles ?? opts.StringValues;
				m_dropdown.PresetStringItems = items?.ToArray();
				SyncDropdownToValue();
			}
		}

		public override object Value
		{
			get => base.Value;
			set
			{
				base.Value = value;
				SyncDropdownToValue();
			}
		}

		protected virtual void OnDropdownValueChanged( int _index )
		{
			if (!Initialized)
				return;

			var stringValues = m_playerSetting.Options.StringValues;
			if (stringValues != null && _index >= 0 && _index < stringValues.Count)
				base.Value = stringValues[_index];
		}

		protected void SyncDropdownToValue()
		{
			if (m_dropdown == null || m_playerSetting == null)
				return;

			var stringValues = m_playerSetting.Options.StringValues;
			if (stringValues == null)
				return;

			string current = m_playerSetting.GetValue<string>();
			int index = stringValues.IndexOf(current);
			if (index >= 0)
				m_dropdown.SelectedIndex = index;
		}
	}
}
