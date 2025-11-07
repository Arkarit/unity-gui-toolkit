using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	/// \brief Base router for disambiguating drag between two UI targets (A/B) based on drag orientation.
	/// 
	/// Determines horizontal vs. vertical drag and routes all pointer/drag events
	/// to the "primary" or "secondary" target accordingly.
	public abstract class UiAbstractDragRouter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
	{
		protected const float DRAG_DETECTION_TIME = 0.2f;

		private PointerEventData m_eventData;
		private ETriState m_isHorizontal;
		private IBeginDragHandler m_beginDragHandler;
		private IDragHandler m_dragHandler;
		private IEndDragHandler m_endDragHandler;
		private IPointerDownHandler m_pointerDownHandler;
		private bool m_handlersDetermined;
		private bool m_wasDragged;

		/// \brief Resolve required references. Return false if unavailable.
		protected abstract bool TryResolveDependencies();

		/// \brief The primary UI target (used when drag orientation matches primary orientation).
		protected abstract MonoBehaviour GetPrimaryTarget();

		/// \brief The secondary UI target (used when drag orientation does not match primary orientation).
		protected abstract MonoBehaviour GetSecondaryTarget();

		/// \brief True if the primary target is considered horizontal, else vertical.
		protected abstract bool IsPrimaryHorizontal();

		/// \brief Dispatches pointer down to the primary control (e.g., Slider.OnPointerDown).
		protected abstract void OnPrimaryPointerDown(PointerEventData _eventData);
		/// \brief Dispatches pointer down to the primary control (e.g., Slider.OnPointerDown).
		protected abstract void OnPrimaryPointerUp(PointerEventData _eventData);

		public virtual void Start()
		{
			if (!TryResolveDependencies())
			{
				Destroy(gameObject);
				return;
			}
		}

		public void OnBeginDrag(PointerEventData _eventData)
		{
			m_eventData = _eventData.ShallowClone();
			m_handlersDetermined = false;
			m_wasDragged = true;
		}

		public void OnDrag(PointerEventData _eventData)
		{
			if (m_eventData != null)
			{
				m_isHorizontal = EvalHorizontal(m_eventData, _eventData);
			}

			if (m_isHorizontal == ETriState.Indeterminate)
				return;

			if (!m_handlersDetermined)
			{
				m_handlersDetermined = true;

				bool isDragHorizontal = m_isHorizontal == ETriState.True;
				bool usePrimary = isDragHorizontal == IsPrimaryHorizontal();

				MonoBehaviour target = usePrimary ? GetPrimaryTarget() : GetSecondaryTarget();
				SetHandlers(target);
			}

			if (m_beginDragHandler != null)
			{
				m_beginDragHandler.OnBeginDrag(m_eventData);
				m_beginDragHandler = null;
			}

			m_eventData = null;

			if (m_dragHandler != null)
			{
				m_dragHandler.OnDrag(_eventData);
			}
		}

		public void OnEndDrag(PointerEventData _eventData)
		{
			if (m_endDragHandler != null)
			{
				m_endDragHandler.OnEndDrag(_eventData);
			}
		}

		public void OnPointerDown(PointerEventData _eventData)
		{
			StartCoroutine(DelayedOnPointerDown(_eventData.ShallowClone()));
		}


		public void OnPointerUp(PointerEventData _eventData)
		{
			OnPrimaryPointerUp(_eventData);
		}
		
		private void SetHandlers(MonoBehaviour _monoBehaviour)
		{
			m_beginDragHandler = _monoBehaviour is IBeginDragHandler ? (IBeginDragHandler)_monoBehaviour : null;
			m_dragHandler = _monoBehaviour is IDragHandler ? (IDragHandler)_monoBehaviour : null;
			m_endDragHandler = _monoBehaviour is IEndDragHandler ? (IEndDragHandler)_monoBehaviour : null;
			m_pointerDownHandler = _monoBehaviour is IPointerDownHandler ? (IPointerDownHandler)_monoBehaviour : null;
		}

		protected static ETriState EvalHorizontal(PointerEventData _before, PointerEventData _after)
		{
			Vector2 delta = _after.position - _before.position;
			Vector2 deltaAbs = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));

			if (deltaAbs.magnitude < 1f)
				return ETriState.Indeterminate;

			return deltaAbs.x > deltaAbs.y ? ETriState.True : ETriState.False;
		}

		private IEnumerator DelayedOnPointerDown(PointerEventData _eventData)
		{
			yield return new WaitForSecondsRealtime(DRAG_DETECTION_TIME);

			if (!m_wasDragged)
			{
				OnPrimaryPointerDown(_eventData);
			}

			m_wasDragged = false;
		}
	}
}
