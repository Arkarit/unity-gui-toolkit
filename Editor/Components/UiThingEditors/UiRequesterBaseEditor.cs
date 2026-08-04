using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Adds the "Requester Button Overrides" foldout to every requester. Derives from
	/// <see cref="UiThingEditor"/> rather than replacing it, so requesters keep the members/events layout
	/// every other UiThing has; the three override fields are pulled out of that layout via
	/// <see cref="excludedProperties"/> and drawn in the closed foldout instead.
	/// </summary>
	[CustomEditor(typeof(UiRequesterBase), true)]
	public class UiRequesterBaseEditor : UiThingEditor
	{
		private const string FoldoutPrefKey = "GuiToolkit.UiRequesterBaseEditor.ButtonOverridesFoldout";

		private const string OverrideHelp =
			"Not needed in a properly set-up project. Requester buttons come from the standard elements "
			+ "StandardButton, OkButton and CancelButton, resolved through the generated "
			+ "UiStandardElementRegistry.\n\n"
			+ "Set one of these only to satisfy a legacy look the registry cannot express — for instance "
			+ "when the project's live dialogs inherit from a differently styled button and re-styling that "
			+ "prefab would change screens that are already shipped.\n\n"
			+ "Whatever is set here beats every other route to a button prefab, including a ButtonInfo "
			+ "whose Prefab a caller set explicitly.";

		private static readonly HashSet<string> s_excludedProperties = new()
		{
			"m_standardButtonPrefabOverride",
			"m_okButtonPrefabOverride",
			"m_cancelButtonPrefabOverride",
		};

		private readonly List<SerializedProperty> m_overrideProperties = new();

		protected override HashSet<string> excludedProperties => s_excludedProperties;

		protected override void OnEnable()
		{
			base.OnEnable();

			m_overrideProperties.Clear();
			foreach (var name in s_excludedProperties)
			{
				var property = serializedObject.FindProperty(name);
				if (property != null)
				{
					m_overrideProperties.Add(property);
				}
			}
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (m_overrideProperties.Count == 0)
			{
				return;
			}

			serializedObject.Update();

			EditorGUILayout.Space();

			bool foldout = EditorPrefs.GetBool(FoldoutPrefKey, false);
			bool newFoldout = EditorGUILayout.Foldout(foldout, "Requester Button Overrides", true);
			if (newFoldout != foldout)
			{
				EditorPrefs.SetBool(FoldoutPrefKey, newFoldout);
			}

			if (newFoldout)
			{
				EditorGUILayout.HelpBox(OverrideHelp, MessageType.Info);

				foreach (var property in m_overrideProperties)
				{
					EditorGUILayout.PropertyField(property);
				}
			}
			else if (HasAnyOverride())
			{
				// A closed foldout must not hide the fact that something in it is active, or the next
				// person debugging an unexpected button look has no reason to open it.
				EditorGUILayout.LabelField("(button prefab overrides are set)", EditorStyles.miniLabel);
			}

			serializedObject.ApplyModifiedProperties();
		}

		private bool HasAnyOverride()
		{
			foreach (var property in m_overrideProperties)
			{
				if (property.objectReferenceValue != null)
				{
					return true;
				}
			}

			return false;
		}
	}
}
