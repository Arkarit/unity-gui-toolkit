#if UNITY_EDITOR
using Codice.Client.BaseCommands.Config;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
using static Codice.CM.WorkspaceServer.WorkspaceTreeDataStore;

namespace GuiToolkit
{
	public class UiApplyStyleGenerator : EditorWindow
	{
		private MonoBehaviour m_MonoBehaviour;
		private MonoBehaviour m_lastMonoBehaviour;
		private List<PropertyRecord> m_PropertyRecords = new();
		private Vector2 m_ScrollPos;

		private GUIStyle[] m_alternatingRowStyles = new GUIStyle[2];
		private static readonly HashSet<string> s_filteredNames = new() { "name", "enabled", "tag", "hideFlags", "runInEditMode", "useGUILayout" };

		#region Templates

		// Format string:
		// 0: qualified property type name
		// 1: member name
		private const string StyleMemberTemplate =
			"		[SerializeField] private ApplicableValue<{0}> m_{1} = new();\n";
		private string GetStyleMember(string _qualifiedPropertyTypeName, string _memberName)
		{
			return string.Format(StyleMemberTemplate, _qualifiedPropertyTypeName, _memberName);
		}

		// Format string:
		// 0: qualified property type name
		// 1: short property type name, starting with upper char
		// 2: member name
		// 3: member name
		private const string StylePropertyTemplate =
			"		public {0} {1}\n" +
			"		{\n" +
			"			get => m_{2}.Value;" +
			"			set => m_{3}.Value = value;" +
			"		}";

		private string GetStyleProperty(string _qualifiedPropertyTypeName, string _shortPropertyName, string _memberName)
		{
			_shortPropertyName = UpperFirstChar(_shortPropertyName);
			return string.Format
			(
				StylePropertyTemplate, 
				_qualifiedPropertyTypeName, 
				_shortPropertyName, 
				_memberName,
				_memberName
			);
		}

		// Format string:
		// 0: namespace
		// 1: short type name,
		// 2: qualified type name
		// 3: members (starting with 2 tabs)
		// 4: properties (starting with 2 tabs)
		private const string StyleTemplate =
			"using System;\n" +
			"using UnityEngine;\n" +
			"using UnityEngine.UI;\n" +
			"\n" +
			"namespace {0}\n" +
			"{\n" +
			"	[Serializable]\n" +
			"	public class UiStyle{1} : UiAbstractStyle<{2}>\n" +
			"	{\n" +
			"{3}" +
			"\n" +
			"{4}" +
			"	}\n" +
			"}\n";
		#endregion

		[Serializable]
		private class PropertyRecord
		{
			public bool Used;
			public string Name;
			public string TypeName;
		}

		[Serializable]
		private class PropertyRecordsJson
		{
			public Type Type;
			public PropertyRecord[] Records;
		}

		#region Drawing
		private void OnGUI()
		{
			if (m_MonoBehaviour == null)
			{
				EditorGUILayout.HelpBox("Drag a mono behaviour into this field to generate", MessageType.Info);
			}

			m_MonoBehaviour = EditorGUILayout.ObjectField(m_MonoBehaviour, typeof(MonoBehaviour), true) as MonoBehaviour;

			if (!m_MonoBehaviour)
				return;


			m_alternatingRowStyles[0] = GUIStyle.none;
			m_alternatingRowStyles[1] = new GUIStyle();
			m_alternatingRowStyles[1].normal.background = MakeTex(1, 1, new Color(1.0f, 1.0f, 1.0f, 0.15f));

			CollectPropertiesIfNecessary();
			DrawProperties();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button($"Show Hidden ({NumHidden})"))
				ShowHidden();

			if (UiToolkitConfiguration.Instance.IsEditingInternal)
			{
				if (GUILayout.Button("Write JSON (internal)"))
					WriteJson(true);

				if (GUILayout.Button("Generate (internal)"))
					Generate(true);
			}

			if (GUILayout.Button("Write JSON"))
				WriteJson(false);

			if (GUILayout.Button("Generate"))
				Generate(false);

			EditorGUILayout.EndHorizontal();
		}

		private void DrawProperties()
		{
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
			int count = 0;
			foreach (var propertyRecord in m_PropertyRecords)
			{
				if (!propertyRecord.Used)
					continue;

				DrawProperty(propertyRecord, m_alternatingRowStyles[count++ & 1]);
			}

			EditorGUILayout.EndScrollView();
		}

