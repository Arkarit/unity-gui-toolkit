using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Threading.Tasks;

namespace GuiToolkit
{
	public abstract class UiRequesterBase : UiView
	{
		public sealed class RequesterHandle
		{
			private readonly TaskCompletionSource<int> m_buttonTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			private readonly TaskCompletionSource<bool> m_closedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
			private UiRequesterBase m_requester;

			public Task<int> Clicked => m_buttonTcs.Task;
			public Task Closed => m_closedTcs.Task;
			
			public UiRequesterBase Requester => m_requester;

			// -1: X Button 0...n Buttons
			public void MarkButton( int _buttonIdx )
			{
				m_buttonTcs.TrySetResult(_buttonIdx);
			}

			public void MarkClosed()
			{
				m_closedTcs.TrySetResult(true);
			}
			
			public RequesterHandle(UiRequesterBase _uiRequester)
			{
				m_requester = _uiRequester;
			}
		}

		public enum EInvalidButtonsHandling
		{
			None,
			Hide,
			SetNonActive
		}
		
		public class ButtonInfo
		{
			public string Text;
			public UiButton Prefab;
			public UnityAction OnClick;
			public bool CloseRequester = true;
			public bool IsValid = true;
			public EInvalidButtonsHandling InvalidButtonsHandling = EInvalidButtonsHandling.SetNonActive;
		}

		public class Options
		{
			public string Title = string.Empty;
			public ButtonInfo[] ButtonInfos = Array.Empty<ButtonInfo>();
			public bool AllowOutsideTap = true;
			public bool ShowCloseButton = true;
			public UnityAction CloseButtonAction = null;
			public EUiLayerDefinition Layer = EUiLayerDefinition.ModalStack;
			public bool UseParent = false;
			public RectTransform Parent = null;
			public bool UseCanvasSortingOrder = false;
			public int CanvasSortingOrder = 0;
		}

		[SerializeField] protected TextMeshProUGUI m_title;
		[FormerlySerializedAs("m_closeButton")]
		[SerializeField] protected UiButton m_optionalCloseButton;
		[SerializeField] protected GameObject m_buttonContainer;
		[SerializeField] protected float m_buttonScale = 1.0f;
		[SerializeField] protected int m_maxButtons = 3;
		[SerializeField] protected bool m_cancelButtonsLeftSide = false;

		private readonly List<UiButton> m_buttons = new();
		private readonly List<UnityAction> m_listeners = new();
		private UnityAction m_closeButtonAction;
		private RequesterHandle m_requesterHandle;
		private bool m_consumed;
		private Options m_options;

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;
		public override bool ShowDestroyFieldsInInspector => false;

		public static ButtonInfo[] CreateButtonInfos( bool _useOkCancelPrefabs, params (string text, UnityAction onClick)[] _buttons )
		{
			ButtonInfo[] result = new ButtonInfo[_buttons.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = new ButtonInfo();
				result[i].Text = _buttons[i].text;
				result[i].OnClick = _buttons[i].onClick;

				var text = _buttons[i].text.ToLower();
				bool isOk = _useOkCancelPrefabs && (text == "ok" || text == "yes");
				bool isCancel = _useOkCancelPrefabs && (text == "cancel" || text == "no");

				UiButton prefab;
				if (isOk)
					prefab = UiMain.Instance.OkButtonPrefab;
				else if (isCancel)
					prefab = UiMain.Instance.CancelButtonPrefab;
				else
					prefab = UiMain.Instance.StandardButtonPrefab;

				result[i].Prefab = prefab;
			}

			return result;
		}

		public static ButtonInfo[] CreateButtonInfos( params (string text, UnityAction onClick)[] _buttons ) =>
			CreateButtonInfos(true, _buttons);

		protected RequesterHandle DoDialog( Options _options, Func<Options, Options> _modifyOptions)
		{
			if (!string.IsNullOrEmpty(_options.Title))
				m_title.text = _options.Title;

			Clear();
			EvaluateOptions(_options, _modifyOptions);
			UiMain.Instance.SortViews();
			if (m_options.UseParent)
				transform.SetParent(m_options.Parent);
			
			if (m_options.UseCanvasSortingOrder)
			{
				Canvas.overrideSorting = true;
				Canvas.sortingOrder = m_options.CanvasSortingOrder;
			}
			
			gameObject.SetActive(true);
			ShowTopmost();
			return m_requesterHandle;
		}

		// Waits until a button is clicked; returns button index (-1 = dismissed).
		protected Task<int> DoDialogAwaitClickAsync( Options _options, Func<Options, Options> _modifyOptions )
		{
			RequesterHandle handle = DoDialog(_options, _modifyOptions);
			return handle.Clicked;
		}

		// Waits until the dialog is fully closed (outro finished),
		// but still returns which button was clicked (-1 = dismissed).
		protected async Task<int> DoDialogAwaitCloseAsync( Options _options, Func<Options, Options> _modifyOptions )
		{
			RequesterHandle handle = DoDialog(_options, _modifyOptions);

			int idx = await handle.Clicked;
			await handle.Closed;

			return idx;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Clear();
		}

		public override void OnPoolReleased()
		{
			base.OnPoolReleased();
			Clear();
		}

