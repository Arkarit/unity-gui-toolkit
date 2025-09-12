using GuiToolkit;
using UnityEngine;
using UnityEngine.UI;

public class DemoDynamicLoaded : UiView
{
	public Button m_closeButton;

	protected override void Awake()
	{
		m_closeButton.onClick.AddListener(()=> Hide());
		base.Awake();
	}
}
