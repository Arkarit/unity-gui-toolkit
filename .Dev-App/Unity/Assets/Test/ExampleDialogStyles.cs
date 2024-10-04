using GuiToolkit;
using GuiToolkit.Style;
using TMPro;

public class ExampleDialogStyles : UiView
{
	public UiButton m_closeButton;
	public UiButton m_btToggleStyleInstant;
	public TMP_Text m_txtCurrentStyle;
	
	
	public override bool AutoDestroyOnHide => true;
	public override bool Poolable => true;

	protected override void Start()
	{
		base.Start();
		m_closeButton.OnClick.AddListener(OnBtClose);
		m_btToggleStyleInstant.OnClick.AddListener(OnBtToggleStyleInstant);
		DisplayCurrentStyle();
	}

	private void OnBtToggleStyleInstant()
	{
		var idx = UiMainStyleConfig.Instance.CurrentSkinIdx + 1;
		if (idx >= UiMainStyleConfig.Instance.NumSkins)
			idx = 0;
		UiMainStyleConfig.Instance.CurrentSkinIdx = idx;
		DisplayCurrentStyle();
	}
	
	private void DisplayCurrentStyle() => m_txtCurrentStyle.text = $"Current Style: {UiMainStyleConfig.Instance.CurrentSkinAlias}";

	private void OnBtClose()
	{
		Hide();
	}
}
