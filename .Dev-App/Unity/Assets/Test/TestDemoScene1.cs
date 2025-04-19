using GuiToolkit;
using System;
using GuiToolkit.Style;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TestDemoScene1 : UiView
{
	public Button m_closeButton;
	public Button m_showSplashMessageButton;
	public Button m_showRequesterButton;
	public Button m_showSettings;
	public Button m_exampleDialogStylesButton;
	public Button m_showDatePickerButton;

	public TMP_InputField m_splashMessageInput;
	public TMP_InputField m_requesterTitleInput;
	public TMP_InputField m_requesterTextInput;

	public TMP_Text m_singularPluralTest;
	
	public ExampleDialogStyles m_exampleDialogStylesPrefab;

	protected override bool NeedsLanguageChangeCallback => true;

	protected override void Start()
	{
		base.Start();

		m_closeButton.onClick.AddListener(OnCloseButtonClicked);
		m_showSplashMessageButton.onClick.AddListener(OnShowSplashMessageClicked);
		m_showRequesterButton.onClick.AddListener(OnShowRequester);
		m_showSettings.onClick.AddListener(OnShowSettings);
		m_exampleDialogStylesButton.onClick.AddListener(OnExampleDialogStylesButton);
		m_showDatePickerButton.onClick.AddListener(OnShowDatePickerButton);
	}

	private void OnShowDatePickerButton()
	{
		UiMain.Instance.CreateRequester(new UiRequester.Options()
		{
			ButtonInfos = UiRequester.CreateButtonInfos
			(
				("Ok", null),
				("Cancel", null)
			),
			DateTimeOptions = new UiDateTimePanel.Options()
			{
				ShowTime = false
			}
		});
	}

	private void OnExampleDialogStylesButton()
	{
		var exampleDialogStylesDialog = UiMain.Instance.CreateView(m_exampleDialogStylesPrefab);
		exampleDialogStylesDialog.Show();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		FillSingularPluralTest();
	}

	private void FillSingularPluralTest()
	{
		string s = "";
		for (int i = 1; i < 20; i++)
		{
			s += String.Format(_n("{0} Tree, ", "{0} Trees, ", i), i);
		}
		m_singularPluralTest.text = s;
	}

	private void OnShowSettings()
	{
		UiMain.Instance.ShowSettingsDialog();
	}

	private void OnShowSplashMessageClicked()
	{
		string text = m_splashMessageInput.text;
		if (string.IsNullOrEmpty(text))
			text = gettext("Hello World");
		UiMain.Instance.ShowToastMessageView(text);
	}

	private void OnShowRequester()
	{
		string title = m_requesterTitleInput.text;
		if (string.IsNullOrEmpty(title))
			title = gettext("Test Requester");

		string text = m_requesterTextInput.text;
		if (string.IsNullOrEmpty(text))
			text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore.";

		UiMain.Instance.OkRequester(title, text);
	}

	private void OnCloseButtonClicked()
	{
		UiMain.Instance.YesNoRequester(gettext("Really Quit?"), gettext("Are you really really sure you want to quit?"), false, OnQuit, null );
	}

	private void OnQuit()
	{
		Hide(false, () => UiMain.Instance.Quit());
	}

	protected override void OnLanguageChanged( string _languageId )
	{
		FillSingularPluralTest();
	}
}
