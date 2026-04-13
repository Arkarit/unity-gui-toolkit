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

			string offender = FindFirstOffendingCaller(_skipTypes);
			string offenderHint = string.IsNullOrEmpty(offender)
				? string.Empty
				: $"\nOffending caller: {offender}";

			throw new InvalidOperationException(
				$"{DebugUtility.GetCallingClassAndMethod(false, true, 1)} needs to be called with\n" +
				$"at least one caller in the stack trace to implement IEditorAware (and of course implement Editor awareness){offenderHint}");
		}

		/// <summary>
		/// Walks the call stack from outermost to innermost and returns the fully-qualified name
		/// of the first method whose declaring type is neither Unity infrastructure nor IEditorAware.
		/// Walking outer-to-inner finds the real instigator (the high-level code that started the
		/// chain) rather than the infrastructure getter that triggered the check.
		/// </summary>
		private static string FindFirstOffendingCaller( Type[] _skipTypes )
		{
			var frames = new StackTrace(1, false).GetFrames();
			if (frames == null)
				return null;

			for (int i = frames.Length - 1; i >= 0; i--)
			{
				var f = frames[i];
				var m = f.GetMethod();
				var t = m?.DeclaringType;
				if (t == null)
					continue;
				if (t == typeof(EditorCallerGate))
					continue;
				if (_skipTypes != null && _skipTypes.Contains(t))
					continue;
				if (IsKnownInfrastructure(t))
					continue;

				if (!IsOrHasOuterEditorAware(t))
					return $"{t.FullName}.{m.Name}";
			}

			return null;
		}

		private static bool IsKnownInfrastructure( Type _type )
		{
			// Walk outer classes as well (handles compiler-generated nested types)
			for (var cur = _type; cur != null; cur = cur.DeclaringType)
			{
				var ns = cur.Namespace ?? string.Empty;
				if (ns.StartsWith("UnityEngine", StringComparison.Ordinal)
					|| ns.StartsWith("UnityEditor", StringComparison.Ordinal))
					return true;
			}
			return false;
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
