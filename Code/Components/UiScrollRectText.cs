using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiScrollRectText : UiScrollRect, ILocaClient
	{
		[SerializeField] protected GameObject m_textPrefab;
		[SerializeField] protected List<string> m_text;
		[SerializeField] protected bool m_autoTranslate;
		[SerializeField] protected bool m_useNumbers;
		[SerializeField] protected int m_startNumber;
		[SerializeField] protected int m_endNumber;
		[SerializeField] protected bool m_pooled = true;

		bool ILocaClient.UsesMultipleLocaKeys => true;
		string ILocaClient.LocaKey => null;
		List<string> ILocaClient.LocaKeys => m_autoTranslate ? m_text : null;

		protected override void Start()
		{
			base.Start();

			RefreshItems();
			return;
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
Image i;
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
			GameObject go = m_pooled ? m_textPrefab.PoolInstantiate() : Instantiate(m_textPrefab);
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

			AddItem(go.RectTransform());
		}

		private void RefreshItems()
		{
			RemoveAllItems(true);

			if (m_useNumbers)
			{
				m_autoTranslate = false;

				for (int i = m_startNumber; i <= m_endNumber; i++)
					InstantiateTextItem(i.ToString());

				return;
			}

			foreach (string text in m_text)
				InstantiateTextItem(text);
		}

	}
}