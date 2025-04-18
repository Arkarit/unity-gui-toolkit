using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public abstract class UiRequesterBase : UiView
	{
		public class ButtonInfo
		{
			public string Text;
			public UiButton Prefab;
			public UnityAction OnClick;
		}

		public class Options
		{
			public string Title = null;
			public ButtonInfo[] ButtonInfos = Array.Empty<ButtonInfo>();
			public bool AllowOutsideTap = true;
			public int CloseButtonIdx = Constants.INVALID;
		}

		[SerializeField] protected TextMeshProUGUI m_title;
		[SerializeField] protected GameObject m_titleContainer;
		[SerializeField] protected UiButton m_closeButton;
		[SerializeField] protected GameObject m_buttonContainer;
		[SerializeField] protected UiButton m_standardButtonPrefab;
		[SerializeField] protected UiButton m_okButtonPrefab;
		[SerializeField] protected UiButton m_cancelButtonPrefab;
		[SerializeField] protected float m_buttonScale = 1.0f;
		[SerializeField] protected int m_maxButtons = 3;
		[SerializeField] protected bool m_cancelButtonsLeftSide = false;

		private int m_closeButtonIdx = Constants.INVALID;

		private readonly List<UiButton> m_buttons = new();
		private readonly List<UnityEngine.Events.UnityAction> m_listeners = new();

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;

		protected void DoDialog( Options _options )
		{
			bool hasTitle = !string.IsNullOrEmpty(_options.Title);
			m_titleContainer.SetActive(hasTitle);
			if (hasTitle)
				m_title.text = _options.Title;

			Clear();
			EvaluateOptions(_options);
			gameObject.SetActive(true);
			ShowTopmost();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Clear();
		}

		public override void OnPooled()
		{
			base.OnPooled();
			Clear();
		}

		protected virtual void Clear()
		{
			Debug.Assert(m_buttons.Count == m_listeners.Count);

			for (int i=0; i<m_buttons.Count; i++)
			{
				m_buttons[i].OnClick.RemoveAllListeners();
				m_buttons[i].PoolDestroy();
			}

			if (m_closeButton)
				m_closeButton.OnClick.RemoveListener(OnCloseButton);
			
			m_buttons.Clear();
			m_listeners.Clear();
			m_closeButtonIdx = Constants.INVALID;
			OnClickCatcher = null;
		}

		protected virtual void EvaluateOptions( Options _options )
		{
			m_closeButtonIdx = _options.CloseButtonIdx;

			if (m_maxButtons != Constants.INVALID && _options.ButtonInfos.Length > m_maxButtons)
				Debug.LogWarning($"Dialog '{this.gameObject}' contains {_options.ButtonInfos.Length} buttons; maximum supported are {m_maxButtons}. Visual problems may appear.");

			for (int i=0; i<_options.ButtonInfos.Length; i++)
			{
				ButtonInfo bi = _options.ButtonInfos[i];
				Debug.Assert(!string.IsNullOrEmpty(bi.Text) && bi.Prefab != null, $"Wrong button info number {i} - either text or prefab not set");
				if ( string.IsNullOrEmpty(bi.Text) || bi.Prefab == null )
					continue;

				UiButton button = bi.Prefab.PoolInstantiate();
				button.transform.SetParent(m_buttonContainer.transform, false);
				button.transform.localScale = Vector3.one * m_buttonScale;

				m_buttons.Add(button);
				m_listeners.Add(bi.OnClick);

				// in a real programming language you would be able to declare the lambda capture mode (ref or copy)
				int buttonIdx = i;
				button.OnClick.AddListener( () => OnClick(buttonIdx) );

				button.Text = bi.Text;
			}

			if (m_cancelButtonsLeftSide && _options.ButtonInfos.Length > 1)
			{
				m_buttons[0].transform.SetAsLastSibling();
				m_buttons[m_buttons.Count -1].transform.SetAsFirstSibling();
			}

			if (m_closeButton)
			{
				m_closeButton.gameObject.SetActive(m_closeButtonIdx >= 0);
				m_closeButton.OnClick.AddListener(OnCloseButton);
			}

			if (_options.AllowOutsideTap)
				OnClickCatcher = OnCloseButton;
			else
				OnClickCatcher = Wiggle;
		}

		private void OnClick( int _idx )
		{
			Debug.Assert(_idx < m_listeners.Count);
			if (_idx < m_listeners.Count && m_listeners[_idx] != null)
				m_listeners[_idx]();
			Hide();
		}

		private void OnCloseButton()
		{
			if (m_closeButtonIdx >= 0)
				OnClick(m_closeButtonIdx);
		}

		private void Wiggle()
		{
			foreach(var button in m_buttons)
				if (button != null && button.gameObject.activeInHierarchy)
					button.Wiggle();
		}
	}
}