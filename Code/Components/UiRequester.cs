using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GuiToolkit
{


	public class UiRequester : UiViewModal
	{
		public const int INVALID = -1;

		public UiButton m_closeButton;

		public GameObject m_buttonContainer;
		public UiButton m_standardButtonPrefab;
		public UiButton m_okButtonPrefab;
		public UiButton m_cancelButtonPrefab;


		public TextMeshProUGUI m_title;
		public TextMeshProUGUI m_text;

		private bool m_allowOutsideTap;
		private int m_closeButtonIdx = INVALID;

		private readonly List<UiButton> m_createdButtons = new List<UiButton>();
		private readonly List<UnityEngine.Events.UnityAction> m_listeners = new List<UnityEngine.Events.UnityAction>();

		public class ButtonInfo
		{
			public string Text;
			public UiButton Prefab;
			public UnityEngine.Events.UnityAction OnClick;
		}

		public class Options
		{
			public ButtonInfo[] ButtonInfos;
			public bool AllowOutsideTap = true;
			public int CloseButtonIdx = INVALID;
		}

		public override bool AutoDestroyOnHide => true;

		public void Requester( string _title, string _text, Options _options )
		{
			m_title.text = _title;
			m_text.text = _text;
			Clear();
			EvaluateOptions(_options);
			gameObject.SetActive(true);
			Show();
		}

		public void OkRequester( string _title, string _text, UnityAction _onOk = null, string _okText = null )
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[] 
				{
					new ButtonInfo {
						Text = string.IsNullOrEmpty(_okText) ? "Ok" : _okText,
						Prefab = m_standardButtonPrefab,
						OnClick = _onOk
					}
				},
				AllowOutsideTap = true,
				CloseButtonIdx = 0
			};
			Requester( _title, _text, options );
		}

		public void YesNoRequester( string _title, string _text, bool _allowOutsideTap, UnityAction _onOk,
			UnityAction _onCancel = null, string _yesText = null, string _noText = null )
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[] 
				{
					new ButtonInfo {
						Text = string.IsNullOrEmpty(_yesText) ? "Yes" : _yesText,
						Prefab = m_okButtonPrefab,
						OnClick = _onOk
					},
					new ButtonInfo {
						Text = string.IsNullOrEmpty(_noText) ? "No" : _noText,
						Prefab = m_cancelButtonPrefab,
						OnClick = _onCancel
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				CloseButtonIdx = _allowOutsideTap ? 1 : INVALID
			};
			Requester( _title, _text, options );
		}

		private void Clear()
		{
			Debug.Assert(m_createdButtons.Count == m_listeners.Count);

			for (int i=0; i<m_createdButtons.Count; i++)
			{
				m_createdButtons[i].OnClick.RemoveAllListeners();
				//TODO ui cache destroy
				m_createdButtons[i].transform.Destroy();
			}

			m_closeButton.OnClick.RemoveListener(OnCloseButton);
			
			m_createdButtons.Clear();
			m_listeners.Clear();
			m_closeButtonIdx = INVALID;
			OnClickCatcher = null;
		}

		private void EvaluateOptions( Options _options )
		{
			m_allowOutsideTap = _options.AllowOutsideTap;
			m_closeButtonIdx = _options.CloseButtonIdx;

			for (int i=0; i<_options.ButtonInfos.Length; i++)
			{
				ButtonInfo bi = _options.ButtonInfos[i];
				Debug.Assert(!string.IsNullOrEmpty(bi.Text) && bi.Prefab != null, $"Wrong button info number {i} - either text or prefab not set");
				if ( string.IsNullOrEmpty(bi.Text) || bi.Prefab == null )
					continue;

				//TODO ui cache instantiate
				UiButton button = Instantiate(bi.Prefab);
				button.transform.SetParent(m_buttonContainer.transform);

				m_createdButtons.Add(button);
				m_listeners.Add(bi.OnClick);

				if (bi.OnClick != null)
					button.OnClick.AddListener( () => OnClick(i) );

				button.Text = bi.Text;
			}

			m_closeButton.gameObject.SetActive(m_closeButtonIdx > 0);
			m_closeButton.OnClick.AddListener(OnCloseButton);

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
			foreach(var button in m_createdButtons)
				if (button != null && button.gameObject.activeInHierarchy)
					button.Wiggle();
		}


	}
}