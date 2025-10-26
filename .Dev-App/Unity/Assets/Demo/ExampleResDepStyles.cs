using System;
using GuiToolkit;
using GuiToolkit.Style;
using TMPro;
using UnityEngine;

public class ExampleResDepStyles : UiView
{
	public UiButton m_closeButton;
	public TMP_Text m_aspectRatioText;
	public UiOrientationDependentStyleConfig m_styleConfig;
	
	public override bool AutoDestroyOnHide => true;
	public override bool Poolable => true;
	public override bool ShowDestroyFieldsInInspector => false;
	

	protected override void Start()
	{
		base.Start();
		m_closeButton.OnClick.AddListener(() => Hide());
	}

	private void Update()
	{
		if (Screen.height == 0)
			return;

		var aspectRatio = (float)Screen.width / Screen.height;
		m_aspectRatioText.text = $"<b>Current skin:</b> {m_styleConfig.CurrentSkinAlias} <b>Aspect Ratio:</b> {aspectRatio.ToString()}";
	}
}
