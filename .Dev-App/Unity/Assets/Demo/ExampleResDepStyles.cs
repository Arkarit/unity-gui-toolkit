using GuiToolkit;
using GuiToolkit.Style;
using TMPro;
using UnityEngine;

public class ExampleResDepStyles : UiView
{
	public UiButton m_closeButton;
	
	public override bool AutoDestroyOnHide => true;
	public override bool Poolable => true;
	public override bool ShowDestroyFieldsInInspector => false;
	

	protected override void Start()
	{
		base.Start();
		m_closeButton.OnClick.AddListener(() => Hide());
	}
}
