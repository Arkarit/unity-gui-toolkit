using System;
using System.Threading.Tasks;
using GuiToolkit.UiStateSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public class UiRequester : UiRequesterBase
	{
		public new class Options : UiRequesterBase.Options
		{
			public string Text = null;
			public string PlaceholderText = null;
			public string InputText = null;
			public UiDateTimePanel.Options DateTimeOptions = null;
		}

		[SerializeField] protected GameObject m_inputFieldContainer;
		[SerializeField] protected GameObject m_textContainer;
		[SerializeField] protected UiDateTimePanel m_dateTimePanel;
		[SerializeField] protected TextMeshProUGUI m_text;
		[SerializeField] protected TMP_InputField m_inputField;

		public void Requester( Options _options ) => DoDialog(_options);

		public void Requester( string _title, string _text, Options _options )
		{
			_options.Title = _title;
			_options.Text = _text;
			DoDialog(_options);
		}

		public void OkRequester
		(
			string _title,
			string _text,
			UnityAction _onOk = null,
			string _okText = null,
			bool _allowOutsideTap = true
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_okText) ? __("Ok") : _okText,
						Prefab = UiMain.Instance.StandardButtonPrefab,
						OnClick = _onOk
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				CloseButtonAction = _onOk,
				Text = _text,
			};

			Requester(_title, _text, options);
		}

		// Waits until dialog is done; no result returned.
		public async Task OkRequesterBlocking
		(
			string _title,
			string _text,
			string _okText = null,
			bool _allowOutsideTap = true,
			bool _waitForClose = true
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
			new ButtonInfo
			{
				Text = string.IsNullOrEmpty(_okText) ? __("Ok") : _okText,
				Prefab = UiMain.Instance.StandardButtonPrefab,
				OnClick = null
			}
				},
				AllowOutsideTap = _allowOutsideTap,
				CloseButtonAction = null,
				Text = _text,
				Title = _title
			};

			if (_waitForClose)
				await DoDialogAwaitCloseAsync(options);
			else
				await DoDialogAwaitClickAsync(options);
		}

		public void YesNoRequester
		(
			string _title,
			string _text,
			bool _allowOutsideTap,
			UnityAction _onOk,
			UnityAction _onCancel = null,
			string _yesText = null,
			string _noText = null
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_yesText) ? __("Yes") : _yesText,
						Prefab = UiMain.Instance.OkButtonPrefab,
						OnClick = _onOk
					},
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_noText) ? __("No") : _noText,
						Prefab = UiMain.Instance.CancelButtonPrefab,
						OnClick = _onCancel
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				ShowCloseButton = _allowOutsideTap,
				CloseButtonAction = _allowOutsideTap ? _onCancel : null,
				Text = _text,
			};

			Requester(_title, _text, options);
		}

		public async Task<bool> YesNoRequesterBlocking
		(
			string _title,
			string _text,
			bool _allowOutsideTap = true,
			bool _waitForClose = true,
			string _yesText = null,
			string _noText = null
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_yesText) ? __("Yes") : _yesText,
						Prefab = UiMain.Instance.OkButtonPrefab,
						OnClick = null
					},
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_noText) ? __("No") : _noText,
						Prefab = UiMain.Instance.CancelButtonPrefab,
						OnClick = null
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				ShowCloseButton = _allowOutsideTap,
				CloseButtonAction = null,
				Text = _text,
				Title = _title
			};

			int idx;

			if (_waitForClose)
				idx = await DoDialogAwaitCloseAsync(options);
			else
				idx = await DoDialogAwaitClickAsync(options);

			return IsOk(idx);
		}
		
		private bool IsOk(int _idx) => _idx == (m_cancelButtonsLeftSide ? 1 : 0); 

		public void OkCancelInputRequester
		(
			string _title,
			string _text,
			bool _allowOutsideTap,
			UnityAction<string> _onOk,
			UnityAction _onCancel = null,
			string _placeholderText = null,
			string _inputText = null,
			string _yesText = null,
			string _noText = null
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_yesText) ? __("Ok") : _yesText,
						Prefab = UiMain.Instance.OkButtonPrefab,
						OnClick = () => _onOk(GetInputText())
					},
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_noText) ? __("Cancel") : _noText,
						Prefab = UiMain.Instance.CancelButtonPrefab,
						OnClick = _onCancel
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				ShowCloseButton = _allowOutsideTap,
				CloseButtonAction = _allowOutsideTap ? _onCancel : null,
				Text = _text,
				PlaceholderText = _placeholderText,
				InputText = _inputText,
			};
			Requester(_title, _text, options);
		}

		// Blocking: returns input on OK, null on cancel/dismiss
		public async Task<string> OkCancelInputRequesterBlocking
		(
			string _title,
			string _text,
			bool _allowOutsideTap,
			bool _waitForClose = true,
			string _placeholderText = null,
			string _inputText = null,
			string _yesText = null,
			string _noText = null
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_yesText) ? __("Ok") : _yesText,
						Prefab = UiMain.Instance.OkButtonPrefab,
						OnClick = null
					},
					new ButtonInfo
					{
						Text = string.IsNullOrEmpty(_noText) ? __("Cancel") : _noText,
						Prefab = UiMain.Instance.CancelButtonPrefab,
						OnClick = null
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				ShowCloseButton = _allowOutsideTap,
				CloseButtonAction = null,
				Text = _text,
				PlaceholderText = _placeholderText,
				InputText = _inputText,
				Title = _title
			};

			int idx;
			if (_waitForClose)
				idx = await DoDialogAwaitCloseAsync(options);
			else
				idx = await DoDialogAwaitClickAsync(options);

			if (IsOk(idx))
				return GetInputText();

			return null;
		}

		public string GetInputText() => m_inputField.text;

		public DateTime GetDateTime() => m_dateTimePanel.SelectedDateTime;

		protected override void EvaluateOptions( UiRequesterBase.Options _options )
		{
			base.EvaluateOptions(_options);

			Options options = (Options)_options;

			bool hasText = !string.IsNullOrEmpty(options.Text);
			bool hasInput = !string.IsNullOrEmpty(options.PlaceholderText);
			bool hasDateTime = options.DateTimeOptions != null;

			m_textContainer.gameObject.SetActive(hasText);
			if (hasText)
				m_text.text = options.Text;

			m_inputFieldContainer.gameObject.SetActive(hasInput);

			if (hasInput)
			{
				if (options.InputText != null)
				{
					m_inputField.text = options.InputText;
				}
				if (options.PlaceholderText != null)
				{
					TMP_Text placeholderText = m_inputField.placeholder.GetComponent<TMP_Text>();
					if (placeholderText != null)
						placeholderText.text = options.PlaceholderText;
				}
				UiMain.Instance.SetFocus(m_inputField);
			}

			m_dateTimePanel.gameObject.SetActive(hasDateTime);
			if (hasDateTime)
				m_dateTimePanel.SetOptions(options.DateTimeOptions);
		}
	}
}
