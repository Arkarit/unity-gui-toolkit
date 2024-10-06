using GuiToolkit;
using GuiToolkit.Style;
using TMPro;

public class ExampleDialogStyles : UiView
{
	public UiButton m_closeButton;
	public UiButton m_btToggleSkinInstant;
	public UiButton m_btToggleSkinHalfSecond;
	public UiButton m_btToggleSkinThreeSeconds;
	public TMP_Text m_txtCurrentSkin;
	
	public override bool AutoDestroyOnHide => true;
	public override bool Poolable => true;

	protected override void Start()
	{
		base.Start();
		m_closeButton.OnClick.AddListener(OnBtClose);
		m_btToggleSkinInstant.OnClick.AddListener(OnBtToggleSkinInstant);
		m_btToggleSkinHalfSecond.OnClick.AddListener(OnBtToggleSkinHalfSecond);
		m_btToggleSkinThreeSeconds.OnClick.AddListener(OnBtToggleSkinThreeSeconds);
		UiEventDefinitions.EvSkinChanged.AddListener(OnSkinChanged);
		DisplayCurrentSkin();
	}

	private void OnBtToggleSkinInstant() => ToggleSkin(0);
	private void OnBtToggleSkinHalfSecond() => ToggleSkin(.5f);
	private void OnBtToggleSkinThreeSeconds() => ToggleSkin(3);

	private void OnSkinChanged(float _) => DisplayCurrentSkin();

	private void ToggleSkin(float _duration)
	{
		int idx = UiMainStyleConfig.Instance.CurrentSkinIdx + 1;
		if (idx >= UiMainStyleConfig.Instance.NumSkins)
			idx = 0;

		string nextSkin = UiMainStyleConfig.Instance.Skins[idx].Alias;
		UiStyleManager.SetSkin(nextSkin, _duration);
	}
	
	private void DisplayCurrentSkin() => m_txtCurrentSkin.text = $"Current Skin: {UiMainStyleConfig.Instance.CurrentSkinAlias}";

	private void OnBtClose()
	{
		Hide();
	}
}
