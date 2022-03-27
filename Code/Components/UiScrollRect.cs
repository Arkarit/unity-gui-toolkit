using DigitalRuby.Tween;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(ScrollRect))]
	public class UiScrollRect : UiThing
	{
		[SerializeField] protected bool m_useInitialNormalizedPosition;
		[SerializeField] protected Vector2 m_initialNormalizedPosition = new Vector2(0, 1);

		[Tooltip("Duration of tween to make child visible. 0 means instant.")]
		[SerializeField] protected float m_ensureChildVisibilityDuration = 0.3f;

		[Tooltip("Padding (Fraction of child width). If a child isn't the first or the last child, it makes sense to also display the neighbor tabs. This distance is used for that.")]
		[SerializeField] protected float m_ensureChildVisibilityPadding = 0.4f;

		[Tooltip("Snapping enabled or disabled")]
		[SerializeField] protected bool m_snappingEnabled;

		[Tooltip("Scroll rect velocity, below which it is snapped")]
		[SerializeField] protected float m_snapBelowSpeed = 50.0f;

		private ScrollRect m_scrollRect;
		private Vector3Tween m_ensureChildVisibilityTween = null;
		private RectTransform m_content;
		private RectTransform m_viewport;
		private bool m_moving;

		#region Getters
		protected bool IsEnsureChildVisibilityAnimated => !Mathf.Approximately(m_ensureChildVisibilityDuration, 0);

		protected ScrollRect ScrollRect
		{
			get
			{
				if (m_scrollRect == null)
					m_scrollRect = GetComponent<ScrollRect>();
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
		#endregion

		#region Add, remove and find Items
		public virtual void AddItem( RectTransform _item, int _idx = -1 )
		{
			if (_item.parent == Content)
				throw new ArgumentException($"Can not add '{_item.gameObject.name}' to UiScrollRect '{gameObject.name}': Item was already added");

			_item.SetParent(Content, false);
			if (_idx != -1)
				_item.SetSiblingIndex(_idx);
		}

		public virtual void AddPooledItem( GameObject _prefab, int _idx = -1 )
		{
			AddPooledItem((RectTransform)_prefab.transform);
		}

		public virtual void AddPooledItem( RectTransform _prefabRt, int _idx = -1 )
		{
			RectTransform rt = _prefabRt.PoolInstantiate();
		}

		public virtual void RemoveItem( RectTransform _item, bool _destroy = true )
		{
			if (_item.parent != Content)
				throw new ArgumentException($"Can not remove '{_item.gameObject.name}' from UiScrollRect '{gameObject.name}': Item not child of the scroll rect");

			_item.transform.SetParent(null);

			if (_destroy)
				_item.PoolDestroy();
		}

		public virtual void RemoveItem( int _idx, bool _destroy = false ) => RemoveItem(GetContentChild(_idx), _destroy);

		public virtual void RemoveAllItems( bool _destroy = true )
		{
			while (Content.childCount > 0)
				RemoveItem(0, _destroy);
		}

		RectTransform GetItemNextToCenter()
		{
			float minDistance = 99999999.0f;
			RectTransform result = null;

			Rect viewportRect = Viewport.GetScreenRect();
			Vector2 viewportCenter = viewportRect.center;

			bool found = false;
			foreach (RectTransform child in Content)
			{
				Rect childRect = child.GetScreenRect();
				float distance = Vector2.Distance(viewportCenter, childRect.center);
				if (distance < minDistance)
				{
					minDistance = distance;
					result = child;
					found = true;
					continue;
				}

				if (found)
					break;
			}

			return result;
		}
		#endregion

		#region Visibility

		public void EnsureVisible( int _idx, bool _centered = false, bool _forceInstant = false ) => EnsureVisible(GetContentChild(_idx), _centered, _forceInstant);

		public void EnsureVisible( RectTransform _child, bool _centered = false, bool _forceInstant = false )
		{
			if (m_ensureChildVisibilityTween != null)
			{
				TweenFactory.RemoveTween(m_ensureChildVisibilityTween, TweenStopBehavior.Complete);
				m_ensureChildVisibilityTween = null;
			}

			float padding = _centered ? 0 : m_ensureChildVisibilityPadding;
			int siblingIndex = _child.GetSiblingIndex();

			if (siblingIndex == 0 || siblingIndex == Content.childCount - 1)
				padding = 0;

			Rect viewportRect = Viewport.GetScreenRect();
			Rect childRect = _child.GetScreenRect();

			Vector3 startPos = Content.position;
			Vector3 pos = startPos;

			if (ScrollRect.horizontal)
			{
				float val = GetScrollValue(_centered, viewportRect.x, viewportRect.width, childRect.x, childRect.width);
				if (Mathf.Approximately(val, 0))
					return;

				padding *= childRect.width;
				val += padding * Mathf.Sign(val);
				pos.x += val;
			}
			else
			{
				float val = GetScrollValue(_centered, viewportRect.y, viewportRect.height, childRect.y, childRect.height);
				if (Mathf.Approximately(val, 0))
					return;

				padding *= childRect.height;
				val += padding * Mathf.Sign(val);
				pos.y -= val;
			}

			if (_forceInstant || !IsEnsureChildVisibilityAnimated)
			{
				Content.position = pos;
				return;
			}

			System.Action<ITween<Vector3>> updateBar = ( t ) =>
			{
				Content.position = t.CurrentValue;
			};

			System.Action<ITween<Vector3>> stopMoving = ( t ) =>
			{
				m_moving = false;
			};

			m_moving = true;
			m_ensureChildVisibilityTween = gameObject.Tween("makeVisible", startPos, pos, m_ensureChildVisibilityDuration, TweenScaleFunctions.QuadraticEaseInOut, updateBar, stopMoving);
		}

		#endregion

		private float GetScrollValue( bool _centered, float _xyViewport, float _whViewport, float _xyChild, float _whChild )
		{
			if (_centered)
				return _xyViewport - _xyChild + (_whViewport - _whChild) / 2;

			if (_xyChild < _xyViewport)
				return _xyViewport - _xyChild;

			if (_xyChild + _whChild > _xyViewport + _whViewport)
				return (_xyViewport + _whViewport) - (_xyChild + _whChild);

			return 0;
		}

		protected RectTransform GetContentChild( int _idx ) => (RectTransform)Content.GetChild(_idx);

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_useInitialNormalizedPosition)
				StartCoroutine(DelayedScrollToTop());
		}

		protected virtual void Update()
		{
//			Debug.Log($"velocity:{ScrollRect.velocity} mouse:{Input.GetMouseButton(0)}");

			if (!m_snappingEnabled || m_moving || Input.GetMouseButton(0))
				return;

			Vector2 vel2 = ScrollRect.velocity;
			float velocity = ScrollRect.horizontal ? vel2.x : vel2.y;

			if (Mathf.Approximately(0, velocity))
				return;

			float absVelocity = Mathf.Abs(velocity);

			if (absVelocity < m_snapBelowSpeed)
			{
				Snap();
			}
		}

		private void Snap()
		{
			ScrollRect.velocity = Vector2.zero;
			RectTransform item = GetItemNextToCenter();
			if (item)
				EnsureVisible(item, true);
		}

		protected IEnumerator DelayedScrollToTop()
		{
			yield return 0;
			ScrollRect.normalizedPosition = m_initialNormalizedPosition;
		}
	}
}