using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiIncDecGridPicker : UiPanel
	{
		[SerializeField] protected UiButton m_increaseButton;
		[SerializeField] protected UiButton m_decreaseButton;
		[SerializeField] protected UiButton m_button;

		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected UiGridPickerCell m_gridPickerCellPrefab;
		[SerializeField] protected List<string> m_strings;
		[SerializeField] protected int m_index = 0;
		[SerializeField] protected bool m_isLocalizable = false;
		[SerializeField] protected string m_caption;
		[SerializeField] protected int m_numColumnsLandscape = 10;
		[SerializeField] protected int m_numColumnsPortrait = 5;
		[SerializeField] protected int m_cellWidth = 150;
		[SerializeField] protected int m_cellHeight = 80;

		[Tooltip("string: changed entry\nint: changed entry index")]
		public CEvent<string, int> OnValueChanged = new();

		public int numColumns => UiUtility.GetCurrentScreenOrientation() == EScreenOrientation.Landscape
			? m_numColumnsLandscape
			: m_numColumnsPortrait;

		protected override bool NeedsLanguageChangeCallback => m_isLocalizable;

		protected override void Awake()
		{
			AddOnEnableButtonListeners
			(
				(m_increaseButton, OnIncrease),
				(m_decreaseButton, OnDecrease),
				(m_button, OnClick)
			);

			base.Awake();
		}

		protected override void OnEnable()
		{
			if (m_strings == null || m_strings.Count <= m_index)
				throw new IndexOutOfRangeException($"Index out of range in {this.GetPath()}");

			base.OnEnable();
			UpdateText();
		}

		protected void OnIncrease() => AddIndexOffset(1);

		protected void OnDecrease() => AddIndexOffset(-1);
	
		private void OnClick()
		{
			var options = new UiGridPicker.Options()
			{
				NumColumns = numColumns,
				NumRows = Mathf.CeilToInt(m_strings.Count / (float) numColumns),
				AllowOutsideTap = true,
				ShowCloseButton = false,
				Caption = _(m_caption),
				ColumnWidth = m_cellWidth,
				RowHeight = m_cellHeight,
				OnCellClicked = OnCellClicked,
				OnPopulateCell = OnPopulateCell,
				Prefab = m_gridPickerCellPrefab
			};

			UiMain.Instance.ShowGridPicker(options);
		}

		private void OnPopulateCell(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int idx = _x + _y * numColumns;
			bool isEmptyCell = idx >= m_strings.Count;

			if (isEmptyCell)
			{
				_cell.OptionalCaption = String.Empty;
				_cell.Button.Button.interactable = false;
				return;
			}

			_cell.Button.Button.interactable = true;
			_cell.OptionalCaption = m_isLocalizable ? _(m_strings[idx]) : m_strings[idx];
		}

		private void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int idx = _x + _y * numColumns;
			bool isEmptyCell = idx >= m_strings.Count;

			if (isEmptyCell)
				return;

			m_index = idx;
			OnValueChanged.InvokeOnce(m_strings[idx], idx);
			UpdateText();

			_gridPicker.Hide();
		}

		protected void AddIndexOffset(int _addend)
		{
			m_index = (m_index + _addend) % m_strings.Count;
			if (m_index < 0)
				m_index += m_strings.Count;

			OnValueChanged.InvokeOnce(m_strings[m_index], m_index);

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