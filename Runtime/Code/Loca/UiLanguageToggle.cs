using UnityEngine;
using UnityEngine.Serialization;
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
			SetDelayed(isActive);

			base.OnValueChanged.AddListener(this.OnValueChanged);
			UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			base.OnValueChanged.RemoveListener(this.OnValueChanged);
			UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);
		}

		private void OnValueChanged( bool _active )
		{
			if (_active)
			{
				LocaManager.Instance.ChangeLanguage(m_languageToken);
			}
		}

		private void OnLanguageChanged( string _languageId )
		{
			SetIsOnWithoutNotify(LocaManager.NormalizeLanguageId(_languageId) == LocaManager.NormalizeLanguageId(m_languageToken));
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