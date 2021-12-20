using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiScrollSnapText : UiScrollSnap, ILocaClient
	{
		[SerializeField] protected GameObject m_textPrefab;
		[SerializeField] protected List<string> m_text;
		[SerializeField] protected bool m_autoTranslate;

		bool ILocaClient.UsesMultipleLocaKeys => true;
		string ILocaClient.LocaKey => null;
		List<string> ILocaClient.LocaKeys => m_autoTranslate ? m_text : null;

		protected override void Start()
		{
			base.Start();
			foreach (string text in m_text)
				InstantiateTextItem(text);
		}

		private void SetText( GameObject _go, string _text, bool _destroyOnError = true )
		{
			if (Application.isPlaying)
			{
				var translator = _go.GetComponentInChildren<UiTMPTranslator>();
				if (translator != null)
				{
					translator.Text = _text;
					return;
				}
			}

			var tmpText = _go.GetComponentInChildren<TMP_Text>();
			if (tmpText != null)
			{
				tmpText.text = _text;
				return;
			}

			var text = _go.GetComponentInChildren<Text>();
			if (text == null)
			{
				text.text = _text;
				if (_destroyOnError)
				{
					Debug.LogError("Neither Text nor text mesh pro component found");
					_go.Destroy();
				}
				return;
			}
		}

		private void InstantiateTextItem( string _text )
		{
			GameObject go = Instantiate(m_textPrefab);
			go.hideFlags = HideFlags.DontSave;

			if (m_autoTranslate)
			{
				var tmpText = go.GetComponentInChildren<TMP_Text>();
				if (tmpText == null)
					Debug.LogError("Only Text mesh pro components can be translated!");
				else
					tmpText.GetOrCreateComponent<UiTMPTranslator>();
			}

			SetText(go, _text);

			go.transform.SetParent(Content, false);
		}

		private void OnValidate()
		{
			// Avoid "SendMessage cannot be called during Awake.." issued by transform.SetParent() sending messages 
			EditorApplication.delayCall += DelayedOnValidate;
		}

		private void DelayedOnValidate()
		{
			RemoveAllItems(true);
			foreach (string text in m_text)
				InstantiateTextItem(text);
		}
	}
}