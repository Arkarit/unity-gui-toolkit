using System;
using UnityEngine;
using UnityEngine.Events;


/// \file EventOverrides.cs
/// \brief Event definition overrides
/// 
/// UnityEvent overrides with separate handling of isPlaying and editor code.

namespace GuiToolkit
{

	/// \brief Event definition override
	/// 
	/// This event definition supports code, which runs in editor.<BR>
	/// If the code is running normally (Application.isPlaying == true), an event is invoked.<BR>
	/// If the code is running in editor ([RunAlways]), the event is suppressed.<BR>
	/// An invocation can be enforced by calling InvokeAlways() even from editor code.
	///
	/// \note Although we usually don't use prefixes for classes, we do it here to not collide with UnityEngine.Event
	[Serializable]
	public class CEvent : UnityEvent
	{
		private readonly bool m_autoInvoke;
		public CEvent(bool _autoInvoke) : base()
		{
			m_autoInvoke = _autoInvoke;
		}

		public CEvent() : this(false) {}

#if UNITY_EDITOR
		public new void Invoke()
		{
			if (!Application.isPlaying)
				return;
			base.Invoke();
		}
#endif
		public void InvokeAlways() => base.Invoke();
		public new void AddListener(UnityAction _call)
		{
			base.AddListener(_call);
			if (m_autoInvoke)
				_call();
		}
	}

	[Serializable]
	public class CEvent<T0> : UnityEvent<T0>
	{
		private bool m_canAutoInvoke;
		private T0 m_lastT0;

		public CEvent( bool _canAutoInvoke,T0 autoInvokeStartValue = default) : base()
		{
			m_canAutoInvoke = _canAutoInvoke;
			m_lastT0 = autoInvokeStartValue;
		}
		public CEvent() : this(false) {}

		public new void Invoke(T0 _arg0)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			if(m_canAutoInvoke)
				m_lastT0 = _arg0;

			base.Invoke(_arg0);
		}

		public void InvokeAlways(T0 _arg0)
		{
			if(m_canAutoInvoke)
				m_lastT0 = _arg0;

			base.Invoke(_arg0);
		}

