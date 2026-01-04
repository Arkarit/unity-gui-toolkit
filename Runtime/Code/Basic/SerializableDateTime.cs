using System;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public class SerializableDateTime : ISerializationCallbackReceiver, IComparable<SerializableDateTime>
	{
		[HideInInspector]
		[SerializeField] private long m_ticks = 0;

		private DateTime m_dateTime = new DateTime();

		public DateTime DateTime
		{
			get => m_dateTime;
			set
			{
				if (value.Kind == DateTimeKind.Utc)
				{
					m_dateTime = value;
				}
				else if (value.Kind == DateTimeKind.Local)
				{
					m_dateTime = value.ToUniversalTime();
				}
				else // Unspecified
				{
					m_dateTime = DateTime.SpecifyKind(value, DateTimeKind.Utc);
				}
			}
		}

		public void OnBeforeSerialize() => m_ticks = m_dateTime.Ticks;

		public void OnAfterDeserialize() => m_dateTime = new DateTime(m_ticks, DateTimeKind.Utc);

		public int CompareTo( SerializableDateTime _other )
		{
			if (_other == null)
				return 1;

#if UNITY_EDITOR
			Debug.Assert(DateTime.Kind == DateTimeKind.Utc && _other.DateTime.Kind == DateTimeKind.Utc, "Should be always UTC");
#endif
			return DateTime.CompareTo(_other.DateTime);
		}
	}
}