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
		/// Number of innermost frames probed one at a time before falling back to capturing the whole
		/// stack. Small on purpose: the point is to decide the common case without a full capture.
		/// </summary>
		/// Kept short deliberately: when no near frame decides it, the cost is the probe PLUS the full
		/// capture, so a long probe would tax the undecided case (an aware caller far up the stack, or the
		/// error path that is about to throw anyway) to buy nothing.
		private const int NearFrameProbeCount = 4;

		/// <summary>
		/// Returns true if ANY caller type on the stack implements IEditorAware.
		/// skipTypes: optional infra types to skip early (e.g., your getters/helpers).
		///
		/// Capturing the whole stack is what costs here - measured at ~66 us against ~3 us for a single
		/// frame, and the walk itself is free by comparison. Since the answer is almost always decided by
		/// the innermost frames (an editor-aware type asking for a singleton, or the gate's own caller),
		/// the near frames are probed one at a time and the full capture only happens when none of them
		/// decides it. That is the failure path and the rare deep-infrastructure call, not the hot one.
		/// This getter sits in front of every singleton in the toolkit, so it was the single most
		/// expensive thing about resolving a style: three of these per resolution.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static bool IsAnyCallerEditorAware( params Type[] _skipTypes )
		{
			// Frame 1 is this method's caller, matching what the full capture below sees first.
			for (int i = 1; i <= NearFrameProbeCount; i++)
			{
				if (IsAwareFrame(new StackFrame(i, false).GetMethod(), _skipTypes))
					return true;
			}

			var frames = new StackTrace(1, false).GetFrames();
			if (frames == null) return false;

			foreach (var f in frames)
			{
				if (IsAwareFrame(f.GetMethod(), _skipTypes))
					return true;
			}
			
			return false;
		}

		/// <summary>
		/// The per-frame decision, shared by the near-frame probe and the full walk so the two can not
		/// drift apart. A frame that says nothing (unknown, the gate itself, an explicitly skipped type,
		/// or simply a type that is not aware) returns false and lets the caller keep looking.
		/// </summary>
		private static bool IsAwareFrame( System.Reflection.MethodBase _method, Type[] _skipTypes )
		{
			var t = _method?.DeclaringType;
			if (t == null)
				return false;

			if (_skipTypes != null && _skipTypes.Contains(t))
				return false;

			if (t == typeof(EditorCallerGate))
				return false;

			if (IsOrHasOuterEditorAware(t))
				return true;

			return _method.IsDefined(typeof(EditorAwareAttribute), inherit: true);
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
		/// Walks the call stack from innermost to outermost and returns the fully-qualified name
		/// of the first method whose declaring type is neither Unity infrastructure nor IEditorAware.
		/// Walking inner-to-outer surfaces the immediate infrastructure entry point (e.g. the
		/// singleton getter) whose generic type argument clearly names the offending singleton.
		/// </summary>
		private static string FindFirstOffendingCaller( Type[] _skipTypes )
		{
			var frames = new StackTrace(1, false).GetFrames();
			if (frames == null)
				return null;

			for (int i = 0; i < frames.Length; i++)
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
