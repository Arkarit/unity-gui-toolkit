using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
		[SerializeField] private RectTransform m_dialogPanel;
		[SerializeField][Min(1f)] private float m_minBodyFontSize = 8f;

		private float m_originalBodyFontSize;
		private bool m_originalBodyAutoSizing;
		private bool m_pendingFit;

		protected override void Awake()
		{
			base.Awake();
			if (m_text != null)
			{
				m_originalBodyFontSize = m_text.fontSize;
				m_originalBodyAutoSizing = m_text.enableAutoSizing;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_pendingFit)
			{
				m_pendingFit = false;
				if (m_dialogPanel != null && m_text != null)
					StartCoroutine(FitTextToScreen());
			}
		}

		private IEnumerator FitTextToScreen()
		{
			// WaitForEndOfFrame ensures the canvas layout pass has fully completed
			yield return new WaitForEndOfFrame();

			if (IsDialogInsideScreen())
				yield break;

			// Capture the actual rendered font size (works for both auto-sizing and fixed-size text)
			float fontSize = m_text.fontSize;
			if (fontSize <= 0)
				fontSize = m_originalBodyFontSize;
			if (fontSize <= 0)
				yield break;

			// Dialog extends outside screen — reduce body font size until it fits or minimum is reached
			m_text.enableAutoSizing = false;
			m_text.fontSize = fontSize;
			LayoutRebuilder.ForceRebuildLayoutImmediate(m_dialogPanel);

			while (!IsDialogInsideScreen() && fontSize > m_minBodyFontSize)
			{
				fontSize = Mathf.Max(fontSize - 1f, m_minBodyFontSize);
				m_text.fontSize = fontSize;
				LayoutRebuilder.ForceRebuildLayoutImmediate(m_dialogPanel);
			}
		}

		private bool IsDialogInsideScreen()
		{
			Canvas rootCanvas = GetComponentInParent<Canvas>();
			if (rootCanvas != null)
				rootCanvas = rootCanvas.rootCanvas;
			if (rootCanvas == null)
				return true;

			// Compare dialog corners against canvas corners — both in world space.
			// This avoids any Screen.width/height vs CanvasScaler coordinate mismatch.
			var canvasCorners = new Vector3[4];
			rootCanvas.GetComponent<RectTransform>().GetWorldCorners(canvasCorners);
			float minX = canvasCorners[0].x;
			float minY = canvasCorners[0].y;
			float maxX = canvasCorners[2].x;
			float maxY = canvasCorners[2].y;

			var dialogCorners = new Vector3[4];
			m_dialogPanel.GetWorldCorners(dialogCorners);

			foreach (var corner in dialogCorners)
			{
				if (corner.x < minX || corner.x > maxX || corner.y < minY || corner.y > maxY)
					return false;
			}
			return true;
		}

		public void Requester( Options _options ) => DoDialog(_options);

		public UiRequester Requester( string _title, string _text, Options _options, Func<Options, Options> _modifyOptions )
		{
			_options.Title = _title;
			_options.Text = _text;
			ModifyOptionsIfNecessary(ref _options, _modifyOptions);

			DoDialog(_options);
			return this;
		}

		public UiRequester OkRequester
		(
			string _title,
			string _text,
			UnityAction _onOk = null,
			string _okText = null,
			bool _allowOutsideTap = true,
			Func<Options, Options> _modifyOptions = null
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

			return Requester(_title, _text, options, _modifyOptions);
		}

		// Waits until dialog is done; no result returned.
		public async Task OkRequesterBlocking
		(
			string _title,
			string _text,
			string _okText = null,
			bool _allowOutsideTap = true,
			bool _waitForClose = true,
			Func<Options, Options> _modifyOptions = null
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

			ModifyOptionsIfNecessary(ref options, _modifyOptions);

			if (_waitForClose)
				await DoDialogAwaitCloseAsync(options);
			else
				await DoDialogAwaitClickAsync(options);
		}

		public UiRequester YesNoRequester
		(
			string _title,
			string _text,
			bool _allowOutsideTap,
			UnityAction _onOk,
			UnityAction _onCancel = null,
			string _yesText = null,
			string _noText = null,
			Func<Options, Options> _modifyOptions = null
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

			return Requester(_title, _text, options, _modifyOptions);
		}

		public UiRequester TwoOptionsRequester
		(
			string _title,
			string _text,
			bool _allowOutsideTap,
			string _option1Text,
			string _option2Text,
			UnityAction _onOption1,
			UnityAction _onOption2,
			Func<Options, Options> _modifyOptions = null
		)
		{
			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[]
				{
					new ButtonInfo
					{
						Text = _option1Text,
						Prefab = UiMain.Instance.StandardButtonPrefab,
						OnClick = _onOption1
					},
					new ButtonInfo
					{
						Text = _option2Text,
						Prefab = UiMain.Instance.StandardButtonPrefab,
						OnClick = _onOption2
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				ShowCloseButton = _allowOutsideTap,
				CloseButtonAction = _allowOutsideTap ? _onOption2 : null,
				Text = _text,
			};

			return Requester(_title, _text, options, _modifyOptions);
		}

		public async Task<bool> YesNoRequesterBlocking
		(
			string _title,
			string _text,
			bool _allowOutsideTap = true,
			bool _waitForClose = true,
			string _yesText = null,
			string _noText = null,
			Func<Options, Options> _modifyOptions = null
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

			ModifyOptionsIfNecessary(ref options, _modifyOptions);

			int idx;

			if (_waitForClose)
				idx = await DoDialogAwaitCloseAsync(options);
			else
				idx = await DoDialogAwaitClickAsync(options);

			return IsOk(idx);
		}

		private bool IsOk( int _idx ) => _idx == (m_cancelButtonsLeftSide ? 1 : 0);

		public UiRequester OkCancelInputRequester
		(
			string _title,
			string _text,
			bool _allowOutsideTap,
			UnityAction<string> _onOk,
			UnityAction _onCancel = null,
			string _placeholderText = null,
			string _inputText = null,
			string _yesText = null,
			string _noText = null,
			Func<Options, Options> _modifyOptions = null
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

			return Requester(_title, _text, options, _modifyOptions);
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
			string _noText = null,
			Func<Options, Options> _modifyOptions = null
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

			ModifyOptionsIfNecessary(ref options, _modifyOptions);

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
			// Restore original font settings before each use (pool-reuse safety)
			if (m_text != null)
			{
				m_text.enableAutoSizing = m_originalBodyAutoSizing;
				m_text.fontSize = m_originalBodyFontSize;
			}
			m_pendingFit = m_dialogPanel != null && m_text != null;

			base.EvaluateOptions(_options);
			var options = (Options)_options;

			bool hasText = !string.IsNullOrEmpty(options.Text);
			bool hasInput =
				!string.IsNullOrEmpty(options.PlaceholderText)
				|| !string.IsNullOrEmpty(options.InputText);
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

		private static void ModifyOptionsIfNecessary( ref Options _options, Func<Options, Options> _modifyOptions )
		{
			if (_modifyOptions != null)
				_options = _modifyOptions(_options);
		}
	}
}
