using GuiToolkit.Debugging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
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
			var trace = new StackTrace(skipFrames: 1, fNeedFileInfo: false);
			var frames = trace.GetFrames();
			if (frames == null)
				return false;

			for (int i = 0; i < frames.Length; i++)
			{
				var currentMethod = frames[i].GetMethod();
				var currentType = currentMethod?.DeclaringType;

				if (currentType == null)
					continue;

				// Skip infra types (self, helpers) if provided
				if (_skipTypes != null && _skipTypes.Contains(currentType))
					continue;

				// Skip our own infra automatically
				if (currentType == typeof(EditorCallerGate))
					continue;

				// Skip compiler-generated (lambdas/async state machines)
				if (currentType.IsDefined(typeof(CompilerGeneratedAttribute), false))
					continue;
				
				// Tests are assumed to always be editor aware
				if (currentMethod.IsDefined(typeof(TestAttribute), false))
					return true;
				
				if (currentType.Name.Length > 0 && currentType.Name[0] == '<')
					continue;

				if (IsEditorAware(currentType, currentMethod))
					return true;
			}

			return false;
		}

		public static bool IsEditorAware( Type _type, MethodBase _method )
		{
			if (_type == null)
				return false;

			if (s_isAwareCache.TryGetValue(_type, out var cached))
				return cached;

			bool isAware =
                typeof(IEditorAware).IsAssignableFrom(_type) ||
                _type.IsDefined(typeof(EditorAwareAttribute), inherit: true) ||
                _method.IsDefined(typeof(EditorAwareAttribute), inherit: true);
			
			s_isAwareCache[_type] = isAware;
			return isAware;
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
