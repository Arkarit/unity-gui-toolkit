using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiScrollSnapText : UiScrollSnap
	{
		[SerializeField] protected GameObject m_textPrefab;
		[SerializeField] protected List<string> m_text;

		protected override void Start()
		{
			base.Start();
			foreach (string text in m_text)
				InstantiateTextItem(text);
		}

		private void InstantiateTextItem( string _text )
		{
			GameObject go = Instantiate(m_textPrefab);
			go.hideFlags = HideFlags.DontSave;

			var tmpText = go.GetComponentInChildren<TMP_Text>();
			if (tmpText != null)
			{
				tmpText.text = _text;
			}
			else
			{
				var text = go.GetComponentInChildren<Text>();
				if (text == null)
				{
					text.text = _text;
					Debug.LogError("Neither Text nor text mesh pro component found");
					go.Destroy();
					return;
				}
			}

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