using GuiToolkit;
using GuiToolkit.Style;
using TMPro;
using UnityEngine;

public class ExampleAnimations : UiView
{
	public UiButton m_closeButton;
	public UiButton m_btPlayChildrenAnimation;
	public UiButton m_btPlayChildrenAnimationBackwards;
	public UiSimpleAnimationBase m_childrenAnimation;
	
	public override bool AutoDestroyOnHide => true;
	public override bool Poolable => true;
	public override bool ShowDestroyFieldsInInspector => false;
	

	protected override void Start()
	{
		base.Start();
		m_closeButton.OnClick.AddListener(() => Hide());
		m_btPlayChildrenAnimation.OnClick.AddListener(() => m_childrenAnimation.Play());
		m_btPlayChildrenAnimationBackwards.OnClick.AddListener(() => m_childrenAnimation.Play(true));
	}
}
