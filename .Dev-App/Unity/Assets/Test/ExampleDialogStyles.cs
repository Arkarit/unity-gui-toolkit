using GuiToolkit;

public class ExampleDialogStyles : UiView
{
	public UiButton m_closeButton;
	
	public override bool AutoDestroyOnHide => true;
	public override bool Poolable => true;

	protected override void Start()
	{
		base.Start();
		m_closeButton.OnClick.AddListener(OnBtClose);
	}

	private void OnBtClose()
	{
		Hide();
	}
}
