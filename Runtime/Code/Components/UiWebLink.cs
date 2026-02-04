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

	[SerializeField] private WebLink[] m_links;

	private WebLink m_currentLink = new();

	protected override bool NeedsLanguageChangeCallback => true;

	protected override void OnLanguageChanged( string _languageId )
	{
		base.OnLanguageChanged(_languageId);
		SetCurrentLink();
	}

	public void OnPointerClick( PointerEventData _eventData )
	{
		if (m_currentLink != null )
			Application.OpenURL(m_currentLink.Url);
	}

	private void SetCurrentLink()
	{
		m_currentLink = null;
		var languageId = LocaManager.Instance.Language;
		foreach (var webLink in m_links)
		{
			if (webLink.LanguageId == languageId)
			{
				m_currentLink = webLink;
				return;
			}
		}

		if (m_links.Length == 0)
		{
			UiLog.LogError($"No Web links defined", this);
			return;
		}

		UiLog.LogWarning($"Current language not found, using fallback link {m_links[0]}");
		m_currentLink = m_links[0];
	}
}
