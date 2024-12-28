using System;
using UnityEngine;

namespace GuiToolkit
{
	public class TimeDatePanel : TimeDatePanelBase
	{
		[SerializeField] private DateTimeHelpers.EDateTimeType m_type;

		protected override void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			base.OnCellClicked(_gridPicker, _x, _y, _cell);

Debug.Log($"---::: Cell clicked: {_x}, {_y} : {SelectedValue} {m_text.text}");
		}

		protected override string GetContentString(int val) => DateTimeHelpers.GetDateTimePartAsString(val, m_type);
	}
}