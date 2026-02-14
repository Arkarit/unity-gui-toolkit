using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
			public List<(UnityEngine.Object owner, string propertyPath)> Referrers;
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
		/// <param name="_preserveReferences">
		/// If true, scans current context (and optionally entire project) for references to blocking components before removal.
		/// More expensive but ensures references are preserved across removal/restore.
		/// </param>
		/// <param name="_scanEntireProject">
		/// If true (and _preserveReferences is true), scans entire project including prefabs and ScriptableObjects.
		/// If false, only scans current scene/prefab context (much faster).
		/// </param>
		public static List<BlockerSnapshot> CaptureAndRemoveBlockers( 
			GameObject _go, 
			MonoBehaviour _savedMonoBehaviour, 
			Type _replacementType = null, 
			bool _preserveReferences = true,
			bool _scanEntireProject = false )
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

			// DEBUG: Log what we have
			UiLog.LogInternal($"[CaptureAndRemoveBlockers] Saved component: {_savedMonoBehaviour?.GetType().Name}");
			UiLog.LogInternal($"[CaptureAndRemoveBlockers] Replacement type: {_replacementType?.Name ?? "null"}");
			UiLog.LogInternal($"[CaptureAndRemoveBlockers] Present types after planned removal:");
			foreach (var kv in presentTypeCounts)
			{
				UiLog.LogInternal($"  - {kv.Key.Name}: {kv.Value}");
			}

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

					var required = GetRequiredComponentTypes(mt);
					UiLog.LogInternal($"[CaptureAndRemoveBlockers] Checking {mt.Name}, requires: {string.Join(", ", required.ConvertAll(t => t?.Name ?? "null"))}");

					if (!AreRequirementsSatisfied(mt, presentTypeCounts))
					{
						UiLog.LogInternal($"  -> {mt.Name} WILL BE REMOVED (requirements not satisfied)");
						toRemove.Add(mb);
					}
				}

				if (toRemove.Count == 0)
					break;

				UiLog.LogInternal($"[CaptureAndRemoveBlockers] Removing {toRemove.Count} blockers:");
				for (int i = 0; i < toRemove.Count; i++)
				{
					var b = toRemove[i];
					if (!b)
						continue;

					var bt = b.GetType();
					UiLog.LogInternal($"  - Removing {bt.Name}");
					blockers.Add(CaptureBlocker(b, _preserveReferences, _scanEntireProject));

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

			UiLog.LogInternal($"[CaptureAndRemoveBlockers] Final: {blockers.Count} blockers captured");
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

			UiLog.LogInternal($"[RestoreBlockers] Restoring {_blockers.Count} blockers");

			for (int i = _blockers.Count - 1; i >= 0; i--)
			{
				var blocker = _blockers[i];
				if (blocker.Type == null) continue;

				UiLog.LogInternal($"  - Restoring {blocker.Type.Name}, has {blocker.Referrers?.Count ?? 0} referrers");

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

				// Restore references to the restored component
				if (blocker.Referrers != null && blocker.Referrers.Count > 0)
				{
					UiLog.LogInternal($"    Rewiring {blocker.Referrers.Count} references");
					ProjectReferrerUtility.RewireReferrers(blocker.Referrers, restored);
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
		/// Uses Unity's internal ComponentUtility for better reliability.
		/// </summary>
		private static List<Type> GetRequiredComponentTypes( Type _t )
		{
			var list = new List<Type>();
			var set = new HashSet<Type>();

			if (_t == null)
				return list;

			// Method 1: Try reflection (works for most cases)
			try
			{

				// Use GetCustomAttributes on the type and all base types explicitly
				var currentType = _t;
				while (currentType != null && currentType != typeof(object))
				{
					var attrs = currentType.GetCustomAttributes(typeof(RequireComponent), false);
					if (attrs != null && attrs.Length > 0)
					{
						foreach (RequireComponent req in attrs)
						{
							Add(req.m_Type0);
							Add(req.m_Type1);
							Add(req.m_Type2);
						}
					}
					currentType = currentType.BaseType;
				}
			}
			catch (Exception ex)
			{
				UiLog.LogInternal($"[GetRequiredComponentTypes] Reflection failed for {_t.Name}: {ex}");
			}

			// Method 2: Unity-specific fallback for built-in types
			if (list.Count == 0)
			{
				// Check if the type implements IMeshModifier
				if (typeof(UnityEngine.UI.IMeshModifier).IsAssignableFrom(_t))
				{
					Add(typeof(UnityEngine.UI.Graphic));
					UiLog.LogInternal($"[GetRequiredComponentTypes] Applied rule: {_t.Name} (IMeshModifier) requires Graphic");
				}

				// Check for UI components requiring RectTransform
				if (typeof(UnityEngine.UI.Graphic).IsAssignableFrom(_t) || typeof(UnityEngine.EventSystems.UIBehaviour).IsAssignableFrom(_t))
				{
					Add(typeof(UnityEngine.RectTransform));
					UiLog.LogInternal($"[GetRequiredComponentTypes] Applied rule: {_t.Name} (UI Component) requires RectTransform");
				}

				// Check for CanvasRenderer dependency
				if (typeof(UnityEngine.UI.Graphic).IsAssignableFrom(_t))
				{
					Add(typeof(UnityEngine.CanvasRenderer));
					UiLog.LogInternal($"[GetRequiredComponentTypes] Applied rule: {_t.Name} (UI Graphic) requires CanvasRenderer");
				}

				// Check for ParticleSystemRenderer dependency
				if (_t == typeof(UnityEngine.ParticleSystem))
				{
					Add(typeof(UnityEngine.ParticleSystemRenderer));
					UiLog.LogInternal($"[GetRequiredComponentTypes] Applied rule: {_t.Name} (ParticleSystem) requires ParticleSystemRenderer");
				}
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

		private static BlockerSnapshot CaptureBlocker( Component _src, bool _preserveReferences, bool _scanEntireProject )
		{
			var tmpGo = new GameObject("__tmp_blocker_snapshot__");
			tmpGo.hideFlags = HideFlags.HideAndDontSave;

			var tmp = tmpGo.AddComponent(_src.GetType());
			EditorUtility.CopySerialized(_src, tmp);

			// Optionally capture all references to this component before removal
			List<(UnityEngine.Object owner, string propertyPath)> referrers = null;
			if (_preserveReferences)
			{
				UiLog.LogInternal($"    Collecting referrers for {_src.GetType().Name} (scanProject: {_scanEntireProject})");
				referrers = _scanEntireProject 
					? ProjectReferrerUtility.CollectReferrersInProject(_src)
					: ProjectReferrerUtility.CollectReferrersInCurrentContext(_src);
				UiLog.LogInternal($"    Found {referrers?.Count ?? 0} referrers");
			}

			return new BlockerSnapshot
			{
				Type = _src.GetType(),
				Template = tmp,
				TemplateOwner = tmpGo,
				Referrers = referrers
			};
		}
	}
}