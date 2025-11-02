// File: Assets/Editor/UiLocaTools/SetAllUiLocaGroups.cs
// Purpose: Set the Loca "Group" on all UiLocaComponent in the current Scene or Prefab Stage.

#if UNITY_EDITOR
using System.Collections.Generic;
using GuiToolkit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.SceneManagement; // PrefabStageUtility is in this namespace in recent Unity
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	public static class SetAllUiLocaGroups
	{
		[MenuItem(StringConstants.SET_ALL_UI_LOCA_GROUPS)]
		private static void SetGroupForAll()
		{
			// 1) Ask for the group text.
			string description = "Set the Loca 'Group' for all UiLocaComponent in the current " +
								 "Scene or the open Prefab Stage.\n\n" +
								 "Leave empty to clear the group.";
			string group = EditorInputDialog.Show(
				"Set Loca Group",
				description,
				string.Empty,
				null,
				"Apply",
				"Cancel"
			);

			// If dialog was canceled, most implementations return null; guard for that.
			if (group == null)
				return;

			// 2) Determine context: Prefab Stage vs. regular Scene.
			PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			bool isPrefabStage = prefabStage != null;

			List<UiLocaComponent> targets = new List<UiLocaComponent>();
			if (isPrefabStage)
			{
				GameObject root = prefabStage.prefabContentsRoot;
				if (root != null)
					targets.AddRange(root.GetComponentsInChildren<UiLocaComponent>(true));
			}
			else
			{
				Scene scene = SceneManager.GetActiveScene();
				if (!scene.IsValid())
				{
					EditorUtility.DisplayDialog("Set Loca Group",
						"No valid active Scene found.", "OK");
					return;
				}

				CollectFromScene(scene, targets);
			}

			if (targets.Count == 0)
			{
				EditorUtility.DisplayDialog("Set Loca Group",
					"No UiLocaComponent found in the current context.", "OK");
				return;
			}

			// 3) Apply the group with full Undo support and mark dirty.
			try
			{
				EditorUtility.DisplayProgressBar("Setting Loca Group",
					"Updating UiLocaComponent...", 0f);

				const string undoName = "Set Loca Group";
				int count = targets.Count;

				for (int i = 0; i < count; i++)
				{
					UiLocaComponent comp = targets[i];
					if (comp == null)
						continue;

					float progress = (i + 1) / (float)count;
					EditorUtility.DisplayProgressBar("Setting Loca Group",
						$"Updating: {comp.name}", progress);

					Undo.RecordObject(comp, undoName);

					// Use SerializedObject to touch the serialized private field explicitly.
					SerializedObject so = new SerializedObject(comp);
					SerializedProperty prop = so.FindProperty("m_group");
					if (prop != null)
					{
						prop.stringValue = group;
						so.ApplyModifiedProperties();
					}
					else
					{
						// Fallback through public API (internal setter) if field was not found.
						comp.Group = group ?? string.Empty;
						EditorUtility.SetDirty(comp);
					}
				}

				// Mark context dirty so the change is saved.
				if (isPrefabStage)
				{
					EditorSceneManager.MarkSceneDirty(prefabStage.scene);
				}
				else
				{
					Scene scene = SceneManager.GetActiveScene();
					if (scene.IsValid())
						EditorSceneManager.MarkSceneDirty(scene);
				}

				Debug.Log($"Set Loca group to '{group}' on {targets.Count} UiLocaComponent(s).");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static void CollectFromScene( Scene _scene, List<UiLocaComponent> _results )
		{
			List<GameObject> roots = new List<GameObject>(64);
			_scene.GetRootGameObjects(roots);

			for (int r = 0; r < roots.Count; r++)
			{
				GameObject root = roots[r];
				if (root == null)
					continue;

				UiLocaComponent[] comps = root.GetComponentsInChildren<UiLocaComponent>(true);
				if (comps != null && comps.Length > 0)
					_results.AddRange(comps);
			}
		}
	}
}
#endif
