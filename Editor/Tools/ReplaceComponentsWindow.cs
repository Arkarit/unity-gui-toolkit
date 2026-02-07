using UnityEditor;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using System.Reflection;

namespace GuiToolkit.Editor
{
	public class ReplaceComponentsWindow : EditorWindow
	{
		private enum ScriptReplacementHandling
		{
			None,
			Generic,
			TextMeshPro,
		}

		private const string PREF_SRC_GUID = "GuiToolkit.ReplaceComponentsWindow.SourceGuid";
		private const string PREF_DST_GUID = "GuiToolkit.ReplaceComponentsWindow.TargetGuid";

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
			// Try restore from prefs first
			m_sourceScript = LoadMonoScriptFromPrefs(PREF_SRC_GUID);
			m_targetScript = LoadMonoScriptFromPrefs(PREF_DST_GUID);

			// Fallback defaults
			if (m_sourceScript == null)
				m_sourceScript = GetMonoScriptForClass(typeof(Text));

			if (m_targetScript == null)
				m_targetScript = GetMonoScriptForClass(typeof(TextMeshProUGUI));
		}

		private void OnDisable()
		{
			SaveMonoScriptToPrefs(PREF_SRC_GUID, m_sourceScript);
			SaveMonoScriptToPrefs(PREF_DST_GUID, m_targetScript);
		}

		private static void SaveMonoScriptToPrefs( string _prefKey, MonoScript _script )
		{
			if (string.IsNullOrEmpty(_prefKey))
				return;

			if (_script == null)
			{
				EditorPrefs.DeleteKey(_prefKey);
				return;
			}

			string path = AssetDatabase.GetAssetPath(_script);
			if (string.IsNullOrEmpty(path))
			{
				EditorPrefs.DeleteKey(_prefKey);
				return;
			}

			string guid = AssetDatabase.AssetPathToGUID(path);
			if (string.IsNullOrEmpty(guid))
			{
				EditorPrefs.DeleteKey(_prefKey);
				return;
			}

			EditorPrefs.SetString(_prefKey, guid);
		}

		private static MonoScript LoadMonoScriptFromPrefs( string _prefKey )
		{
			if (string.IsNullOrEmpty(_prefKey))
				return null;

			if (!EditorPrefs.HasKey(_prefKey))
				return null;

			string guid = EditorPrefs.GetString(_prefKey, "");
			if (string.IsNullOrEmpty(guid))
				return null;

			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path))
				return null;

			return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
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
						case ScriptReplacementHandling.Generic:
							InvokeReplaceMonoBehaviourInCurrentContext(srcType, dstType);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}

		private static void InvokeReplaceMonoBehaviourInCurrentContext( Type _srcType, Type _dstType )
		{
			if (_srcType == null)
				throw new ArgumentNullException(nameof(_srcType));
			if (_dstType == null)
				throw new ArgumentNullException(nameof(_dstType));

			if (!typeof(MonoBehaviour).IsAssignableFrom(_srcType))
				throw new ArgumentException($"Source type '{_srcType.FullName}' is not a MonoBehaviour.", nameof(_srcType));

			if (!typeof(MonoBehaviour).IsAssignableFrom(_dstType))
				throw new ArgumentException($"Target type '{_dstType.FullName}' is not a MonoBehaviour.", nameof(_dstType));

			try
			{
				typeof(EditorCodeUtility).CallStaticMethod(_srcType, _dstType, "ReplaceMonoBehaviourInCurrentContext", out bool _);
			}
			catch (TargetInvocationException ex)
			{
				// unwrap to show the real error in console
				throw ex.InnerException ?? ex;
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

			_message = $" {srcType.Name} -> {dstType.Name}";
			return ScriptReplacementHandling.Generic;
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