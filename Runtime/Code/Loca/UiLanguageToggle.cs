using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiLanguageToggle : UiToggle
	{
		[SerializeField]
		private Image m_flagImage;

		[SerializeField]
		private string m_languageToken;

		public string Language
		{
			get => m_languageToken;
#if UNITY_EDITOR
			set
			{
				m_languageToken = value;
				OnValidate();
			}
#endif
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			bool isActive = LocaManager.Instance.Language == Language;
			if (isActive)
				SetDelayed(true);

			base.OnValueChanged.AddListener(this.OnValueChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			base.OnValueChanged.RemoveListener(this.OnValueChanged);
		}

		private void OnValueChanged( bool _active )
		{
			if (_active)
			{
				LocaManager.Instance.ChangeLanguage(m_languageToken);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			SetNationalFlag();
		}
#endif
		private void SetNationalFlag()
		{
			m_flagImage.sprite = Resources.Load<Sprite>("Flags/" + m_languageToken );
		}

	}
}