		protected virtual void Clear()
		{
			Debug.Assert(m_buttons.Count == m_listeners.Count);

			// complete previous handle (if any)
			if (m_requesterHandle != null)
			{
				m_requesterHandle.MarkButton(-1);
				m_requesterHandle.MarkClosed();
			}

			m_requesterHandle = new RequesterHandle(this);
			m_consumed = false;
			SetAllButtonsInteractability(true);

			for (int i = 0; i < m_buttons.Count; i++)
			{
				m_buttons[i].OnClick.RemoveAllListeners();
				m_buttons[i].PoolDestroy();
			}

			if (m_optionalCloseButton)
			{
				m_optionalCloseButton.Button.interactable = true;
				m_optionalCloseButton.OnClick.RemoveListener(OnCloseButton);
			}

			m_buttons.Clear();
			m_listeners.Clear();
			OnClickCatcher = null;
			m_closeButtonAction = null;
			m_options = null;
		}

		protected virtual void EvaluateOptions( Options _options, Func<Options, Options> _modifyOptions )
		{
			if (_modifyOptions != null)
				_options = _modifyOptions(_options);
			
			m_options = _options;
			
			bool closable =
				_options.AllowOutsideTap
				|| (_options.ShowCloseButton && m_optionalCloseButton != null)
				|| _options.ButtonInfos.Length > 0;

			if (!closable)
				throw new InvalidOperationException("Dialog has no close path (no buttons, no outside tap, no X).");

			if (m_maxButtons != Constants.INVALID && _options.ButtonInfos.Length > m_maxButtons)
				UiLog.LogWarning($"Dialog '{gameObject}' contains {_options.ButtonInfos.Length} buttons; maximum supported are {m_maxButtons}. Visual problems may appear.");

			m_layer = _options.Layer;
			m_buttonContainer.SetActive(_options.ButtonInfos.Length > 0);
			for (int i = 0; i < _options.ButtonInfos.Length; i++)
			{
				ButtonInfo bi = _options.ButtonInfos[i];
				Debug.Assert(!string.IsNullOrEmpty(bi.Text) && bi.Prefab != null, $"Wrong button info number {i} - either text or prefab not set");
				if (string.IsNullOrEmpty(bi.Text) || bi.Prefab == null)
					continue;

				UiButton button = bi.Prefab.PoolInstantiate();
				button.transform.SetParent(m_buttonContainer.transform, false);
				button.transform.localScale = Vector3.one * m_buttonScale;

				if (!bi.IsValid)
				{
					switch (bi.InvalidButtonsHandling)
					{
						case EInvalidButtonsHandling.None:
							break;
						case EInvalidButtonsHandling.Hide:
							button.gameObject.SetActive(false);
							break;
						case EInvalidButtonsHandling.SetNonActive:
							button.Button.interactable = false;
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
				
				m_buttons.Add(button);
				m_listeners.Add(bi.OnClick);

				// in a real programming language you would be able to declare the lambda capture mode (ref or copy)
				int buttonIdx = i;
				button.OnClick.AddListener(() =>
				{
					OnClick(buttonIdx);
				});

				button.Text = bi.Text;
			}

			if (m_cancelButtonsLeftSide && _options.ButtonInfos.Length > 1)
			{
				m_buttons[0].transform.SetAsLastSibling();
				m_buttons[m_buttons.Count - 1].transform.SetAsFirstSibling();
			}

			if (m_optionalCloseButton)
			{
				m_closeButtonAction = _options.CloseButtonAction;
				m_optionalCloseButton.OnClick.AddListener(OnCloseButton);
			}

			if (_options.AllowOutsideTap)
				OnClickCatcher = OnCloseButton;
			else
				OnClickCatcher = Wiggle;

			if (!m_optionalCloseButton)
				return;

			bool hasCloseButton = _options.ShowCloseButton;
			m_optionalCloseButton.gameObject.SetActive(hasCloseButton);
		}

		private void OnClick( int _idx ) => HandleButtonClick(_idx);
		private void OnCloseButton() => HandleButtonClick(-1);

		private void HandleButtonClick( int _idx )
		{
			bool idxIsCloseButton = _idx == -1;
			bool buttonClosesRequester = idxIsCloseButton || m_options.ButtonInfos[_idx].CloseRequester;
			Debug.Assert(_idx < m_listeners.Count && _idx >= -1);
			
			if (buttonClosesRequester)
			{
				if (m_consumed)
					return;

				m_consumed = true;
				SetAllButtonsInteractability(false);
			}

			try
			{
				if (_idx == -1)
				{
					m_closeButtonAction?.Invoke();
				}
				else
				{
					if (_idx < m_listeners.Count && m_listeners[_idx] != null)
						m_listeners[_idx]();
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				if (buttonClosesRequester)
				{
					m_requesterHandle.MarkButton(_idx);
					Hide(false, () => m_requesterHandle.MarkClosed());
				}
			}
		}

		private void SetAllButtonsInteractability( bool _isInteractable )
		{
			foreach (var button in m_buttons)
				button.Button.interactable = _isInteractable;

			if (m_optionalCloseButton)
				m_optionalCloseButton.Button.interactable = _isInteractable;
		}

		private void Wiggle()
		{
			foreach (var button in m_buttons)
				if (button != null && button.gameObject.activeInHierarchy)
					button.Wiggle();
		}
	}
}