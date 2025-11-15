using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Button))]
	public class UiButton : UiButtonBase
	{
		[Tooltip("Simple wiggle animation (optional)")]
		public UiSimpleAnimation m_simpleWiggleAnimation;

		private Button m_button;

		public Button Button
		{
			get
			{
				InitIfNecessary();
				return m_button;
			}
		}

		public Button.ButtonClickedEvent OnClick => Button.onClick;

		public void Wiggle()
		{
			if (m_simpleWiggleAnimation)
				m_simpleWiggleAnimation.Play();
		}

		public override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			base.OnEnabledInHierarchyChanged(_enabled);
			InitIfNecessary();
			m_button.interactable = _enabled;
		}

		protected override void Init()
		{
			base.Init();

			m_button = GetComponent<Button>();
		}

		protected override bool ForwardClick()
		{
			if (!EvaluateButton(true))
				return false;
			
			m_button.onClick.Invoke();
			return true;
		}
	}
}