		private void DrawProperty(PropertyRecord _propertyRecord, GUIStyle _guiStyle)
		{
			EditorGUILayout.BeginHorizontal(_guiStyle);
			EditorGUILayout.Space(10, false);
			_propertyRecord.Used = GUILayout.Toggle(_propertyRecord.Used, "", GUILayout.Width(20));
			EditorGUILayout.LabelField($"{_propertyRecord.Name}", GUILayout.Width(200));
			EditorGUILayout.LabelField($"({_propertyRecord.TypeName})", GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
		}
		#endregion

		#region Calculations
		private void ShowHidden()
		{
			foreach (var propertyRecord in m_PropertyRecords)
				propertyRecord.Used = true;
		}

		private int NumHidden
		{
			get
			{
				int result = 0;

				foreach (var propertyRecord in m_PropertyRecords)
				{
					if (!propertyRecord.Used)
						result++;
				}

				return result;
			}
		}
		#endregion

		#region Json
		private void WriteJson(bool _internal)
		{
			var jsonClass = new PropertyRecordsJson();
			jsonClass.Type = m_MonoBehaviour.GetType();
			jsonClass.Records = m_PropertyRecords.ToArray();
			string path = _internal ?
				UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir + $"Type-Json/{m_MonoBehaviour.GetType().FullName}.json" :
				UiToolkitConfiguration.Instance.GeneratedAssetsDir + $"{m_MonoBehaviour.GetType().FullName}.json";

			try
			{
				string json = JsonUtility.ToJson(jsonClass, true);
				File.WriteAllText(path, json);
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not write Json, reason:'{e.Message}'");
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		}

		private bool FindJson()
		{
			string path = UiToolkitConfiguration.Instance.GeneratedAssetsDir + $"{m_MonoBehaviour.GetType().FullName}.json";
			if (TryReadJson(path))
				return true;

			string pathInternal = UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir +
			                      $"Type-Json/{m_MonoBehaviour.GetType().FullName}.json";
			if (TryReadJson(pathInternal))
				return true;

			return false;
		}

		private bool TryReadJson(string path)
		{
			try
			{
				var content = File.ReadAllText(path);
				if (string.IsNullOrEmpty(content))
					return false;

				var propertyRecordsJson = JsonUtility.FromJson<PropertyRecordsJson>(content);
				m_PropertyRecords = propertyRecordsJson.Records.ToList();
				return true;
			}
			catch
			{
				return false;
			}
		}
		#endregion

		#region Data
		private void CollectPropertiesIfNecessary()
		{
			if (m_lastMonoBehaviour == m_MonoBehaviour && m_PropertyRecords.Count > 0)
				return;

			m_lastMonoBehaviour = m_MonoBehaviour;
			m_PropertyRecords.Clear();

			if (FindJson())
				return;

			var propertyInfos = m_MonoBehaviour.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var propertyInfo in propertyInfos)
			{
				if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
					continue;

				m_PropertyRecords.Add(new PropertyRecord()
				{
					Used = !s_filteredNames.Contains(propertyInfo.Name),
					Name = propertyInfo.Name,
					TypeName = propertyInfo.PropertyType.FullName.Replace("+", ".")
				});
			}
		}
		#endregion

		#region Generation
		private void Generate(bool _internal)
		{
		}
		#endregion

		#region Helper
		[MenuItem(StringConstants.APPLY_STYLE_GENERATOR_MENU_NAME, priority = Constants.STYLES_HEADER_PRIORITY)]
		public static UiApplyStyleGenerator GetWindow()
		{
			var window = GetWindow<UiApplyStyleGenerator>();
			window.titleContent = new GUIContent("'Ui Apply Style' Generator");
			window.Focus();
			window.Repaint();
			return window;
		}

		private static Texture2D MakeTex(int width, int height, Color col)
		{
			Color[] pix = new Color[width*height];
 
			for(int i = 0; i < pix.Length; i++)
				pix[i] = col;
 
			Texture2D result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
 
			return result;
		}

		private static string UpperFirstChar(string _s)
		{
			if (string.IsNullOrEmpty(_s))
				return string.Empty;
			return _s[0].ToString().ToUpper() + _s.Substring(1);
		}
		#endregion

	}
}
#endif