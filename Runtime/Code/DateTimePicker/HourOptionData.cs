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
				NumColumns = 6,
				NumRows = 4,
				AllowOutsideTap = true,
				ShowCloseButton = false,
				Caption = _("Set Hour"),
				ColumnWidth = 100,
				RowHeight = 50,
				MaxElements = 24,
				OnCellClicked = OnCellClicked,
				OnPopulateCell = OnPopulateCell,
				Prefab = m_gridPickerCellPrefab
			};

			UiMain.Instance.ShowGridPicker(options);
		}

		private void OnPopulateCell(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
_cell.OptionalCaption = $"{_x}/{_y}";
		}

		private void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
Debug.Log($"---::: Cell clicked: {_x}, {_y}");
		}
	}
}