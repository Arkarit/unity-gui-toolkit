using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiKeyBindingRequester : UiRequesterBase
	{
		[SerializeField]
		protected UiButton m_restoreDefaultsButton;

		protected override void Awake()
		{
			base.Awake();
			m_restoreDefaultsButton.OnClick.AddListener(OnRestoreDefaultButton);
		}

		public void Requester( string _title = "Key Bindings", bool _allowOutsideTap = false, UnityAction _onOk = null, UnityAction _onCancel = null, string _okText = null, string _cancelText = null )
		{
			UiMain.Instance.KeyBindings.BeginChangeBindings();

			Options options = new Options
			{
				ButtonInfos = new ButtonInfo[] 
				{
					new ButtonInfo {
						Text = string.IsNullOrEmpty(_okText) ? "OK" : _okText,
						Prefab = m_okButtonPrefab,
						OnClick = () => { OnOk( _onOk ); }
					},
					new ButtonInfo {
						Text = string.IsNullOrEmpty(_cancelText) ? "Cancel" : _cancelText,
						Prefab = m_cancelButtonPrefab,
						OnClick = () => { OnCancel( _onCancel ); }
					}
				},
				AllowOutsideTap = _allowOutsideTap,
				CloseButtonIdx = _allowOutsideTap ? 1 : Constants.INVALID,
			};
			DoDialog(_title, options);
		}

		private void OnOk(UnityAction _onOk)
		{
			UiMain.Instance.KeyBindings.EndChangeBindings(true);
			_onOk?.Invoke();
		}

		private void OnCancel(UnityAction _onCancel)
		{
			UiMain.Instance.KeyBindings.EndChangeBindings(false);
			_onCancel?.Invoke();
		}

		private void OnRestoreDefaultButton()
		{
			UiMain.Instance.KeyBindings.RestoreDefaults();
			UiKeyBindingsList.EvRefreshList.Invoke();
		}

	}
}