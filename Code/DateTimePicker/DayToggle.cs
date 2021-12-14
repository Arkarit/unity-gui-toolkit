/// <summary>
/// Originally taken from "UnityCalender"
/// https://github.com/n-sundara-pandian/UnityCalender
/// MIT License
/// </summary>
/// 
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(EventTrigger))]
	public class DayToggle : Toggle
	{
		public class OnDateTimeSelectedEvent : UnityEvent<DateTime?> { }
		public OnDateTimeSelectedEvent m_evtOnDateSelected = new OnDateTimeSelectedEvent();

		private bool m_clicked = false;
		private DateTime? m_dateTime;

		public DateTime? dateTime
		{
			get
			{
				return m_dateTime;
			}
			set
			{
				m_dateTime = value;
				var todayMarker = GetTodayMarker();
				if (todayMarker != null)
				{
					GetTodayMarker().gameObject.SetActive(m_dateTime != null && DateTime.Today.IsSameDate((DateTime)m_dateTime));
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
			entry.callback.AddListener(( data ) => { OnPointerUpDelegate((PointerEventData)data); });
			trigger.triggers.Add(entry);
		}

		// User trigger because ValueChanged is triggered when toggle is set inactive
		private void OnPointerUpDelegate( PointerEventData data )
		{
			if (isActiveAndEnabled && IsInteractable())
			{
				m_clicked = true;
				m_evtOnDateSelected.Invoke(dateTime);
			}
		}

		public void onToggleValueChanged( bool value )
		{
			AllowSwitchOffHack(value);
		}
		/// <summary>
		/// A hack to replace AllowSwitchOff of Toggle Group since it doesnt work well when the GameObject it is set inactive
		/// </summary>
		/// <param name="value"></param>
		void AllowSwitchOffHack( bool value )
		{
			if (m_clicked && !value)
			{
				isOn = true;
			}
			m_clicked = false;
		}


		public void SetText( string text )
		{
			GetComponentInChildren<TMP_Text>().text = text;
		}

		public void ClearText()
		{
			SetText(string.Empty);
		}
	}
}