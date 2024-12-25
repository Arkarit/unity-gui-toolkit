using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using RSToolkit.Helpers;

namespace GuiToolkit
{
	[RequireComponent(typeof(EventTrigger))]
	public class DayToggle : Toggle
	{
		public class OnDateTimeSelectedEvent : UnityEvent<DateTime?>
		{
		}

		public OnDateTimeSelectedEvent onDateSelected = new OnDateTimeSelectedEvent();

		private DateTime? m_dateTime;
		private bool m_clicked;


		public DateTime? DateTime
		{
			get { return m_dateTime; }
			set
			{
				m_dateTime = value;
				var todayMarker = GetTodayMarker();
				if (todayMarker != null)
				{
					GetTodayMarker().gameObject
						.SetActive(m_dateTime != null && System.DateTime.Today.IsSameDate((DateTime)m_dateTime));
				}

			}
		}

		// Cannot make serializable field when inheriting toggle
		private Image GetTodayMarker()
		{
			Component[] transforms = GetComponentsInChildren(typeof(Transform), true);

			foreach (Transform transform in transforms)
			{
				if (transform.gameObject.name == "Today Marker")
				{
					return transform.gameObject.GetComponent<Image>();
				}
			}

			return null;
		}

		protected override void Start()
		{
			base.Start();

			onValueChanged.AddListener(onToggleValueChanged);
			EventTrigger trigger = GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerUp;
			entry.callback.AddListener((data) => { OnPointerUpDelegate((PointerEventData)data); });
			trigger.triggers.Add(entry);
		}

		// User trigger because ValueChanged is triggered when toggle is set inactive
		private void OnPointerUpDelegate(PointerEventData _)
		{
			if (isActiveAndEnabled && IsInteractable())
			{
				m_clicked = true;
				onDateSelected.Invoke(DateTime);
			}
		}

		public void onToggleValueChanged(bool _value)
		{
			AllowSwitchOffHack(_value);
		}

		/// <summary>
		/// A hack to replace AllowSwitchOff of Toggle Group since it doesnt work well when the GameObject it is set inactive
		/// </summary>
		/// <param name="_value"></param>
		void AllowSwitchOffHack(bool _value)
		{
			if (m_clicked && !_value)
			{
				isOn = true;
			}

			m_clicked = false;
		}


		public void SetText(string _text)
		{
			GetComponentInChildren<Text>().text = _text;
		}

		public void ClearText()
		{
			SetText(string.Empty);
		}
	}
}