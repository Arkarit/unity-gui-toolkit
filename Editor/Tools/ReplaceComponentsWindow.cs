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

			var handling = AreValidComponentScripts(m_sourceScript, m_targetScript, out string buttonMessage);
			UiLog.Log($"Handling:{handling}");

			using (new EditorGUI.DisabledScope(handling == ScriptReplacementHandling.None))
			{
				if (GUILayout.Button("Replace" + buttonMessage))
				{
					var srcType = m_sourceScript.GetClass();
					var dstType = m_targetScript.GetClass();

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

		private static ScriptReplacementHandling AreValidComponentScripts( MonoScript _src, MonoScript _dst, out string _message )
		{
			_message = string.Empty;

			if (!IsValidComponentScript(_src))
			{
				_message = $" (Can not replace; Source {_src.GetClass()} is not a valid Monobehaviour)";
				return ScriptReplacementHandling.None;
			}

			if (!IsValidComponentScript(_dst))
			{
				_message = $" (Can not replace; Destination {_dst.GetClass()} is not a valid Monobehaviour)";
				return ScriptReplacementHandling.None;
			}

			Type srcType = _src.GetClass();
			Type dstType = _dst.GetClass();

			if (srcType == null || dstType == null)
			{
				_message = $" (Can not replace; one of the script types is null)";
				return ScriptReplacementHandling.None;
			}

			if (srcType == dstType)
			{
				_message = $" (Nothing to replace; source and destination type are equal)";
				return ScriptReplacementHandling.None;
			}

			if (typeof(Text).IsAssignableFrom(srcType) &&
			    typeof(TextMeshProUGUI).IsAssignableFrom(dstType))
			{
				_message = $" Text -> TextMeshProUGUI";
				return ScriptReplacementHandling.TextMeshPro;
			}

			if (dstType.IsAssignableFrom(srcType) || srcType.IsAssignableFrom(dstType))
				return ScriptReplacementHandling.GenericRelated;

			return ScriptReplacementHandling.GenericUnrelated;
		}

		private static bool IsValidComponentScript( MonoScript _ms )
		{
			if (_ms == null)
				return false;

			Type type = _ms.GetClass();

			return type != null &&
				typeof(MonoBehaviour).IsAssignableFrom(type) &&
				!type.IsAbstract;
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