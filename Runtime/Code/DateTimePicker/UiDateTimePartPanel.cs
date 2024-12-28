using System;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiDateTimePartPanel : UiPanel
	{
		[SerializeField] protected DateTimeHelpers.EDateTimeType m_type;
		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected string m_Caption;
		[SerializeField] protected UiButton m_button;
		[SerializeField] protected UiGridPickerCell m_gridPickerCellPrefab;
		[SerializeField] protected int m_numColumns = 6;
		[SerializeField] protected int m_numRows = 4;
		[SerializeField] protected int m_maxElements = 24;

		public int Value { get; set; }
		public DateTimeHelpers.EDateTimeType DateTimeType => m_type;

		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnLanguageChanged(string _languageId)
		{
			base.OnLanguageChanged(_languageId);
			m_text.text = GetContentString(Value);
		}

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


		protected virtual void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int val = _y * m_numColumns + _x;
			Value = val;
			m_text.text = GetContentString(Value);
			_gridPicker.Hide();
Debug.Log($"---::: Cell clicked: {_x}, {_y} : {Value} {m_text.text}");
		}

		protected virtual void OnPopulateCell(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			int val = _y * m_numColumns + _x;
Debug.Log($"---::: OnPopulateCell: {_x}, {_y} : {val} {m_type}");
			_cell.OptionalCaption = GetContentString(val);
		}

		protected virtual string GetContentString(int val) => DateTimeHelpers.GetDateTimePartAsString(val, m_type);
	}
}