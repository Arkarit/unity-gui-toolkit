using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingButton : UiPlayerSettingBase
	{
		[SerializeField]
		protected UiButton m_button;

		[SerializeField]
		protected UiButton m_visualButton;

		public override void SetData(string _gameObjectNamePrefix, PlayerSetting _playerSetting, string _subKey)
		{
			base.SetData(_gameObjectNamePrefix, _playerSetting, _subKey);
			
			if (PlayerSetting.Options.Titles != null)
			{
				if (PlayerSetting.Options.Titles.Count >= 1 && !string.IsNullOrEmpty(PlayerSetting.Options.Titles[0]))
					Text = PlayerSetting.Options.Titles[0];
				if (PlayerSetting.Options.Titles.Count >= 2 )
					m_visualButton.Text = PlayerSetting.Options.Titles[1];
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_button.OnClick.AddListener(OnClick);
		}

		protected override void OnDisable()
		{
			m_button.OnClick.RemoveListener(OnClick);
			base.OnDisable();
		}

		protected virtual void OnClick() => PlayerSetting.InvokeEvents();
	}
}