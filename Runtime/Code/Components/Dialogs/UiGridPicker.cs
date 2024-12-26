using System.Collections.Generic;
using GuiToolkit;
using UnityEngine;
using UnityEngine.UI;
using GridPickerEvent = GuiToolkit.CEvent<UiGridPicker, int, int, UnityEngine.RectTransform>;
using GridPickerAction = UnityEngine.Events.UnityAction<UiGridPicker, int, int, UnityEngine.RectTransform>;
using TMPro;

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
	}

	[SerializeField] private GridLayoutGroup m_gridLayout;
	[SerializeField] private RectTransform m_prefab;
	[SerializeField] private UiButton m_closeButton;
	[SerializeField] private TextMeshProUGUI m_title;

	private Options m_options;
	private readonly List<RectTransform> m_cells = new ();

	private RectTransform GridTransform => (RectTransform) m_gridLayout.transform;

	public readonly GridPickerEvent OnPopulateCell = new();
	public readonly GridPickerEvent OnDestroyCell = new();
	public readonly GridPickerEvent OnCellClicked = new();
	
	public void GridPicker(Options _options)
	{
		m_options = _options;
	}

	public override void OnBeginShow()
	{
		base.OnBeginShow();
		if (m_options == null)
			return;

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

		OnPopulateCell.AddListener(m_options.OnPopulateCell);
		OnCellClicked.AddListener(m_options.OnCellClicked);
		if (m_options.OnDestroyCell != null )
			OnDestroyCell.AddListener(m_options.OnDestroyCell);

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
				OnPopulateCell.Invoke(this, x, y, cell);
				m_cells.Add(cell);
			}
		}
	}

	public override void OnEndHide()
	{
		base.OnEndHide();
		foreach (var cell in m_cells)
			cell.PoolDestroy();
	}
}
