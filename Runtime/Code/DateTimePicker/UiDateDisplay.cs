using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiDateDisplay : UiPanel
	{
		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected SerializableDateTime m_dateTime;
		[SerializeField] protected UiDateTimePanel m_dateTimePanel;

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

			var dateTime = m_dateTimePanel.SelectedDateTime;
			m_text.text = dateTime.ToLongDateString();
		}
	}
}