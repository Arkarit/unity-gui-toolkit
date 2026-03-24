using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiLanguageToggle : UiToggle
	{
		[SerializeField]
		[HideInInspector]
		private Image m_flagImage;

		[SerializeField]
		[HideInInspector]
		private string m_languageToken;
		
		[SerializeField] private bool m_displayEnAsEnUs;

		public string Language
		{
			get => m_languageToken;
			set
			{
				m_languageToken = value;
				if (m_flagImage != null)
					SetNationalFlag();
				if (isActiveAndEnabled && LocaManager.Instance.Language == m_languageToken)
					SetDelayed(true);
			}
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
			var lang = m_displayEnAsEnUs && m_languageToken.Equals("en") ? "en_us" : m_languageToken;
			m_flagImage.sprite = Resources.Load<Sprite>("Flags/" + lang );
		}

	}
}