using System;
using GuiToolkit;
using UnityEngine;
using UnityEngine.EventSystems;

public class UiWebLink : UiThing, IPointerClickHandler
{
	[Serializable]
	public class WebLink
	{
		public string LanguageId;
		public string Url;

		public override string ToString()
		{
			return $"'{LanguageId}' -> '{Url}'";
		}
	}

	[SerializeField] private WebLink[] m_links = new WebLink[0];
	[SerializeField] private bool m_isLocalized = false;
	[SerializeField] private string m_link;

	protected override bool NeedsLanguageChangeCallback => m_isLocalized;

	protected override void OnEnable()
	{
		base.OnEnable();
		SetCurrentLink();
	}

	protected override void OnLanguageChanged( string _languageId )
	{
		base.OnLanguageChanged(_languageId);
		SetCurrentLink();
	}

	public void OnPointerClick( PointerEventData _eventData )
	{
		if (_eventData.button != PointerEventData.InputButton.Left)
			return;

		if (m_link == null)
		{
			UiLog.LogWarning("No link selected", this);
			return;
		}

		if (string.IsNullOrWhiteSpace(m_link))
		{
			UiLog.LogError($"Current link has no URL: {m_link}", this);
			return;
		}

		Application.OpenURL(m_link);
	}

	private void SetCurrentLink()
	{
		if (!m_isLocalized)
			return;

		m_link = null;

		if (m_links == null || m_links.Length == 0)
		{
			UiLog.LogError("No Web links defined", this);
			return;
		}

		string languageId = LocaManager.Instance.Language;

		foreach (WebLink webLink in m_links)
		{
			if (webLink == null)
				continue;

			if (string.Equals(webLink.LanguageId, languageId, StringComparison.OrdinalIgnoreCase))
			{
				m_link = webLink.Url;
				return;
			}
		}

		UiLog.LogWarning($"Current language not found, using fallback link {m_links[0]}", this);
		m_link = m_links[0].Url;
	}
}
