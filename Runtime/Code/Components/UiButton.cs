using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Button))]
	public class UiButton : UiButtonBase
	{
		[Tooltip("Simple wiggle animation (optional)")]
		public UiSimpleAnimation m_simpleWiggleAnimation;

		[Tooltip("Other buttons whose Wiggle() is triggered when this button is clicked.")]
		public List<UiButton> m_wiggleButtons = new();

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

		private void WiggleLinkedButtons()
		{
			if (m_wiggleButtons == null)
				return;

			foreach (var btn in m_wiggleButtons)
			{
				if (btn != null)
					btn.Wiggle();
			}
		}

		public override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			base.OnEnabledInHierarchyChanged(_enabled);
			InitIfNecessary();
			m_button.interactable = _enabled;
		}

		protected override bool EvaluateButton(bool _playBackwardsAnimation)
		{
			if (!m_button.enabled || !m_button.gameObject.activeInHierarchy || !m_button.interactable)
				return false;
			
			return base.EvaluateButton(_playBackwardsAnimation);
		}

		protected override void Init()
		{
			base.Init();

			m_button = GetComponent<Button>();
			m_button.onClick.RemoveListener(WiggleLinkedButtons);
			m_button.onClick.AddListener(WiggleLinkedButtons);
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