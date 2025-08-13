using UnityEditor;
using UnityEngine;
using System;

namespace GuiToolkit.Editor
{
	public class ReplaceComponentsWindow : EditorWindow
	{
		private MonoScript sourceScript;
		private MonoScript targetScript;

		[MenuItem(StringConstants.REPLACE_COMPONENTS_WINDOW)]
		public static void ShowWindow()
		{
			var w = GetWindow<ReplaceComponentsWindow>("Replace Components");
			w.minSize = new Vector2(420, 120);
		}

		private void OnEnable()
		{
			// Defaults: Text -> TextMeshProUGUI (NOT TMP_Text -> abstract)
			sourceScript = GetMonoScriptForClass(typeof(UnityEngine.UI.Text));
			targetScript = GetMonoScriptForClass(typeof(TMPro.TextMeshProUGUI));
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Replace in Active Scene", EditorStyles.boldLabel);

			sourceScript = (MonoScript)EditorGUILayout.ObjectField(
				"Source Component", sourceScript, typeof(MonoScript), false);

			targetScript = (MonoScript)EditorGUILayout.ObjectField(
				"Target Component", targetScript, typeof(MonoScript), false);

			EditorGUILayout.Space();

			using (new EditorGUI.DisabledScope(!IsValidComponentScript(sourceScript) || !IsValidComponentScript(targetScript)))
			{
				if (GUILayout.Button("Replace Now"))
				{
					var srcType = sourceScript.GetClass();
					var dstType = targetScript.GetClass();

					if (srcType.IsAbstract || dstType.IsAbstract)
					{
						Debug.LogError("Source and target must be non-abstract Component types.");
						return;
					}
					if (!typeof(Component).IsAssignableFrom(srcType) ||
						!typeof(Component).IsAssignableFrom(dstType))
					{
						Debug.LogError("Both source and target must inherit from UnityEngine.Component.");
						return;
					}

					var result = EditorCodeUtility.ReplaceUITextWithTMPInActiveScene();
					Debug.Log($"Replaced {result?.Count ?? 0} components in active scene.");
				}
			}
		}

		private static bool IsValidComponentScript( MonoScript ms )
			=> ms != null && typeof(Component).IsAssignableFrom(ms.GetClass()) && !ms.GetClass().IsAbstract;

		private static MonoScript GetMonoScriptForClass( Type t )
		{
			var go = new GameObject("__tmp__");
			try
			{
				var comp = go.AddComponent(t) as MonoBehaviour;
				return MonoScript.FromMonoBehaviour(comp);
			}
			finally { GameObject.DestroyImmediate(go); }
		}
	}
}