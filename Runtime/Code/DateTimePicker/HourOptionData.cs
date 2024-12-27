using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class HourOptionData : UiThing
	{
		[SerializeField] private bool m_is24hour;
		[SerializeField] private UiButton m_button;

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
				NumColumns = 12,
				NumRows = 2,
				AllowOutsideTap = true,
				ShowCloseButton = false,
				Caption = _("Set Hour"),
				ColumnWidth = 100,
				RowHeight = 50,
				MaxElements = 24,
				OnCellClicked = OnCellClicked,
				OnPopulateCell = OnPopulateCell,
			};

			UiMain.Instance.ShowGridPicker(options);
		}

		private void OnPopulateCell(UiGridPicker arg0, int arg1, int arg2, RectTransform arg3)
		{
		}

		private void OnCellClicked(UiGridPicker _gridPicker, int x, int y, RectTransform _cellParent)
		{
		}
	}
}