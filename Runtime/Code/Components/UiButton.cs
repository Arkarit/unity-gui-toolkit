using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A clickable button that sits next to a Unity <see cref="Button"/> on the same GameObject
	/// (added via RequireComponent). Adds an optional wiggle animation on click and can trigger the
	/// wiggle of other linked buttons; disables the underlying Button when the hierarchy is disabled.
	/// Subscribe to clicks via <see cref="OnClick"/>.
	/// </summary>
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

				// The cached reference can outlive the Button it points to: the init flag survives a domain
				// reload, the object behind the reference does not have to. Unity then reports the field as
				// missing instead of null, so resolve it again rather than trusting the flag.
				if (!m_button)
					BindButton();

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

			// Also reached from OnValidate, i.e. on objects that are half torn down or not set up yet
			var button = Button;
			if (button)
				button.interactable = _enabled;
		}

		protected override bool EvaluateButton(bool _playBackwardsAnimation)
		{
			var button = Button;
			if (!button || !button.enabled || !button.gameObject.activeInHierarchy || !button.interactable)
				return false;
			
			return base.EvaluateButton(_playBackwardsAnimation);
		}

		protected override void Init()
		{
			base.Init();
			BindButton();
		}

		private void BindButton()
		{
			m_button = GetComponent<Button>();
			if (!m_button)
				return;

			m_button.onClick.RemoveListener(WiggleLinkedButtons);
			m_button.onClick.AddListener(WiggleLinkedButtons);
		}

		protected override bool ForwardClick()
		{
			if (!EvaluateButton(true))
				return false;
			
			Button.onClick.Invoke();
			return true;
		}
	}
}