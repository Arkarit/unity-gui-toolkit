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

			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

			MethodInfo method = typeof(GuiToolkit.Editor.EditorCodeUtility).GetMethod(
				"ReplaceMonoBehaviourInCurrentContext",
				flags
			);

			if (method == null)
				throw new MissingMethodException("EditorCodeUtility.ReplaceMonoBehaviourInCurrentContext<T1,T2>() not found.");

			MethodInfo closed = method.MakeGenericMethod(_srcType, _dstType);

			try
			{
				closed.Invoke(null, null);
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