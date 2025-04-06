using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiIncDecGridPicker : UiPanel
	{
		[SerializeField] protected UiButton m_increaseButton;
		[SerializeField] protected UiButton m_decreaseButton;
		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected UiGridPickerCell m_gridPickerCellPrefab;
		[SerializeField] protected List<string> m_strings;
		[SerializeField] protected int m_index = 0;
		[SerializeField] protected bool m_isLocalizable = false;

		protected override bool NeedsLanguageChangeCallback => m_isLocalizable;

		protected override void OnEnable()
		{
			if (m_strings == null || m_strings.Count <= m_index)
				throw new IndexOutOfRangeException($"Index out of range in {this.GetPath()}");

			base.OnEnable();

			m_increaseButton.OnClick.AddListener(OnIncrease);
			m_decreaseButton.OnClick.AddListener(OnDecrease);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_increaseButton.OnClick.RemoveListener(OnIncrease);
			m_decreaseButton.OnClick.RemoveListener(OnDecrease);
		}

		protected void OnIncrease() => AddIndexOffset(1);

		protected void OnDecrease() => AddIndexOffset(-1);
	
		protected void AddIndexOffset(int _addend)
		{
			m_index = (m_index + _addend) % m_strings.Count;
			if (m_index < 0)
				m_index += m_strings.Count;

			UpdateText();
		}

		protected void UpdateText()
		{
			string key = m_strings[m_index];
			string s = m_isLocalizable ? _(key) : key;
			m_text.text = s;
		}

		protected override void OnLanguageChanged(string _languageId)
		{
			base.OnLanguageChanged(_languageId);
			UpdateText();
		}
	}
}