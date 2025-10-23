using GuiToolkit;
using UnityEngine;
using UnityEngine.UI;

public class DemoDynamicLoaded : UiView
{
	public Button m_closeButton;

	protected override void Awake()
	{
		m_closeButton.onClick.AddListener(OnCloseButtonClicked);
		base.Awake();
	}
	
	private void OnCloseButtonClicked()
	{
		UiTransitionOverlay.Instance.FadeInOverlay(() =>
		{
			Hide(true);
			UiTransitionOverlay.Instance.FadeOutOverlay();
		});
	}
}
