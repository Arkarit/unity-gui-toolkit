using System;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class TimeDatePanelBase : UiPanel
	{
		[SerializeField] private TMP_Text m_text;
		[SerializeField] private string m_Caption;
		[SerializeField] private UiButton m_button;
		[SerializeField] private UiGridPickerCell m_gridPickerCellPrefab;
		[SerializeField] private int m_numColumns = 6;
		[SerializeField] private int m_numRows = 4;
		[SerializeField] private int m_maxElements = 24;

		public int SelectedValue { get; set; }

		protected override void OnEnable()
		{
			base.OnEnable();
			m_button.OnClick.AddListener(OnClick);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_button.OnClick.RemoveListener(OnClick);
		}

		private void OnClick()
		{
			var options = new UiGridPicker.Options()
			{
				NumColumns = m_numColumns,
				NumRows = m_numRows,
				AllowOutsideTap = true,
				ShowCloseButton = false,
				Caption = _(m_Caption),
				ColumnWidth = 100,
				RowHeight = 50,
				MaxElements = m_maxElements,
				OnCellClicked = OnCellClicked,
				OnPopulateCell = OnPopulateCell,
				Prefab = m_gridPickerCellPrefab
			};

			UiMain.Instance.ShowGridPicker(options);
		}

		protected abstract string GetContentString(int val);

		protected virtual void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int val = _y * m_numColumns + _x;
			m_text.text = GetContentString(val);
			_gridPicker.Hide();
		}

		protected virtual void OnPopulateCell(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int val = _y * m_numColumns + _x;
			_cell.OptionalCaption = GetContentString(val);
		}
	}
}