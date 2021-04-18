using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class CEvent : UnityEngine.Events.UnityEvent
	{
#if UNITY_EDITOR
		public new void Invoke()
		{
			if (!Application.isPlaying)
				return;
			base.Invoke();
		}
#endif
		public void InvokeAlways() => base.Invoke();
	}

	public class CEvent<T0> : UnityEngine.Events.UnityEvent<T0>
	{
#if UNITY_EDITOR
		public new void Invoke(T0 _arg0)
		{
			if (!Application.isPlaying)
				return;
			base.Invoke(_arg0);
		}
#endif
		public void InvokeAlways(T0 _arg0) => base.Invoke(_arg0);
	}

	public class CEvent<T0,T1> : UnityEngine.Events.UnityEvent<T0,T1>
	{
#if UNITY_EDITOR
		public new void Invoke(T0 _arg0, T1 _arg1)
		{
			if (!Application.isPlaying)
				return;
			base.Invoke(_arg0, _arg1);
		}
#endif
		public void InvokeAlways(T0 _arg0, T1 _arg1) => base.Invoke(_arg0, _arg1);
	}

	public class CEvent<T0,T1,T2> : UnityEngine.Events.UnityEvent<T0,T1,T2>
	{
#if UNITY_EDITOR
		public new void Invoke(T0 _arg0, T1 _arg1, T2 _arg2)
		{
			if (!Application.isPlaying)
				return;
			base.Invoke(_arg0, _arg1, _arg2);
		}
#endif
		public void InvokeAlways(T0 _arg0, T1 _arg1, T2 _arg2) => base.Invoke(_arg0, _arg1, _arg2);
	}

	public class CEvent<T0,T1,T2,T3> : UnityEngine.Events.UnityEvent<T0,T1,T2,T3>
	{
#if UNITY_EDITOR
		public new void Invoke(T0 _arg0, T1 _arg1, T2 _arg2, T3 _arg3)
		{
			if (!Application.isPlaying)
				return;
			base.Invoke(_arg0, _arg1, _arg2, _arg3);
		}
#endif
		public void InvokeAlways(T0 _arg0, T1 _arg1, T2 _arg2, T3 _arg3) => base.Invoke(_arg0, _arg1, _arg2, _arg3);
	}
}