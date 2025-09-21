using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GuiToolkit
{
	/// <summary>
	/// Lightweight logger with "once per callsite" modes.
	/// </summary>
	public static class Logger
	{
		public enum LogMode
		{
			Verbose,
			Default,
			Warning,
			Error,

			VerboseOnce,
			DefaultOnce,
			WarningOnce,
			ErrorOnce,
		}

		private static readonly HashSet<string> s_onceKeys = new HashSet<string>();
		private static readonly object s_lock = new object();

		private static bool IsOnce( LogMode _logMode ) => _logMode > LogMode.Error;

		/// <summary>
		/// Clears the internal "once" cache. Called on domain reload, playmode changes, and project changes.
		/// </summary>
		public static void Clear()
		{
			lock (s_lock)
			{
				s_onceKeys.Clear();
			}
		}

		public static void LogVerbose( string _s, Object _context = null ) => Log(_s, _context, LogMode.Verbose);
		public static void Log( string _s, Object _context = null ) => Log(_s, _context, LogMode.Default);
		public static void LogWarning( string _s, Object _context = null ) => Log(_s, _context, LogMode.Warning);
		public static void LogError( string _s, Object _context = null ) => Log(_s, _context, LogMode.Error);
		public static void LogVerboseOnce( string _s, Object _context = null ) => Log(_s, _context, LogMode.VerboseOnce);
		public static void LogOnce( string _s, Object _context = null ) => Log(_s, _context, LogMode.DefaultOnce);
		public static void LogWarningOnce( string _s, Object _context = null ) => Log(_s, _context, LogMode.WarningOnce);
		public static void LogErrorOnce( string _s, Object _context = null ) => Log(_s, _context, LogMode.ErrorOnce);

		/// <summary>
		/// Core logger. For "Once" modes, the message is emitted only once per callsite (file:line) per editor/runtime session.
		/// Caution: "Once" modes have a high performance impact, since they have to extract an additional stack trace.
		/// Use sparingly and handle with care.
		/// </summary>
		public static void Log( string _s, Object _context, LogMode _logMode )
		{
			// Release player: only warnings and errors
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
			if (_logMode != LogMode.Warning &&
			    _logMode != LogMode.WarningOnce &&
			    _logMode != LogMode.Error &&
			    _logMode != LogMode.ErrorOnce)
				return;
#endif

			string ts = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string msg = "[" + ts + "] " + _s;

			if (IsOnce(_logMode))
			{
				string key = GetCallsiteKey();
				lock (s_lock)
				{
					if (!s_onceKeys.Add(key))
						return;
				}
			}

			switch (_logMode)
			{
				case LogMode.Verbose:
				case LogMode.VerboseOnce:
					if (_context)
						Debug.Log("[VERBOSE] " + msg, _context);
					else
						Debug.Log("[VERBOSE] " + msg);
					break;

				case LogMode.Default:
				case LogMode.DefaultOnce:
					if (_context)
						Debug.Log(msg, _context);
					else
						Debug.Log(msg);
					break;

				case LogMode.Warning:
				case LogMode.WarningOnce:
					if (_context)
						Debug.LogWarning(msg, _context);
					else
						Debug.LogWarning(msg);
					break;

				case LogMode.Error:
				case LogMode.ErrorOnce:
					if (_context)
						Debug.LogError(msg, _context);
					else
						Debug.LogError(msg);
					break;
			}
		}

		/// <summary>
		/// Builds a stable callsite key by scanning the stack trace for the first frame outside Logger.
		/// Prefers file:line if available; otherwise falls back to method signature and IL offset.
		/// </summary>
		private static string GetCallsiteKey()
		{
			var st = new StackTrace(true);

			for (int i = 0; i < st.FrameCount; i++)
			{
				var f = st.GetFrame(i);
				var m = f.GetMethod();
				if (m == null)
					continue;

				var declaringType = m.DeclaringType;
				if (declaringType == typeof(Logger))
					continue;

				string file = f.GetFileName();
				int line = f.GetFileLineNumber();

				if (!string.IsNullOrEmpty(file) && line > 0)
					return file + ":" + line;

				// Fallback when PDBs or file info are not available (e.g., some player builds)
				string typeName = declaringType != null ? declaringType.FullName : "<UnknownType>";
				string methodName = m is MethodInfo mi ? mi.Name : m.Name;
				int il = f.GetILOffset();
				return typeName + "." + methodName + "#" + il;
			}

			return "<UnknownCaller>";
		}
	}

#if UNITY_EDITOR
	[InitializeOnLoad]
	static class LoggerReset
	{
		static LoggerReset()
		{
			AssemblyReloadEvents.beforeAssemblyReload += Clear;
			EditorApplication.playModeStateChanged += _ => Clear();
			EditorApplication.projectChanged += Clear;
		}

		private static void Clear()
		{
			Logger.Clear();
		}
	}
#endif
}
