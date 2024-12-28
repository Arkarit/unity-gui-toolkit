using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class HourOptionData : UiThing
	{
		[SerializeField] private bool m_is24hour;
		[SerializeField] private UiButton m_button;
		[SerializeField] private UiGridPickerCell m_gridPickerCellPrefab;
		[SerializeField] private int m_numColumns = 6;
		[SerializeField] private int m_numRows = 4;
		[SerializeField] private int m_maxElements = 24;

		public int SelectedValue { get; internal set; }

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
				Caption = _("Set Hour"),
				ColumnWidth = 100,
				RowHeight = 50,
				MaxElements = m_maxElements,
				OnCellClicked = OnCellClicked,
				OnPopulateCell = OnPopulateCell,
				Prefab = m_gridPickerCellPrefab
			};

			UiMain.Instance.ShowGridPicker(options);
		}

		private void OnPopulateCell(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int hour = _y * m_numColumns + _x;
			DateTime time = new DateTime(2000, 1, 1, hour, 0, 0);
			_cell.OptionalCaption = time.ToShortTimeString().Replace(":00", "");
		}

		private void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
Debug.Log($"---::: Cell clicked: {_x}, {_y}");
		}
	}
}