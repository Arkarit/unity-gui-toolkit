using GuiToolkit.Debugging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace GuiToolkit
{
	public static class EditorCallerGate
	{
#if UNITY_EDITOR
		private static readonly Dictionary<Type, bool> s_isAwareCache = new();

		/// <summary>
		/// Returns true if ANY caller type on the stack implements IEditorAware.
		/// skipTypes: optional infra types to skip early (e.g., your getters/helpers).
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsAnyCallerEditorAware( params Type[] _skipTypes )
		{
			var frames = new StackTrace(1, false).GetFrames();
			if (frames == null) return false;

			foreach (var f in frames)
			{
				var m = f.GetMethod();
				var t = m?.DeclaringType;
				if (t == null) 
					continue;

				if (_skipTypes != null && _skipTypes.Contains(t)) 
					continue;
				
				if (t == typeof(EditorCallerGate)) 
					continue;

				if (IsOrHasOuterEditorAware(t)) 
					return true;

				if (m.IsDefined(typeof(EditorAwareAttribute), inherit: true)) 
					return true;
			}
			
			return false;
		}

		private static bool IsOrHasOuterEditorAware( Type _type )
		{
			for (var cur = _type; cur != null; cur = cur.DeclaringType)
			{
				if (!s_isAwareCache.TryGetValue(cur, out bool aware))
				{
					aware = typeof(IEditorAware).IsAssignableFrom(cur)
						 || cur.IsDefined(typeof(EditorAwareAttribute), inherit: true);
					s_isAwareCache[cur] = aware;
				}
				if (aware) 
					return true;
			}
			return false;
		}

		/// <summary>
		/// Clears the internal cache of editor-aware caller types.
		/// Call this when the domain is reloaded or Play/Edit mode changes.
		/// </summary>
		public static void Clear() => s_isAwareCache.Clear();

		public static void ThrowIfNotEditorAware( string _name, params Type[] _skipTypes )
		{
			if (Application.isPlaying || IsAnyCallerEditorAware(_skipTypes))
				return;
			
			throw new InvalidOperationException($"{DebugUtility.GetCallingClassAndMethod(false, true, 1)} needs to be called with\n" +
												"at least one caller in the stack trace to implement IEditorAware (and of course implement Editor awareness)");
		}

#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAnyCallerEditorAware( params Type[] _skipTypes ) => true;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsEditorAware( Type _callerType ) => true;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear() {}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotEditorAware( string _name, params Type[] _skipTypes ) {}
#endif
	}

#if UNITY_EDITOR
	[InitializeOnLoad]
	static class EditorCallerGateReset
	{
		static EditorCallerGateReset()
		{
			AssemblyReloadEvents.beforeAssemblyReload += Clear;
			EditorApplication.playModeStateChanged += _ => Clear();
		}

		private static void Clear()
		{
			EditorCallerGate.Clear();
		}
	}
#endif

}
