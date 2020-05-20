using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace GuiToolkit
{
	public static class Log
	{
		[Conditional("UNITY_EDITOR")]
		public static void Editor(string _s)
		{
			UnityEngine.Debug.Log(_s);
		}

		[Conditional("DEBUG")]
		public static void Debug(string _s)
		{
			UnityEngine.Debug.Log(_s);
		}

		[Conditional("DEBUG_LAYOUT")]
		public static void Layout(string _s)
		{
			UnityEngine.Debug.Log(_s);
		}
	}
}