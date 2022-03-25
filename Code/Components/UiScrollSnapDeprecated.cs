/// Credit BinaryX 
/// Sourced from - http://forum.unity3d.com/threads/scripts-useful-4-6-scripts-collection.264161/page-2#post-1945602
/// Updated by simonDarksideJ - removed dependency on a custom ScrollRect script. Now implements drag interfaces and standard Scroll Rect.
/// Update by xesenix - rewrote almost the entire code 
/// - configuration for direction move instead of 2 concurrent class (easier to change direction in editor)
/// - supports list layout with horizontal or vertical layout need to match direction with type of layout used
/// - dynamic checks if scrolled list size changes and recalculates anchor positions 
///   and item size based on itemsVisibleAtOnce and size of root container
///   if you don't wish to use this auto resize turn of autoLayoutItems
/// - fixed current page made it independent from pivot
/// - replaced pagination with delegate function
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	[RequireComponent(typeof(ScrollRect))]
	[AddComponentMenu("UI/UIToolkit/UI Scroll Snap")]
	public class UiScrollSnapDeprecated : UiThing, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollSnap
	{
		// needed because of reversed behaviour of axis Y compared to X
		// (positions of children lower in children list in horizontal directions grows when in vertical it gets smaller)
		public enum EScrollDirection
		{
			Horizontal,
			Vertical
		}

		public delegate void PageSnapChange( int page );
		public event PageSnapChange onPageChange;

		[Tooltip("Button to go to the next page. (optional)")]
		[SerializeField] protected UiButton m_nextButton;
		[Tooltip("Button to go to the previous page. (optional)")]
		[SerializeField] protected UiButton m_prevButton;
		[Tooltip("Number of items visible in one page of scroll frame.")]
		[RangeAttribute(1, 100)]
		[SerializeField] protected int m_itemsVisibleAtOnce = 1;
		[Tooltip("Sets minimum width of list items to 1/itemsVisibleAtOnce.")]
		[SerializeField] protected bool m_autoLayoutItems = true;
		[Tooltip("If you wish to update scrollbar numberOfSteps to number of active children on list.")]
		[SerializeField] protected bool m_linkScrollbarSteps = false;
		[Tooltip("If you wish to update scrollrect sensitivity to size of list element.")]
		[SerializeField] protected bool m_linkScrollrectScrollSensitivity = false;
		[SerializeField] protected bool m_useFastSwipe = true;
		[SerializeField] protected int m_fastSwipeThreshold = 100;
		[SerializeField] protected EScrollDirection m_direction = EScrollDirection.Horizontal;

		private ScrollRect m_scrollRect;
		private RectTransform m_content;
		private RectTransform m_viewport;

		private int m_pages;
		private int m_startingPage = 0;
		private bool m_dirty = true;

		// anchor points to lerp to see child on certain indexes
		private Vector3[] m_pageAnchorPositions;
		private Vector3 m_lerpTarget;
		private bool m_lerp;

		// item list related
		private float m_listContainerMinPosition;
		private float m_listContainerMaxPosition;
		private float m_listContainerSize;
		private Vector2 m_listContainerCachedSize;
		private float m_itemSize;
		private int m_itemsCount = 0;

		// drag related
		private bool m_startDrag = true;
		private Vector3 m_positionOnDragStart = new Vector3();
		private int m_pageOnDragStart;
		private bool m_fastSwipeTimer = false;
		private int m_fastSwipeCounter = 0;
		private int m_fastSwipeTarget = 10;
		private bool m_fastSwipe = false; //to determine if a fast swipe was performed

		protected ScrollRect ScrollRect
		{
			get
			{
				if (m_scrollRect == null)
					m_scrollRect = gameObject.GetComponent<ScrollRect>();
				return m_scrollRect;
			}
		}

		protected RectTransform Content
		{
			get
			{
				if (m_content == null)
					m_content = ScrollRect.content;
				return m_content;
			}
		}

		protected RectTransform Viewport
		{
			get
			{
				if (m_viewport == null)
					m_viewport = ScrollRect.viewport;
				return m_viewport;
			}
		}

		protected bool Dirty
		{
			get
			{
				if (Application.isPlaying)
					return m_dirty;
				return true;
			}
		}

		public int ItemCount => Content.childCount;

		public GameObject GetItem(int _idx)
		{
			return Content.GetChild(_idx).gameObject;
		}

		public void AddItem(GameObject _item, int _idx = -1)
		{
			_item.transform.SetParent(Content);
			if (_idx != -1)
			{
				_item.transform.SetSiblingIndex(_idx);
			}
			SetDirty();
		}

		public void RemoveItem(GameObject _item, bool _destroy = false)
		{
			_item.transform.SetParent(null);

			if (_destroy)
				_item.Destroy(false);

			SetDirty();
		}

		public void RemoveItem(int _idx, bool _destroy = false)
		{
			GameObject item = Content.GetChild(_idx).gameObject;

			item.transform.SetParent(null);

			if (_destroy)
				item.Destroy(false);

			SetDirty();
		}

		public void RemoveAllItems(bool _destroy = false)
		{
			while(ItemCount > 0)
				RemoveItem(0, _destroy);
		}


		// Use this for initialization
		protected override void Start()
		{
			base.Start();

			if (m_nextButton != null)
				m_nextButton.OnClick.AddListener(GotoNextPage);

			if (m_prevButton != null)
				m_prevButton.OnClick.AddListener(GotoPreviousPage);

			m_lerp = false;

			UpdateListItemsSize();
			UpdateListItemPositions();

			OnPageChanged(CurrentPage());

			if (ScrollRect.horizontalScrollbar != null && ScrollRect.horizontal)
			{

				var hscroll = ScrollRect.horizontalScrollbar.gameObject.GetOrCreateComponent<UiScrollSnapScrollbarHelper>();
				hscroll.m_ss = this;
			}

			if (ScrollRect.verticalScrollbar != null && ScrollRect.vertical)
			{
				var vscroll = ScrollRect.verticalScrollbar.gameObject.GetOrCreateComponent<UiScrollSnapScrollbarHelper>();
				vscroll.m_ss = this;
			}
		}

		public void UpdateListItemsSize()
		{
			if (!Dirty)
				return;

			float size = 0;

			if (m_direction == EScrollDirection.Horizontal)
				size = Viewport.rect.width / m_itemsVisibleAtOnce;
			else
				size = Viewport.rect.height / m_itemsVisibleAtOnce;

			m_itemSize = size;

			if (m_linkScrollrectScrollSensitivity)
			{
				ScrollRect.scrollSensitivity = m_itemSize;
			}

			if (m_autoLayoutItems && m_itemsCount > 0)
			{
				if (m_direction == EScrollDirection.Horizontal)
				{
					foreach (var tr in Content)
					{
						GameObject child = ((Transform)tr).gameObject;
						if (child.activeInHierarchy)
						{
							var childLayout = child.GetOrCreateComponent<LayoutElement>();
							childLayout.minWidth = m_itemSize;
							childLayout.preferredWidth = m_itemSize;
						}
					}
				}
				else
				{
					foreach (var tr in Content)
					{
						GameObject child = ((Transform)tr).gameObject;
						if (child.activeInHierarchy)
						{
							var childLayout = child.GetOrCreateComponent<LayoutElement>();
							childLayout.minHeight = m_itemSize;
							childLayout.preferredHeight = m_itemSize;
						}
					}
				}
			}

			m_dirty = false;
		}

		public void UpdateListItemPositions()
		{
			if (!Content.rect.size.Equals(m_listContainerCachedSize))
			{
				// checking how many children of list are active
				int activeCount = 0;

				foreach (var tr in Content)
				{
					if (((Transform)tr).gameObject.activeInHierarchy)
					{
						activeCount++;
					}
				}

				// if anything changed since last check reinitialize anchors list
				m_itemsCount = 0;
				Array.Resize(ref m_pageAnchorPositions, activeCount);

				if (activeCount > 0)
				{
					m_pages = Mathf.Max(activeCount - m_itemsVisibleAtOnce + 1, 1);

					if (m_direction == EScrollDirection.Horizontal)
					{
						// looking for list spanning range min/max
						ScrollRect.horizontalNormalizedPosition = 0;
						m_listContainerMaxPosition = Content.localPosition.x;
						ScrollRect.horizontalNormalizedPosition = 1;
						m_listContainerMinPosition = Content.localPosition.x;

						m_listContainerSize = m_listContainerMaxPosition - m_listContainerMinPosition;

						for (var i = 0; i < m_pages; i++)
						{
							m_pageAnchorPositions[i] = new Vector3(
								m_listContainerMaxPosition - m_itemSize * i,
								Content.localPosition.y,
								Content.localPosition.z
							);
						}
					}
					else
					{
						//Debug.Log ("-------------looking for list spanning range----------------");
						// looking for list spanning range
						ScrollRect.verticalNormalizedPosition = 1;
						m_listContainerMinPosition = Content.localPosition.y;
						ScrollRect.verticalNormalizedPosition = 0;
						m_listContainerMaxPosition = Content.localPosition.y;

						m_listContainerSize = m_listContainerMaxPosition - m_listContainerMinPosition;

						for (var i = 0; i < m_pages; i++)
						{
							m_pageAnchorPositions[i] = new Vector3(
								Content.localPosition.x,
								m_listContainerMinPosition + m_itemSize * i,
								Content.localPosition.z
							);
						}
					}

					UpdateScrollbar(m_linkScrollbarSteps);
					m_startingPage = Mathf.Min(m_startingPage, m_pages);
					ResetPage();
				}

				if (m_itemsCount != activeCount)
				{
					OnPageChanged(CurrentPage());
				}

				m_itemsCount = activeCount;
				m_listContainerCachedSize.Set(Content.rect.size.x, Content.rect.size.y);
				SetDirty();
			}
		}

		public void ResetPage()
		{
			if (m_direction == EScrollDirection.Horizontal)
			{
				ScrollRect.horizontalNormalizedPosition = m_pages > 1 ? (float)m_startingPage / (float)(m_pages - 1) : 0;
			}
			else
			{
				ScrollRect.verticalNormalizedPosition = m_pages > 1 ? (float)(m_pages - m_startingPage - 1) / (float)(m_pages - 1) : 0;
			}
		}

		private void UpdateScrollbar( bool linkSteps )
		{
			if (linkSteps)
			{
				if (m_direction == EScrollDirection.Horizontal)
				{
					if (ScrollRect.horizontalScrollbar != null)
					{
						ScrollRect.horizontalScrollbar.numberOfSteps = m_pages;
					}
				}
				else
				{
					if (ScrollRect.verticalScrollbar != null)
					{
						ScrollRect.verticalScrollbar.numberOfSteps = m_pages;
					}
				}
			}
			else
			{
				if (m_direction == EScrollDirection.Horizontal)
				{
					if (ScrollRect.horizontalScrollbar != null)
					{
						ScrollRect.horizontalScrollbar.numberOfSteps = 0;
					}
				}
				else
				{
					if (ScrollRect.verticalScrollbar != null)
					{
						ScrollRect.verticalScrollbar.numberOfSteps = 0;
					}
				}
			}
		}

		void LateUpdate()
		{
			UpdateListItemsSize();
			UpdateListItemPositions();

			if (m_lerp)
			{
				UpdateScrollbar(false);

				Content.localPosition = Vector3.Lerp(Content.localPosition, m_lerpTarget, Mathf.Clamp01(7.5f * Time.deltaTime));

				if (Vector3.Distance(Content.localPosition, m_lerpTarget) < 0.001f)
				{
					Content.localPosition = m_lerpTarget;
					m_lerp = false;

					UpdateScrollbar(m_linkScrollbarSteps);
				}

				//change the info bullets at the bottom of the screen. Just for visual effect
				if (Vector3.Distance(Content.localPosition, m_lerpTarget) < 10f)
				{
					OnPageChanged(CurrentPage());
				}
			}

			if (m_fastSwipeTimer)
			{
				m_fastSwipeCounter++;
			}
		}

		//Function for switching screens with buttons
		public void GotoNextPage()
		{
			UpdateListItemPositions();
			if (CurrentPage() < m_pages - 1)
			{
				m_lerp = true;
				m_lerpTarget = m_pageAnchorPositions[CurrentPage() + 1];

				OnPageChanged(CurrentPage() + 1);
			}
		}

		//Function for switching screens with buttons
		public void GotoPreviousPage()
		{
			UpdateListItemPositions();

			if (CurrentPage() > 0)
			{
				m_lerp = true;
				m_lerpTarget = m_pageAnchorPositions[CurrentPage() - 1];

				OnPageChanged(CurrentPage() - 1);
			}
		}

		//Because the CurrentScreen function is not so reliable, these are the functions used for swipes
		private void NextScreenCommand()
		{
			if (m_pageOnDragStart < m_pages - 1)
			{
				int targetPage = Mathf.Min(m_pages - 1, m_pageOnDragStart + m_itemsVisibleAtOnce);
				m_lerp = true;

				m_lerpTarget = m_pageAnchorPositions[targetPage];

				OnPageChanged(targetPage);
			}
		}

		//Because the CurrentScreen function is not so reliable, these are the functions used for swipes
		private void PrevScreenCommand()
		{
			if (m_pageOnDragStart > 0)
			{
				int targetPage = Mathf.Max(0, m_pageOnDragStart - m_itemsVisibleAtOnce);
				m_lerp = true;

				m_lerpTarget = m_pageAnchorPositions[targetPage];

				OnPageChanged(targetPage);
			}
		}


		//returns the current screen that the is seeing
		public int CurrentPage()
		{
			float pos;

			if (m_direction == EScrollDirection.Horizontal)
			{
				pos = m_listContainerMaxPosition - Content.localPosition.x;
				pos = Mathf.Clamp(pos, 0, m_listContainerSize);
			}
			else
			{
				pos = Content.localPosition.y - m_listContainerMinPosition;
				pos = Mathf.Clamp(pos, 0, m_listContainerSize);
			}

			float page = pos / m_itemSize;

			return Mathf.Clamp(Mathf.RoundToInt(page), 0, m_pages);
		}

		/// <summary>
		/// Added to provide a uniform interface for the ScrollBarHelper
		/// </summary>
		public void SetLerp( bool value )
		{
			m_lerp = value;
		}

		public void ChangePage( int page )
		{
			if (0 <= page && page < m_pages)
			{
				m_lerp = true;

				m_lerpTarget = m_pageAnchorPositions[page];

				OnPageChanged(page);
			}
		}

		//changes the bullets on the bottom of the page - pagination
		protected virtual void OnPageChanged( int currentPage )
		{
			if (!Application.isPlaying)
				return;

			m_startingPage = currentPage;

			if (m_nextButton)
			{
				m_nextButton.EnabledInHierarchy = currentPage < m_pages - 1;
			}

			if (m_prevButton)
			{
				m_prevButton.EnabledInHierarchy = currentPage > 0;
			}

			if (onPageChange != null)
			{
				onPageChange(currentPage);
			}
		}

		public void SetDirty()
		{
			m_dirty = true;
		}

		private void OnValidate()
		{
			UpdateListItemsSize();
			UpdateListItemPositions();
		}

		#region Interfaces
		public void OnBeginDrag( PointerEventData eventData )
		{
			UpdateScrollbar(false);

			m_fastSwipeCounter = 0;
			m_fastSwipeTimer = true;

			m_positionOnDragStart = eventData.position;
			m_pageOnDragStart = CurrentPage();
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			m_startDrag = true;
			float change = 0;

			if (m_direction == EScrollDirection.Horizontal)
			{
				change = m_positionOnDragStart.x - eventData.position.x;
			}
			else
			{
				change = -m_positionOnDragStart.y + eventData.position.y;
			}

			if (m_useFastSwipe)
			{
				m_fastSwipe = false;
				m_fastSwipeTimer = false;

				if (m_fastSwipeCounter <= m_fastSwipeTarget)
				{
					if (Math.Abs(change) > m_fastSwipeThreshold)
					{
						m_fastSwipe = true;
					}
				}
				if (m_fastSwipe)
				{
					if (change > 0)
					{
						NextScreenCommand();
					}
					else
					{
						PrevScreenCommand();
					}
				}
				else
				{
					m_lerp = true;
					m_lerpTarget = m_pageAnchorPositions[CurrentPage()];
				}
			}
			else
			{
				m_lerp = true;
				m_lerpTarget = m_pageAnchorPositions[CurrentPage()];
			}
		}

		public void OnDrag( PointerEventData eventData )
		{
			m_lerp = false;

			if (m_startDrag)
			{
				OnBeginDrag(eventData);
				m_startDrag = false;
			}
		}

		public void StartScreenChange() { }
		#endregion
	}
}