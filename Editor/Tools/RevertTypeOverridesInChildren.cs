using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Adds "Revert Overrides of this Type in Children" to the context menu of every component on a prefab
	/// instance root. It reverts the overrides of all components of exactly the same type on the root's
	/// descendants (the component itself is left alone), e.g. the RectTransform overrides a layout group or a
	/// slip of the mouse left on the children of a prefab instance. One undo step.
	/// </summary>
	internal static class RevertTypeOverridesInChildren
	{
		private const string MenuPath = "CONTEXT/Component/Revert Overrides of this Type in Children";

		[MenuItem(MenuPath, true)]
		private static bool Validate( MenuCommand _command )
		{
			var component = _command.context as Component;
			return component != null && PrefabUtility.IsAnyPrefabInstanceRoot(component.gameObject);
		}

		[MenuItem(MenuPath)]
		private static void Execute( MenuCommand _command )
		{
			var component = _command.context as Component;
			if (component == null)
				return;

			var type = component.GetType();
			var overridden = CollectOverridden(component);
			if (overridden.Count == 0)
			{
				Debug.Log($"No {type.Name} overrides below '{component.name}'", component);
				return;
			}

			Undo.SetCurrentGroupName($"Revert {type.Name} Overrides in Children");
			int group = Undo.GetCurrentGroup();
			foreach (var child in overridden)
				PrefabUtility.RevertObjectOverride(child, InteractionMode.UserAction);
			Undo.CollapseUndoOperations(group);

			Debug.Log($"Reverted the {type.Name} overrides of {overridden.Count} object(s) below '{component.name}'", component);
		}

		/// <summary>
		/// The components of the same type below the root (not on the root itself) that are part of a prefab
		/// instance and have at least one overridden property.
		/// </summary>
		internal static List<Component> CollectOverridden( Component _root )
		{
			var result = new List<Component>();
			var type = _root.GetType();
			foreach (var candidate in _root.GetComponentsInChildren(type, true))
			{
				// Exactly the type: GetComponentsInChildren finds subclasses too (Image -> UiImage)
				if (candidate == null || candidate.gameObject == _root.gameObject || candidate.GetType() != type)
					continue;

				if (!PrefabUtility.IsPartOfPrefabInstance(candidate) || PrefabUtility.IsAddedComponentOverride(candidate))
					continue;

				if (HasOverriddenProperty(candidate))
					result.Add(candidate);
			}

			return result;
		}

		private static bool HasOverriddenProperty( Component _component )
		{
			using var serializedObject = new SerializedObject(_component);
			var property = serializedObject.GetIterator();
			while (property.Next(true))
			{
				if (property.prefabOverride)
					return true;
			}

			return false;
		}
	}
}
