using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class CaptureComponentUtility
	{
		// Helper: capture, destroy, later restore "blocker" components
		public struct BlockerSnapshot
		{
			public Type Type;
			public Component Template;
			public GameObject TemplateOwner;
		}

		/// <summary>
		/// Captures and removes components that would prevent removing/replacing a component due to [RequireComponent].
		/// This handles *all* RequireComponent dependencies and resolves chains iteratively:
		/// If B requires C and C requires D, and D would be missing, then C becomes a blocker,
		/// and therefore B becomes a blocker as well.
		/// </summary>
		/// <param name="_go">Target GameObject.</param>
		/// <param name="_savedMonoBehaviour">The component we are about to remove/replace (must not be removed here).</param>
		/// <param name="_replacementType">
		/// Optional: the component type that will exist after replacement (TB). If null, assume no replacement.
		/// </param>
		public static List<BlockerSnapshot> CaptureAndRemoveBlockers( GameObject _go, MonoBehaviour _savedMonoBehaviour, Type _replacementType = null )
		{
			var blockers = new List<BlockerSnapshot>();

			if (_go == null)
				return blockers;

			var presentComponents = _go.GetComponents<Component>();
			if (presentComponents == null || presentComponents.Length == 0)
				return blockers;

			var removable = _go.GetComponents<MonoBehaviour>();
			if (removable == null || removable.Length == 0)
				return blockers;

			// Build initial "present types" multiset after the planned removal/replacement.
			// Presence includes ALL Components (including non-MonoBehaviours) so RequireComponent checks are correct.
			// Removal applies only to MonoBehaviours.
			var presentTypeCounts = new Dictionary<Type, int>();

			for (int i = 0; i < presentComponents.Length; i++)
			{
				var c = presentComponents[i];
				if (!c)
					continue;

				// The component we are about to remove should be considered "not present"
				if (c == _savedMonoBehaviour)
					continue;

				Increment(presentTypeCounts, c.GetType());
			}

			if (_replacementType != null)
				Increment(presentTypeCounts, _replacementType);

			var toRemove = new List<MonoBehaviour>();

			bool changed = true;
			while (changed)
			{
				changed = false;
				toRemove.Clear();

				for (int i = 0; i < removable.Length; i++)
				{
					var mb = removable[i];
					if (!mb)
						continue;

					if (mb == _savedMonoBehaviour)
						continue;

					var mt = mb.GetType();

					// If this instance type is no longer modeled as present, it has been removed already.
					if (!IsPresentType(presentTypeCounts, mt))
						continue;

					if (!AreRequirementsSatisfied(mt, presentTypeCounts))
						toRemove.Add(mb);
				}

				if (toRemove.Count == 0)
					break;

				for (int i = 0; i < toRemove.Count; i++)
				{
					var b = toRemove[i];
					if (!b)
						continue;

					var bt = b.GetType();
					blockers.Add(CaptureBlocker(b));

					Decrement(presentTypeCounts, bt);

					Undo.DestroyObjectImmediate(b);
					changed = true;
				}

				// Refresh arrays - Unity can reorder / null out destroyed entries.
				presentComponents = _go.GetComponents<Component>();
				removable = _go.GetComponents<MonoBehaviour>();

				// Rebuild presence model from scratch to stay correct (cheap per GO, avoids edge cases)
				presentTypeCounts.Clear();

				for (int i = 0; i < presentComponents.Length; i++)
				{
					var c = presentComponents[i];
					if (!c)
						continue;

					if (c == _savedMonoBehaviour)
						continue;

					Increment(presentTypeCounts, c.GetType());
				}

				if (_replacementType != null)
					Increment(presentTypeCounts, _replacementType);
			}

			return blockers;

			static void Increment( Dictionary<Type, int> _dict, Type _t )
			{
				if (_t == null)
					return;

				if (_dict.TryGetValue(_t, out int n))
					_dict[_t] = n + 1;
				else
					_dict[_t] = 1;
			}

			static void Decrement( Dictionary<Type, int> _dict, Type _t )
			{
				if (_t == null)
					return;

				if (!_dict.TryGetValue(_t, out int n))
					return;

				n--;
				if (n <= 0)
					_dict.Remove(_t);
				else
					_dict[_t] = n;
			}

			static bool IsPresentType( Dictionary<Type, int> _dict, Type _t )
			{
				if (_t == null)
					return false;

				return _dict.TryGetValue(_t, out int n) && n > 0;
			}
		}

		public static void RestoreBlockers( GameObject _go, List<BlockerSnapshot> _blockers )
		{
			if (_blockers == null) return;

			for (int i = _blockers.Count - 1; i >= 0; i--)
			{
				var blocker = _blockers[i];
				if (blocker.Type == null) continue;

				var restored = Undo.AddComponent(_go, blocker.Type) as Component;
				if (restored == null)
				{
					UiLog.LogError($"Error: Can not restore '{blocker.Type.Name}' on '{_go.GetPath()}'");
					continue;
				}

				if (blocker.Template)
				{
					EditorUtility.CopySerialized(blocker.Template, restored);
					EditorUtility.SetDirty(restored);
				}

				if (blocker.TemplateOwner)
					UnityEngine.Object.DestroyImmediate(blocker.TemplateOwner);
			}
		}

		/// <summary>
		/// Returns true if all [RequireComponent] dependencies of <paramref name="_componentType"/>
		/// are satisfied by the current set of present component types.
		/// A requirement is satisfied if any present concrete type is assignable to the required type.
		/// </summary>
		private static bool AreRequirementsSatisfied( Type _componentType, Dictionary<Type, int> _presentTypeCounts )
		{
			if (_componentType == null)
				return true;

			var required = GetRequiredComponentTypes(_componentType);
			if (required == null || required.Count == 0)
				return true;

			for (int i = 0; i < required.Count; i++)
			{
				var req = required[i];
				if (req == null)
					continue;

				if (!HasAssignable(_presentTypeCounts, req))
					return false;
			}

			return true;

			static bool HasAssignable( Dictionary<Type, int> _dict, Type _required )
			{
				foreach (var kv in _dict)
				{
					if (kv.Value <= 0)
						continue;

					var present = kv.Key;
					if (_required.IsAssignableFrom(present))
						return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Collects all required component types declared via [RequireComponent] on <paramref name="_t"/> (including inherited).
		/// Duplicates are removed.
		/// </summary>
		private static List<Type> GetRequiredComponentTypes( Type _t )
		{
			var list = new List<Type>();
			var set = new HashSet<Type>();

			var reqs = (RequireComponent[])Attribute.GetCustomAttributes(_t, typeof(RequireComponent), inherit: true);
			if (reqs == null || reqs.Length == 0)
				return list;

			for (int i = 0; i < reqs.Length; i++)
			{
				var r = reqs[i];
				Add(r.m_Type0);
				Add(r.m_Type1);
				Add(r.m_Type2);
			}

			return list;

			void Add( Type _req )
			{
				if (_req == null)
					return;

				if (set.Add(_req))
					list.Add(_req);
			}
		}

		private static BlockerSnapshot CaptureBlocker( Component _src )
		{
			var tmpGo = new GameObject("__tmp_blocker_snapshot__");
//			tmpGo.hideFlags = HideFlags.HideAndDontSave;

			var tmp = tmpGo.AddComponent(_src.GetType());
			EditorUtility.CopySerialized(_src, tmp);

			return new BlockerSnapshot
			{
				Type = _src.GetType(),
				Template = tmp,
				TemplateOwner = tmpGo
			};
		}
	}
}
