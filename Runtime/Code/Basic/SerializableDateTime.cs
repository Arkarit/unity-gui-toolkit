using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public class SerializableDateTime : ISerializationCallbackReceiver, IComparable<SerializableDateTime>
	{
		[HideInInspector]
		[SerializeField] private long m_ticks = 0;

		private DateTime m_dateTime = new DateTime();

		public SerializableDateTime() { }
		public SerializableDateTime(DateTime _time) => m_dateTime = _time;

		public DateTime DateTime
		{
			get => m_dateTime;
			set => m_dateTime = value;
		}

		public void OnBeforeSerialize() => m_ticks = m_dateTime.Ticks;

		public void OnAfterDeserialize() => m_dateTime = new DateTime(m_ticks);

		public int CompareTo(SerializableDateTime _other) => DateTime.CompareTo(_other.DateTime);
	}
}