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
			public string Json;   // EditorJsonUtility snapshot
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

			var comps = _go.GetComponents<MonoBehaviour>();
			if (comps == null || comps.Length == 0)
				return blockers;

			// Build initial "present types" multiset after the planned removal/replacement.
			// We model presence by concrete runtime type counts; requirements are satisfied by assignability.
			var presentTypeCounts = new Dictionary<Type, int>();

			for (int i = 0; i < comps.Length; i++)
			{
				var c = comps[i];
				if (!c)
					continue;

				if (c == _savedMonoBehaviour)
					continue;

				var t = c.GetType();
				Increment(presentTypeCounts, t);
			}

			if (_replacementType != null)
				Increment(presentTypeCounts, _replacementType);

			// Determine blocker instances iteratively until stable:
			// Remove any component whose RequireComponent cannot be satisfied by current "present" set.
			var toRemove = new List<MonoBehaviour>();

			bool changed = true;
			while (changed)
			{
				changed = false;

				toRemove.Clear();

				for (int i = 0; i < comps.Length; i++)
				{
					var c = comps[i];
					if (!c)
						continue;

					if (c == _savedMonoBehaviour)
						continue;

					// already scheduled/removed?
					if (IsInBlockerList(blockers, c.GetType()) == false && c != null)
					{
						// ok - but we still need to detect if it is invalid under current presence
					}

					var ct = c.GetType();

					// Skip components that were already removed earlier in the loop
					// (Undo.DestroyObjectImmediate will null them, but be defensive)
					if (!IsPresentInstanceType(presentTypeCounts, ct))
						continue;

					if (!AreRequirementsSatisfied(ct, presentTypeCounts))
						toRemove.Add(c);
				}

				if (toRemove.Count == 0)
					break;

				// Remove all newly detected blockers from the modeled set, then loop again.
				for (int i = 0; i < toRemove.Count; i++)
				{
					var b = toRemove[i];
					if (!b)
						continue;

					var bt = b.GetType();

					// snapshot
					string json = EditorJsonUtility.ToJson(b, true);
					blockers.Add(new BlockerSnapshot { Type = bt, Json = json });

//					LogReplacement($"Temporarily delete '{bt.Name}' on '{b.GetPath()}' due to RequireComponent dependencies");

					// Update modeled presence BEFORE destroying (so subsequent checks see it missing)
					Decrement(presentTypeCounts, bt);

					Undo.DestroyObjectImmediate(b);
					changed = true;
				}

				// Refresh comps array because Unity may reorder / null entries; but keep it cheap:
				// We can just re-fetch each iteration, that's still tiny per GO.
				comps = _go.GetComponents<MonoBehaviour>();
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

			static bool IsPresentInstanceType( Dictionary<Type, int> _dict, Type _t )
			{
				if (_t == null)
					return false;

				return _dict.TryGetValue(_t, out int n) && n > 0;
			}

			static bool IsInBlockerList( List<BlockerSnapshot> _blockers, Type _t )
			{
				// Not strictly needed for correctness; keep as a cheap guard against duplicates by type.
				// If you may have multiple components of same type with different data, you should remove this guard.
				for (int i = 0; i < _blockers.Count; i++)
					if (_blockers[i].Type == _t)
						return true;

				return false;
			}
		}

		public static void RestoreBlockers( GameObject _go, List<BlockerSnapshot> _blockers )
		{
			if (_blockers == null) return;

			foreach (var blocker in _blockers)
			{
				if (blocker.Type == null) continue;
				var restored = Undo.AddComponent(_go, blocker.Type);
				if (restored == null)
				{
					UiLog.LogError($"Error: Can not restore '{blocker.Type.Name}' on '{_go.GetPath()}'");
					continue;
				}

//				LogReplacement($"Restored '{blocker.Type.Name}' on '{_go.GetPath()}'");
				if (!string.IsNullOrEmpty(blocker.Json))
				{
					// restore serialized values
//					LogReplacement($"Restoreding properties for '{blocker.Type.Name}' on '{_go.GetPath()}':\n{blocker.Json}");
					EditorJsonUtility.FromJsonOverwrite(blocker.Json, restored);
					EditorUtility.SetDirty(restored);
				}
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
    }
}
