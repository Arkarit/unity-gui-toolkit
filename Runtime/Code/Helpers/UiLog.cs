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
	/// Named <c>UiLog</c> instead of <c>Logger</c> to avoid
	/// collisions with Unity's own logging API and common third-party loggers.
	/// The "Ui" prefix ties it to the GuiToolkit namespace, even though
	/// it can be used for non-UI logging as well.
	///
	/// Note:
	/// - <c>LogOnce</c> variants are more expensive than regular logging.
	///   They rely on caller information and a global cache lookup to ensure
	///   that a message is only emitted once per file/line. This adds overhead
	///   compared to standard <c>Debug.Log</c> calls and should be used sparingly
	///   in performance-critical runtime code.
	/// - In non-development player builds, only warnings and errors are logged.
	///   Verbose and default messages are stripped at compile time.
	/// </summary>
	public static class UiLog
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

		public static void LogVerbose( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.Verbose, _prefix);
		public static void LogVerbose( string _s, string _prefix = null ) => Log(_s, null, LogMode.Verbose, _prefix);
		public static void Log( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.Default, _prefix);
		public static void Log( string _s, string _prefix = null ) => Log(_s, null, LogMode.Default, _prefix);
		public static void LogWarning( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.Warning, _prefix);
		public static void LogWarning( string _s, string _prefix = null ) => Log(_s, null, LogMode.Warning, _prefix);
		public static void LogError( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.Error, _prefix);
		public static void LogError( string _s, string _prefix = null ) => Log(_s, null, LogMode.Error, _prefix);
		public static bool LogVerboseOnce( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.VerboseOnce, _prefix);
		public static bool LogVerboseOnce( string _s, string _prefix = null ) => Log(_s, null, LogMode.VerboseOnce, _prefix);
		public static bool LogOnce( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.DefaultOnce, _prefix);
		public static bool LogOnce( string _s, string _prefix = null ) => Log(_s, null, LogMode.DefaultOnce, _prefix);
		public static bool LogWarningOnce( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.WarningOnce, _prefix);
		public static bool LogWarningOnce( string _s, string _prefix = null ) => Log(_s, null, LogMode.WarningOnce, _prefix);
		public static bool LogErrorOnce( string _s, Object _context, string _prefix = null ) => Log(_s, _context, LogMode.ErrorOnce, _prefix);
		public static bool LogErrorOnce( string _s, string _prefix = null ) => Log(_s, null, LogMode.ErrorOnce, _prefix);
		
		internal static void LogInternal(string _s, Object _context, string _prefix = null )
		{
			var prefix = "::GuiToolkit::";
			if (!string.IsNullOrEmpty(_prefix))
				prefix = $"{prefix}{_prefix}::";
			
			Log(_s, _context, LogMode.Default, prefix);
		}
		
		internal static void LogInternal(string _s, string _prefix = null ) => LogInternal(_s, null, _prefix);

		/// <summary>
		/// Core logger. For "Once" modes, the message is emitted only once per callsite (file:line) per editor/runtime session.
		/// Caution: "Once" modes have a high performance impact, since they have to extract an additional stack trace.
		/// Use sparingly and handle with care.
		/// Returns true if the text was actually logged (important for "Once" modes, where you can detect the first occurrence of the logging)
		/// </summary>
		public static bool Log( string _s, Object _context, LogMode _logMode, string _prefix = null )
		{
			// Release player: only warnings and errors
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
			if (_logMode != LogMode.Warning &&
			    _logMode != LogMode.WarningOnce &&
			    _logMode != LogMode.Error &&
			    _logMode != LogMode.ErrorOnce)
				return false;
#endif

			string ts = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			string msg = "[" + ts + "] " + _s;
			if (!string.IsNullOrEmpty(_prefix))
				msg = _prefix + msg;

			if (IsOnce(_logMode))
			{
				string key = GetCallsiteKey();
				lock (s_lock)
				{
					if (!s_onceKeys.Add(key))
						return false;
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

			return true;
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
				if (declaringType == typeof(UiLog))
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
			UiLog.Clear();
		}
	}
#endif
}
