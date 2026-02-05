using UnityEditor;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	public class ReplaceComponentsWindow : EditorWindow
	{
		private enum ScriptReplacementHandling
		{
			None,
			GenericRelated,
			GenericUnrelated,
			TextMeshPro,
		}

		private MonoScript m_sourceScript;
		private MonoScript m_targetScript;

		[MenuItem(StringConstants.REPLACE_COMPONENTS_WINDOW)]
		public static void ShowWindow()
		{
			var w = GetWindow<ReplaceComponentsWindow>("Replace Components");
			w.minSize = new Vector2(420, 120);
		}

		private void OnEnable()
		{
			// Defaults: Text -> TextMeshProUGUI (NOT TMP_Text -> abstract)
			m_sourceScript = GetMonoScriptForClass(typeof(Text));
			m_targetScript = GetMonoScriptForClass(typeof(TextMeshProUGUI));
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Replace in Active Scene", EditorStyles.boldLabel);

			m_sourceScript = (MonoScript)EditorGUILayout.ObjectField(
				"Source Component", m_sourceScript, typeof(MonoScript), false);

			m_targetScript = (MonoScript)EditorGUILayout.ObjectField(
				"Target Component", m_targetScript, typeof(MonoScript), false);

			EditorGUILayout.Space();

			var handling = AreValidComponentScripts(m_sourceScript, m_targetScript);

			using (new EditorGUI.DisabledScope(handling == ScriptReplacementHandling.None))
			{
				if (GUILayout.Button("Replace Now"))
				{
					var srcType = m_sourceScript.GetClass();
					var dstType = m_targetScript.GetClass();

					if (srcType.IsAbstract || dstType.IsAbstract)
					{
						UiLog.LogError("Source and target must be non-abstract Component types.");
						return;
					}
					if (!typeof(Component).IsAssignableFrom(srcType) ||
						!typeof(Component).IsAssignableFrom(dstType))
					{
						UiLog.LogError("Both source and target must inherit from UnityEngine.Component.");
						return;
					}

					switch (handling)
					{
						case ScriptReplacementHandling.TextMeshPro:
							EditorCodeUtility.ReplaceTextWithTextMeshProInCurrentContext();
							break;
						case ScriptReplacementHandling.GenericRelated:
							//TODO replace components
							break;
						case ScriptReplacementHandling.GenericUnrelated:
							//TODO
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
					
				}
			}
		}

		private static ScriptReplacementHandling AreValidComponentScripts(MonoScript _src, MonoScript _dst)
		{
			if (!IsValidComponentScript(_src) || !IsValidComponentScript(_dst))
				return ScriptReplacementHandling.None;

			var srcType = _src.GetType();
			var dstType = _dst.GetType();

			// Works, but makes no sense
			if (srcType == dstType)
				return ScriptReplacementHandling.None;

			if (typeof(Text).IsAssignableFrom(srcType) && typeof(TMP_Text).IsAssignableFrom(dstType))
				return ScriptReplacementHandling.TextMeshPro;

			if (dstType.IsAssignableFrom(srcType) || srcType.IsAssignableFrom(dstType))
				return ScriptReplacementHandling.GenericRelated;

			return ScriptReplacementHandling.GenericUnrelated;
		}
		private static bool IsValidComponentScript( MonoScript _ms )
		{
			return _ms != null && typeof(Component).IsAssignableFrom(_ms.GetClass()) && !_ms.GetClass().IsAbstract;
		}

		private static MonoScript GetMonoScriptForClass( Type _t )
		{
			var go = new GameObject("__tmp__");
			try
			{
				var comp = go.AddComponent(_t) as MonoBehaviour;
				return MonoScript.FromMonoBehaviour(comp);
			}
			finally { GameObject.DestroyImmediate(go); }
		}
	}
}