		public void AddListener(UnityAction<T0> _call, bool autoInvoke)
		{
			base.AddListener(_call);
			if (autoInvoke)
			{
				if(!m_canAutoInvoke)
					Debug.LogError($"Event {GetType().Name} cannot auto invoke");
				
				_call(m_lastT0);
			}
		}
	}

	[Serializable]
	public class CEvent<T0,T1> : UnityEvent<T0,T1>
	{
		private readonly bool m_canAutoInvoke;
		private T0 m_lastT0;
		private T1 m_lastT1;

		public CEvent(
			bool _canAutoInvoke, 
			T0 autoInvokeStartValue0 = default, 
			T1 autoInvokeStartValue1 = default
			) : base()
		{
			m_canAutoInvoke = _canAutoInvoke;
			m_lastT0 = autoInvokeStartValue0;
			m_lastT1 = autoInvokeStartValue1;
		}

		public CEvent() : this(false) {}

		public new void Invoke(T0 _arg0, T1 _arg1)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			if (m_canAutoInvoke)
			{
				m_lastT0 = _arg0;
				m_lastT1 = _arg1;
			}

			base.Invoke(_arg0, _arg1);
		}

		public void InvokeAlways(T0 _arg0, T1 _arg1)
		{
			if (m_canAutoInvoke)
			{
				m_lastT0 = _arg0;
				m_lastT1 = _arg1;
			}

			base.Invoke(_arg0, _arg1);
		}

		public void AddListener(UnityAction<T0,T1> _call, bool _canAutoInvoke = false)
		{
			base.AddListener(_call);
			if (_canAutoInvoke)
			{
				if(!m_canAutoInvoke)
					Debug.LogError($"Event {GetType().Name} cannot auto invoke");
				
				_call(m_lastT0, m_lastT1);
			}
		}
	}

	[Serializable]
	public class CEvent<T0,T1,T2> : UnityEvent<T0,T1,T2>
	{
		private readonly bool m_canAutoInvoke;
		private T0 m_lastT0;
		private T1 m_lastT1;
		private T2 m_lastT2;

		public CEvent(
			bool _canAutoInvoke, 
			T0 autoInvokeStartValue0 = default, 
			T1 autoInvokeStartValue1 = default, 
			T2 autoInvokeStartValue2 = default
			) : base()
		{
			m_canAutoInvoke = _canAutoInvoke;
			m_lastT0 = autoInvokeStartValue0;
			m_lastT1 = autoInvokeStartValue1;
			m_lastT2 = autoInvokeStartValue2;
		}

		public CEvent() : this(false) {}

		public new void Invoke(T0 _arg0, T1 _arg1, T2 _arg2)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			if (m_canAutoInvoke)
			{
				m_lastT0 = _arg0;
				m_lastT1 = _arg1;
				m_lastT2 = _arg2;
			}

			base.Invoke(_arg0, _arg1, _arg2);
		}

		public void InvokeAlways(T0 _arg0, T1 _arg1, T2 _arg2)
		{
			if (m_canAutoInvoke)
			{
				m_lastT0 = _arg0;
				m_lastT1 = _arg1;
				m_lastT2 = _arg2;
			}

			base.Invoke(_arg0, _arg1, _arg2);
		}

		public new void AddListener(UnityAction<T0,T1,T2> _call, bool _canAutoInvoke = false)
		{
			base.AddListener(_call);
			if (_canAutoInvoke)
			{
				if(!m_canAutoInvoke)
					Debug.LogError($"Event {GetType().Name} cannot auto invoke");
				
				_call(m_lastT0, m_lastT1, m_lastT2);
			}
		}
	}

	[Serializable]
	public class CEvent<T0,T1,T2,T3> : UnityEvent<T0,T1,T2,T3>
	{
		private readonly bool m_canAutoInvoke;
		private T0 m_lastT0;
		private T1 m_lastT1;
		private T2 m_lastT2;
		private T3 m_lastT3;

		public CEvent(
			bool _canAutoInvoke, 
			T0 autoInvokeStartValue0 = default, 
			T1 autoInvokeStartValue1 = default, 
			T2 autoInvokeStartValue2 = default,
			T3 autoInvokeStartValue3 = default
			) : base()
		{
			m_canAutoInvoke = _canAutoInvoke;
			m_lastT0 = autoInvokeStartValue0;
			m_lastT1 = autoInvokeStartValue1;
			m_lastT2 = autoInvokeStartValue2;
			m_lastT3 = autoInvokeStartValue3;
		}

		public CEvent() : this(false) {}

		public new void Invoke(T0 _arg0, T1 _arg1, T2 _arg2, T3 _arg3)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				return;
#endif
			if (m_canAutoInvoke)
			{
				m_lastT0 = _arg0;
				m_lastT1 = _arg1;
				m_lastT2 = _arg2;
				m_lastT3 = _arg3;
			}

			base.Invoke(_arg0, _arg1, _arg2, _arg3);
		}

		public void InvokeAlways(T0 _arg0, T1 _arg1, T2 _arg2, T3 _arg3)
		{
			if (m_canAutoInvoke)
			{
				m_lastT0 = _arg0;
				m_lastT1 = _arg1;
				m_lastT2 = _arg2;
				m_lastT3 = _arg3;
			}

			base.Invoke(_arg0, _arg1, _arg2, _arg3);
		}

		public new void AddListener(UnityAction<T0,T1,T2, T3> _call, bool _canAutoInvoke = false)
		{
			base.AddListener(_call);
			if (_canAutoInvoke)
			{
				if(!m_canAutoInvoke)
					Debug.LogError($"Event {GetType().Name} cannot auto invoke");
				
				_call(m_lastT0, m_lastT1, m_lastT2, m_lastT3);
			}
		}
	}
}
