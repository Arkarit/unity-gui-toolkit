using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GridPickerEvent = GuiToolkit.CEvent<GuiToolkit.UiGridPicker, int, int, GuiToolkit.UiGridPickerCell>;
using GridPickerAction = UnityEngine.Events.UnityAction<GuiToolkit.UiGridPicker, int, int, GuiToolkit.UiGridPickerCell>;
using TMPro;

namespace GuiToolkit
{
	public class UiGridPicker : UiView
	{
		public class Options
		{
			public string Caption;
			public bool AllowOutsideTap = true;
			public bool ShowCloseButton = false;
			public int NumColumns;
			public int NumRows;
			public int MaxElements = -1;
			public GridPickerAction OnPopulateCell;
			public GridPickerAction OnCellClicked;
			public GridPickerAction OnDestroyCell = null;
			public float ColumnWidth = 100;
			public float RowHeight = 50;
			public UiGridPickerCell Prefab = null;
		}

		[SerializeField] private GridLayoutGroup m_gridLayout;
		[SerializeField] private UiGridPickerCell m_defaultPrefab;
		[SerializeField] private UiButton m_closeButton;
		[SerializeField] private TextMeshProUGUI m_title;

		private UiGridPickerCell m_prefab;
		private Options m_options;
		private readonly List<UiGridPickerCell> m_cells = new();

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;
		private RectTransform GridTransform => (RectTransform)m_gridLayout.transform;

		public readonly GridPickerEvent EvOnPopulateCell = new();
		public readonly GridPickerEvent EvOnDestroyCell = new();
		public readonly GridPickerEvent EvOnCellClicked = new();

		public void SetOptions( Options _options )
		{
			m_options = _options;
		}

		public override void OnBeginShow()
		{
			base.OnBeginShow();
			if (m_options == null)
				return;

			m_prefab = m_options.Prefab ? m_options.Prefab : m_defaultPrefab;

			if (m_options.AllowOutsideTap)
				OnClickCatcher = () => Hide();

			if (m_options.ShowCloseButton)
				m_closeButton.OnClick.AddListener(() => Hide());

			GridTransform.DestroyAllChildren();

			GridTransform.sizeDelta = new Vector2
			(
				m_options.NumColumns * m_options.ColumnWidth,
				m_options.NumRows * m_options.RowHeight
			);

			m_gridLayout.cellSize = new Vector2
			(
				m_options.ColumnWidth,
				m_options.RowHeight
			);

			m_gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			m_gridLayout.constraintCount = m_options.NumColumns;

			EvOnPopulateCell.AddListener(m_options.OnPopulateCell);
			EvOnCellClicked.AddListener(m_options.OnCellClicked);
			if (m_options.OnDestroyCell != null)
				EvOnDestroyCell.AddListener(m_options.OnDestroyCell);

			m_closeButton.gameObject.SetActive(m_options.ShowCloseButton);
			bool hasTitle = !string.IsNullOrEmpty(m_options.Caption);
			m_title.gameObject.SetActive(hasTitle);
			if (hasTitle)
				m_title.text = _(m_options.Caption);

			for (int y = 0; y < m_options.NumRows; y++)
			{
				for (int x = 0; x < m_options.NumColumns; x++)
				{
					if (m_options.MaxElements > 0)
					{
						if (y * m_options.NumColumns + x > m_options.MaxElements)
							return;
					}

					var cell = m_prefab.PoolInstantiate(GridTransform);
					int xx = x;
					int yy = y;
					cell.Button.OnClick.AddListener(() => { EvOnCellClicked.Invoke(this, xx, yy, cell); });

					EvOnPopulateCell.Invoke(this, x, y, cell);
					m_cells.Add(cell);
				}
			}
		}

		public override void OnEndHide()
		{
			base.OnEndHide();
			foreach (var cell in m_cells)
			{
				cell.Button.OnClick.RemoveAllListeners();
				cell.PoolDestroy();
			}

			m_closeButton.OnClick.RemoveAllListeners();
			EvOnPopulateCell.RemoveAllListeners();
			EvOnDestroyCell.RemoveAllListeners();
			EvOnCellClicked.RemoveAllListeners();
		}
	}
}