using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A scroll list that only has items for what is in view. A ScrollRect with many expensive items (3D icons,
	/// animations, nested canvases) costs for every item, seen or not; this list keeps the visible items plus a few
	/// on either side, and when one scrolls out, it is reused for the one scrolling in: same GameObject, same
	/// components, only fed with another index. Items not needed any more go back to the UiPool.
	///
	/// Usage: put it next to the ScrollRect. Author the content with a HorizontalLayoutGroup, VerticalLayoutGroup or
	/// UiHorizontalOrVerticalLayoutGroup as usual - it is what you see in the editor, and the list takes its direction,
	/// padding, spacing, child alignment and child size control from it. At runtime call
	/// SetData(prefab, count, bind); the list switches the layout group (and a ContentSizeFitter) off, sizes the
	/// content and positions the items itself.
	///
	/// Rules:
	/// - All items have the same size: the prefab's (or its preferred size, where the layout group controls it).
	/// - The bind callback sets EVERYTHING an item shows, including state like selected or locked: an item comes in
	///   with what its previous index showed. State that has to survive scrolling belongs to the data, not to the item.
	/// - The content holds the list's items only. Reverse order and grids are not supported.
	/// </summary>
	[RequireComponent(typeof(ScrollRect))]
	public class UiRecyclingList : UiThing
	{
		[Tooltip("Items kept beyond each end of the view, so a fast scroll doesn't show a gap for a frame")]
		[SerializeField][Min(0)] private int m_buffer = 1;

		private ScrollRect m_scrollRect;
		private Component m_prefab;
		private Action<Component, int> m_bind;
		private int m_count;

		// Layout, taken from the content's layout group
		private bool m_layoutTaken;
		private bool m_vertical;
		private float m_spacing;
		private RectOffset m_padding = new();
		private TextAnchor m_childAlignment = TextAnchor.UpperLeft;
		private bool m_controlMainSize;
		private bool m_controlCrossSize;
		private bool m_expandCrossSize;
		private Vector2 m_itemSize;
		private float m_preferredCross;
		private float m_crossAvailable = float.NaN;
		private bool m_itemSizeKnown;

		private readonly Dictionary<int, Component> m_items = new();
		private readonly List<Component> m_spare = new();
		private readonly List<int> m_leaving = new();
		private int m_first;
		private int m_last = -1;

		public ScrollRect ScrollRect => m_scrollRect != null ? m_scrollRect : m_scrollRect = GetComponent<ScrollRect>();
		public RectTransform Content => ScrollRect.content;

		/// <summary>The number of entries the list shows.</summary>
		public int Count => m_count;

		/// <summary>The items that exist right now (in view and the buffer), by their index.</summary>
		public IReadOnlyDictionary<int, Component> Items => m_items;

		/// <summary>
		/// Shows _count entries; _bind(item, index) fills an item for an index, whenever one comes into view. Calling
		/// it again (new data, new count) rebinds the existing items instead of creating new ones; the scroll position
		/// stays where it is.
		/// </summary>
		public void SetData<T>( T _prefab, int _count, Action<T, int> _bind ) where T : Component
		{
			if (_prefab == null)
				throw new ArgumentNullException(nameof(_prefab));

			if (m_prefab != null && m_prefab != _prefab)
				Clear();

			m_prefab = _prefab;
			m_bind = _bind == null ? null : ( item, index ) => _bind((T)item, index);
			m_count = Mathf.Max(0, _count);

			TakeLayout();
			UpdateContentSize();
			Rebuild();
		}

		/// <summary>Binds all existing items again, e.g. after the data behind them changed.</summary>
		public void Refresh()
		{
			foreach (var kv in m_items)
				Bind(kv.Value, kv.Key);
		}

		/// <summary>Binds the item of one index again, if it exists right now.</summary>
		public void Refresh( int _index )
		{
			if (m_items.TryGetValue(_index, out var item))
				Bind(item, _index);
		}

		/// <summary>The item showing _index, or null when that index is not in view (or the buffer).</summary>
		public T GetItem<T>( int _index ) where T : Component => m_items.TryGetValue(_index, out var item) ? item as T : null;

		/// <summary>Removes all items (back to the pool) and the data.</summary>
		public void Clear()
		{
			foreach (var kv in m_items)
				ReleaseItem(kv.Value);

			foreach (var item in m_spare)
				ReleaseItem(item);

			m_items.Clear();
			m_spare.Clear();
			m_count = 0;
			m_first = 0;
			m_last = -1;
			m_itemSizeKnown = false;

			if (m_layoutTaken)
				UpdateContentSize();
		}

		/// <summary>Scrolls just far enough that the item of _index is fully in view.</summary>
		public void EnsureVisible( int _index )
		{
			if (_index < 0 || _index >= m_count || !m_itemSizeKnown)
				return;

			GetView(out float viewStart, out float viewEnd);
			float start = ItemStart(_index);
			float end = start + MainSize;

			float shift = 0;
			if (start < viewStart)
				shift = start - viewStart;
			else if (end > viewEnd)
				shift = Mathf.Min(end - viewEnd, start - viewStart);

			if (Mathf.Approximately(shift, 0))
				return;

			ScrollRect.StopMovement();
			var position = Content.anchoredPosition;
			// Content runs right resp. down; to show what lies further on, it moves left resp. up
			if (m_vertical)
				position.y += shift;
			else
				position.x -= shift;

			Content.anchoredPosition = position;
			UpdateRange();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			ScrollRect.onValueChanged.AddListener(OnScrolled);
		}

		protected override void OnDisable()
		{
			ScrollRect.onValueChanged.RemoveListener(OnScrolled);
			base.OnDisable();
		}

		// Scrolling is reported, a resized viewport isn't
		private void LateUpdate() => UpdateRange();

		private void OnScrolled( Vector2 _ ) => UpdateRange();


		//
		// Layout
		//

		private float MainSize => m_vertical ? m_itemSize.y : m_itemSize.x;
		private float CrossSize => m_vertical ? m_itemSize.x : m_itemSize.y;
		private float PaddingStart => m_vertical ? m_padding.top : m_padding.left;
		private float PaddingEnd => m_vertical ? m_padding.bottom : m_padding.right;
		private float CrossPaddingStart => m_vertical ? m_padding.left : m_padding.top;
		private float CrossPaddingEnd => m_vertical ? m_padding.right : m_padding.bottom;

		// Distance of an item's start from the content's start edge (left resp. top)
		private float ItemStart( int _index ) => PaddingStart + _index * (MainSize + m_spacing);

		// The layout group stays what the editor shows; at runtime the list takes over
		private void TakeLayout()
		{
			if (m_layoutTaken)
				return;

			m_layoutTaken = true;
			var content = Content;
			m_vertical = ScrollRect.vertical && !ScrollRect.horizontal;

			if (content.TryGetComponent(out HorizontalOrVerticalLayoutGroup layoutGroup))
			{
				m_vertical = layoutGroup is VerticalLayoutGroup || (layoutGroup is UiHorizontalOrVerticalLayoutGroup switchable && switchable.Vertical);
				m_spacing = layoutGroup.spacing;
				m_padding = new RectOffset(layoutGroup.padding.left, layoutGroup.padding.right, layoutGroup.padding.top, layoutGroup.padding.bottom);
				m_childAlignment = layoutGroup.childAlignment;
				m_controlMainSize = m_vertical ? layoutGroup.childControlHeight : layoutGroup.childControlWidth;
				m_controlCrossSize = m_vertical ? layoutGroup.childControlWidth : layoutGroup.childControlHeight;
				m_expandCrossSize = m_vertical ? layoutGroup.childForceExpandWidth : layoutGroup.childForceExpandHeight;
				layoutGroup.enabled = false;
			}

			if (content.TryGetComponent(out ContentSizeFitter fitter))
				fitter.enabled = false;
		}

		// Measured on the first item: the size the layout group would have given it. A size the layout group doesn't
		// control is the prefab's own (a reused item already carries the size of its last placement).
		private void MeasureItem( RectTransform _item )
		{
			if (m_itemSizeKnown)
				return;

			m_itemSizeKnown = true;
			var prefabSize = ((RectTransform)m_prefab.transform).rect.size;
			int mainAxis = m_vertical ? 1 : 0;
			int crossAxis = 1 - mainAxis;

			if (m_controlMainSize || m_controlCrossSize)
				LayoutRebuilder.ForceRebuildLayoutImmediate(_item);

			float main = m_controlMainSize ? LayoutUtility.GetPreferredSize(_item, mainAxis) : prefabSize[mainAxis];
			m_preferredCross = m_controlCrossSize ? LayoutUtility.GetPreferredSize(_item, crossAxis) : prefabSize[crossAxis];
			m_itemSize = m_vertical ? new Vector2(0, main) : new Vector2(main, 0);
			m_crossAvailable = float.NaN;
			UpdateCrossSize();
			UpdateContentSize();
		}

		// The cross size can depend on the content's, which may only be laid out after SetData (or change later):
		// then all items are placed again. Returns whether it changed.
		private bool UpdateCrossSize()
		{
			float available = CrossAvailable;
			if (available == m_crossAvailable)
				return false;

			m_crossAvailable = available;
			float cross;
			if (m_controlCrossSize && m_expandCrossSize)
				cross = available;
			else if (m_controlCrossSize)
				cross = Mathf.Min(m_preferredCross, available);
			else
				cross = m_preferredCross;

			cross = Mathf.Max(0, cross);
			if (m_vertical)
				m_itemSize.x = cross;
			else
				m_itemSize.y = cross;

			return true;
		}

		private float CrossAvailable
		{
			get
			{
				var size = Content.rect.size;
				return (m_vertical ? size.x : size.y) - CrossPaddingStart - CrossPaddingEnd;
			}
		}

		private void UpdateContentSize()
		{
			float size = PaddingStart + PaddingEnd;
			if (m_itemSizeKnown && m_count > 0)
				size += m_count * MainSize + (m_count - 1) * m_spacing;

			Content.SetSizeWithCurrentAnchors(m_vertical ? RectTransform.Axis.Vertical : RectTransform.Axis.Horizontal, size);
		}

		// Like a layout group: anchored top left, placed by its pivot
		private void Place( RectTransform _item, int _index )
		{
			float crossFraction = m_vertical
				? (int)m_childAlignment % 3 * 0.5f
				: (int)m_childAlignment / 3 * 0.5f;
			float cross = CrossPaddingStart + (CrossAvailable - CrossSize) * crossFraction;
			float main = ItemStart(_index);

			float x = m_vertical ? cross : main;
			float y = m_vertical ? main : cross;

			_item.anchorMin = _item.anchorMax = new Vector2(0, 1);
			_item.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_itemSize.x);
			_item.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_itemSize.y);
			_item.anchoredPosition = new Vector2(x + m_itemSize.x * _item.pivot.x, -y - m_itemSize.y * (1 - _item.pivot.y));
		}

		// The view in the content's terms: distances from its start edge (left resp. top)
		private void GetView( out float _start, out float _end )
		{
			var viewport = ScrollRect.viewport != null ? ScrollRect.viewport : (RectTransform)ScrollRect.transform;
			var content = Content;
			var viewRect = viewport.rect;
			var contentRect = content.rect;
			Vector2 min = content.InverseTransformPoint(viewport.TransformPoint(viewRect.min));
			Vector2 max = content.InverseTransformPoint(viewport.TransformPoint(viewRect.max));

			if (m_vertical)
			{
				_start = contentRect.yMax - Mathf.Max(min.y, max.y);
				_end = contentRect.yMax - Mathf.Min(min.y, max.y);
			}
			else
			{
				_start = Mathf.Min(min.x, max.x) - contentRect.xMin;
				_end = Mathf.Max(min.x, max.x) - contentRect.xMin;
			}
		}


		//
		// Items
		//

		// All items again: new data or a new count
		private void Rebuild()
		{
			foreach (var kv in m_items)
				m_spare.Add(kv.Value);

			m_items.Clear();
			m_first = 0;
			m_last = -1;
			UpdateRange(true);
		}

		private void UpdateRange() => UpdateRange(false);

		private void UpdateRange( bool _force )
		{
			if (m_prefab == null || !m_layoutTaken)
				return;

			// The first item tells the size, and the size which items are in view
			if (!m_itemSizeKnown && m_count > 0)
			{
				var first = TakeItem();
				MeasureItem((RectTransform)first.transform);
				m_spare.Add(first);
			}

			if (m_itemSizeKnown && UpdateCrossSize())
			{
				foreach (var kv in m_items)
					Place((RectTransform)kv.Value.transform, kv.Key);
			}

			int from = 0, to = -1;
			if (m_count > 0)
			{
				GetView(out float viewStart, out float viewEnd);
				float step = MainSize + m_spacing;
				if (step > 0)
				{
					from = Mathf.FloorToInt((viewStart - PaddingStart) / step) - m_buffer;
					to = Mathf.FloorToInt((viewEnd - PaddingStart) / step) + m_buffer;
				}
				else
				{
					from = 0;
					to = m_count - 1;
				}

				from = Mathf.Clamp(from, 0, m_count - 1);
				to = Mathf.Clamp(to, -1, m_count - 1);
			}

			if (!_force && from == m_first && to == m_last)
				return;

			m_first = from;
			m_last = to;

			// What left the range first, so its items serve what came in
			m_leaving.Clear();
			foreach (var kv in m_items)
			{
				if (kv.Key < from || kv.Key > to)
					m_leaving.Add(kv.Key);
			}

			foreach (int index in m_leaving)
			{
				m_spare.Add(m_items[index]);
				m_items.Remove(index);
			}

			for (int index = from; index <= to; index++)
			{
				if (m_items.ContainsKey(index))
					continue;

				var item = TakeItem();
				m_items.Add(index, item);
				Place((RectTransform)item.transform, index);
				Bind(item, index);
			}

			// Not needed in this frame: back to the pool
			foreach (var item in m_spare)
				ReleaseItem(item);

			m_spare.Clear();
		}

		private Component TakeItem()
		{
			if (m_spare.Count > 0)
			{
				var spare = m_spare[^1];
				m_spare.RemoveAt(m_spare.Count - 1);
				return spare;
			}

			// Not UiPool.Get<T>: with T = Component it would hand out the first component, the RectTransform
			var pool = UiPool.Instance;
			Component item;
			if (pool != null)
			{
				item = pool.Get(m_prefab.gameObject, Content).GetComponent(m_prefab.GetType());
				if (item is IPoolable poolable)
					poolable.OnPoolCreated();
			}
			else
			{
				item = Instantiate(m_prefab, Content);
			}

			item.gameObject.SetActive(true);
			return item;
		}

		private void ReleaseItem( Component _item )
		{
			if (_item == null)
				return;

			var pool = UiPool.Instance;
			if (pool != null)
				pool.Release(_item);
			else
				_item.gameObject.SafeDestroy();
		}

		private void Bind( Component _item, int _index )
		{
			try
			{
				m_bind?.Invoke(_item, _index);
			}
			catch (Exception e)
			{
				UiLog.LogError($"[UiRecyclingList] Binding index {_index} failed: {e}");
			}
		}
	}
}
