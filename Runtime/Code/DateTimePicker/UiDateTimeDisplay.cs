using System;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiDateTimeDisplay : UiPanel
	{
		[Serializable]
		public class Options
		{
			public bool ShowDate = true;
			public bool ShowTime = true;
			public bool LongDateFormat = true;
			public bool LongTimeFormat = true;
		}

		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected SerializableDateTime m_dateTime;
		[SerializeField] protected UiDateTimePanel m_dateTimePanel;
		[SerializeField] protected Options m_options;

		public void SetOptions(Options _options)
		{
			m_options = _options;
			UpdateText();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_dateTimePanel.OnValueChanged.AddListener(OnDateTimeChanged);
			UpdateText();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_dateTimePanel.OnValueChanged.RemoveListener(OnDateTimeChanged);
		}

		private void OnDateTimeChanged(DateTime _dateTime)
		{
			m_dateTime.DateTime = _dateTime;
			UpdateText();
		}

		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnLanguageChanged(string _languageId)
		{
			base.OnLanguageChanged(_languageId);
			UpdateText();
		}

		protected virtual void UpdateText()
		{
			if (m_dateTimePanel == null)
				return;

			if (m_options == null)
				m_options = new Options();

			var dateTime = m_dateTimePanel.SelectedDateTime;

			var dateStr = m_options.ShowDate ? 
				m_options.LongDateFormat ? 
					dateTime.ToLongDateString() : 
					dateTime.ToShortDateString() :
				string.Empty;

			var timeStr = m_options.ShowTime ?
				m_options.LongTimeFormat ?
					dateTime.ToLongTimeString() :
					dateTime.ToShortTimeString() :
				string.Empty;

			var bindStr = m_options.ShowDate && m_options.ShowTime ? "\n" : string.Empty;
 			m_text.text = $"{dateStr}{bindStr}{timeStr}";
		}
	}
}