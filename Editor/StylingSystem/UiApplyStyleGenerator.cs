#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

namespace GuiToolkit
{
	public class UiApplyStyleGenerator : EditorWindow
	{
		private const string GeneratedWarningComment = "// Auto-generated, please do not change!\n";

		private MonoBehaviour m_monoBehaviour;
		private MonoBehaviour m_lastMonoBehaviour;
		private Type m_monoBehaviourType;
		private List<PropertyRecord> m_PropertyRecords = new();
		private Vector2 m_ScrollPos;

		private GUIStyle[] m_alternatingRowStyles = new GUIStyle[2];
		private static readonly HashSet<string> s_filteredNames = new() { "name", "enabled", "tag", "hideFlags", "runInEditMode", "useGUILayout" };
		private string m_namespace;
		private string m_prefix;

		#region Types
		private const int JsonVersion = 2;

		[Serializable]
		private class PropertyRecord
		{
			public bool Used;
			public string Name;
			public string QualifiedTypeName;
			public string TypeName;
		}

		[Serializable]
		private class PropertyRecordsJson
		{
			public int Version;
			public string Namespace;
			public string Prefix;
			public PropertyRecord[] Records;
		}
		#endregion

		#region Style class Templates
		// Format string:
		// 0: qualified property type name
		// 1: member name
		private const string StyleMemberTemplate =
			"		[SerializeField] private ApplicableValue<{0}> m_{1} = new();\n";
		private string GetStyleMemberString(string _qualifiedPropertyTypeName, string _memberName)
		{
			return string.Format
			(
				StyleMemberTemplate, 
				_qualifiedPropertyTypeName, 
				_memberName
			);
		}

		// Format string:
		// 0: qualified property type name
		// 1: short property type name, starting with upper char
		// 2: member name
		// 3: member name
		private const string StylePropertyTemplate =
			  "		public {0} {1}\n"
			+ "		{{\n"
			+ "			get => m_{2}.Value;\n"
			+ "			set => m_{3}.Value = value;\n"
			+ "		}}\n"
			+ "\n";
		private string GetStylePropertyString(string _qualifiedPropertyTypeName, string _shortPropertyName, string _memberName)
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
		// 1: prefix
		// 2: short type name,
		// 3: qualified type name
		// 4: members (starting with 2 tabs)
		// 5: properties (starting with 2 tabs)
		private const string StyleTemplate =
			GeneratedWarningComment
			+ "using System;\n"
			+ "using UnityEngine;\n"
			+ "using GuiToolkit.Style;\n"
			+ "\n"
			+ "namespace {0}\n"
			+ "{{\n"
			+ "	[Serializable]\n"
			+ "	public class UiStyle{1}{2} : UiAbstractStyle<{3}>\n"
			+ "	{{\n"
			+ "{4}"
			+ "\n"
			+ "{5}"
			+ "	}}\n"
			+ "}}\n";

		private string GetStyleString(string _namespace, string _prefix, string _shortTypeName, string _qualifiedTypeName, string _members, string _properties)
		{
			return string.Format
			(
				StyleTemplate,
				_namespace,
				_prefix,
				_shortTypeName,
				_qualifiedTypeName,
				_members,
				_properties
			);
		}

		#endregion

		#region Apply class Templates
		// Format string:
		// 0: member name
		// 1: member name, starting with upper char
		private const string ApplyInstructionTemplate =
			"			SpecificMonoBehaviour.{0} = SpecificStyle.{1};\n";
		private string GetApplyInstructionString(string _memberName)
		{
			return string.Format
			(
				ApplyInstructionTemplate,
				_memberName,
				UpperFirstChar(_memberName)
			);
		}

		// Format string:
		// 0: member name, starting with upper char
		// 1: member name
		private const string PresetInstructionTemplate =
			"			result.{0} = SpecificMonoBehaviour.{1};\n";
		private string GetPresetInstructionString(string _memberName)
		{
			return string.Format
			(
				PresetInstructionTemplate,
				UpperFirstChar(_memberName),
				_memberName
			);
		}

		// Format string:
		// 0: Namespace
		// 1: Prefix
		// 2: Short type name
		// 3: Qualified type name
		// 4: Apply instructions (starting with 3 tabs)
		// 5: Preset instructions (starting with 3 tabs)
		private const string ApplyStyleTemplate =
			GeneratedWarningComment
			+ "using UnityEngine;\n"
			+ "using GuiToolkit.Style;\n"
			+ "\n"
			+ "namespace {0}\n"
			+ "{{"
			+ "	[ExecuteAlways]\n"
			+ "	public class UiApplyStyle{1}{2} : UiAbstractApplyStyle<{3}, UiStyle{1}{2}>\n"
			+ "	{{\n"
			+ "		public override void Apply()\n"
			+ "		{{\n"
			+ "			if (!SpecificMonoBehaviour || SpecificStyle == null)\n" 
			+ "				return;\n"
			+ "\n"
			+ "{4}"
			+ "		}}\n"
			+ "\n"
			+ "		public override UiAbstractStyleBase CreateStyle(string _name)\n"
			+ "		{{\n"
			+ "			UiStyle{1}{2} result = new UiStyle{1}{2}();\n"
			+ "\n"
			+ "			if (!SpecificMonoBehaviour)\n"
			+ "				return result;\n"
			+ "\n"
			+ "			result.Name = _name;\n"
			+ "{5}\n"
			+ "			return result;\n"
			+ "		}}\n"
			+ "	}}\n"
			+ "}}\n";
		private string GetApplyStyleString(string _namespace, string _prefix, string _typeName, string _qualifiedTypeName, string _applyInstructions, string _presetInstructions)
		{
			return string.Format
			(
				ApplyStyleTemplate,
				_namespace,
				_prefix,
				_typeName,
				_qualifiedTypeName,
				_applyInstructions,
				_presetInstructions
			);
		}
		#endregion

		#region Drawing
		private void OnGUI()
		{
			if (m_monoBehaviour == null)
			{
				EditorGUILayout.HelpBox("Drag a mono behaviour into this field to generate", MessageType.Info);
			}

			m_monoBehaviour = EditorGUILayout.ObjectField(m_monoBehaviour, typeof(MonoBehaviour), true) as MonoBehaviour;

			if (!m_monoBehaviour)
				return;

			m_alternatingRowStyles[0] = GUIStyle.none;
			m_alternatingRowStyles[1] = new GUIStyle();
			m_alternatingRowStyles[1].normal.background = MakeTex(1, 1, new Color(1.0f, 1.0f, 1.0f, 0.15f));

			bool isInternal = UiToolkitConfiguration.Instance.IsEditingInternal;

			InitIfNecessary(isInternal);
			DrawHeader();
			DrawProperties();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button($"Show Hidden ({NumHidden})"))
				ShowHidden();

			if (isInternal)
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

		private void DrawHeader()
		{
			EditorGUILayout.BeginHorizontal();

			List<string> typeNames = new();
			List<Type> types = new();
			for (var type = m_monoBehaviour.GetType(); type != typeof(MonoBehaviour); type = type.BaseType)
			{
				types.Add(type);
				typeNames.Add(type.FullName);
			}

			int idx = EditorUiUtility.StringPopup("Type", typeNames, m_monoBehaviourType.FullName,
				out string newSelection);

			if (idx != -1)
			{
				m_monoBehaviourType = types[idx];
				CollectProperties();
			}

			EditorGUILayout.LabelField("Namespace:", GUILayout.Width(75));
			m_namespace = EditorGUILayout.TextField(m_namespace, GUILayout.Width(200));
			EditorGUILayout.Space(1);
			EditorGUILayout.LabelField("Prefix:", GUILayout.Width(40));
			m_prefix = EditorGUILayout.TextField(m_prefix, GUILayout.Width(200));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(5);
		}

		private void DrawProperties()
		{
			DrawPropertyHeader();
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

		private void DrawPropertyHeader()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.Space(5, false);
			EditorGUILayout.LabelField("Used", EditorStyles.boldLabel, GUILayout.Width(45));
			EditorGUILayout.LabelField("Property", EditorStyles.boldLabel, GUILayout.Width(200));
			EditorGUILayout.LabelField($"Type", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
			EditorUiUtility.HorizontalLine(new Color(0, 0, 0, 0.5f));
			EditorGUILayout.Space(2);
		}

		private void DrawProperty(PropertyRecord _propertyRecord, GUIStyle _guiStyle)
		{
			EditorGUILayout.BeginHorizontal(_guiStyle);
			EditorGUILayout.Space(10, false);
			_propertyRecord.Used = GUILayout.Toggle(_propertyRecord.Used, "", GUILayout.Width(40));
			EditorGUILayout.LabelField($"{_propertyRecord.Name}", GUILayout.Width(200));
			EditorGUILayout.LabelField($"({_propertyRecord.QualifiedTypeName})", GUILayout.ExpandWidth(true));
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
			var jsonClass = new PropertyRecordsJson()
			{
				Version = JsonVersion,
				Namespace = m_namespace,
				Prefix = m_prefix,
				Records = m_PropertyRecords.ToArray(),
			};

			string path = _internal ?
				UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir + $"Type-Json/{m_monoBehaviourType.FullName}.json" :
				UiToolkitConfiguration.Instance.GeneratedAssetsDir + $"{m_monoBehaviourType.FullName}.json";

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
			string path = UiToolkitConfiguration.Instance.GeneratedAssetsDir + $"{m_monoBehaviourType.FullName}.json";
			if (TryReadJson(path))
				return true;

			string pathInternal = UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir +
			                      $"Type-Json/{m_monoBehaviourType.FullName}.json";
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
				if (propertyRecordsJson.Version < JsonVersion)
					return false;

				m_namespace = propertyRecordsJson.Namespace;
				m_prefix = propertyRecordsJson.Prefix;
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
		private void InitIfNecessary(bool _internal)
		{
			if (m_lastMonoBehaviour == m_monoBehaviour && m_PropertyRecords.Count > 0)
				return;

			m_lastMonoBehaviour = m_monoBehaviour;
			m_monoBehaviourType = m_monoBehaviour.GetType();
			m_PropertyRecords.Clear();

			if (FindJson())
				return;

			m_namespace = _internal ? "GuiToolkit.Style" : string.Empty;
			m_prefix = string.Empty;

			CollectProperties();
		}

		private void CollectProperties()
		{
			m_PropertyRecords.Clear();
			var propertyInfos = m_monoBehaviourType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var propertyInfo in propertyInfos)
			{
				if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
					continue;

				m_PropertyRecords.Add(new PropertyRecord()
				{
					Used = !s_filteredNames.Contains(propertyInfo.Name),
					Name = propertyInfo.Name,
					QualifiedTypeName = propertyInfo.PropertyType.FullName.Replace("+", "."),
					TypeName = propertyInfo.PropertyType.Name
				});
			}
		}

		#endregion

		#region Generation
		private void Generate(bool _internal)
		{
			string styleClassContent = GenerateStyleClass();
			if (string.IsNullOrEmpty(styleClassContent))
				return;

			string applicationClassContent = GenerateApplicationClass();
			
			string dir = _internal ?
				UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir :
				UiToolkitConfiguration.Instance.GeneratedAssetsDir;

			string classPrefix = m_prefix;
			string shortTypeName = m_monoBehaviourType.Name;

			string styleClassPath = $"{dir}UiStyle{classPrefix}{shortTypeName}.cs";
			string applicationClassPath = $"{dir}UiApplyStyle{classPrefix}{shortTypeName}.cs";

			try
			{
				File.WriteAllText(styleClassPath, styleClassContent);
				File.WriteAllText(applicationClassPath, applicationClassContent);
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not write class(es), reason:{e.Message}");
				return;
			}

			AssetDatabase.ImportAsset(EditorFileUtility.GetUnityPath(styleClassPath), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			AssetDatabase.ImportAsset(EditorFileUtility.GetUnityPath(applicationClassPath), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
			WriteJson(_internal);
		}

		private string GenerateStyleClass()
		{
			string members = string.Empty;
			string properties = string.Empty;
			bool foundSome = false;

			foreach (var propertyRecord in m_PropertyRecords)
			{
				if (!propertyRecord.Used)
					continue;

				string qualifiedPropertyType = propertyRecord.QualifiedTypeName;
				string memberName = propertyRecord.Name;

				members += GetStyleMemberString(qualifiedPropertyType, memberName);
				properties += GetStylePropertyString(qualifiedPropertyType, memberName, memberName);
				foundSome = true;
			}

			if (!foundSome)
			{
				EditorUtility.DisplayDialog(
					"No Properties defined",
					"The class you'd like to generate has no properties.\n" + 
					"Try to switch on some properties with the 'Show Hidden' button.", 
					"Ok");
				return null;
			}

			string namespaceStr = m_namespace;
			string classPrefix = m_prefix;
			string shortTypeName = m_monoBehaviourType.Name;
			string qualifiedTypeName = m_monoBehaviourType.FullName;

			return GetStyleString(namespaceStr, classPrefix, shortTypeName, qualifiedTypeName,
				members, properties);
		}

		private string GenerateApplicationClass()
		{
			string applyInstructions = string.Empty;
			string presetInstructions = string.Empty;

			foreach (var propertyRecord in m_PropertyRecords)
			{
				if (!propertyRecord.Used)
					continue;

				string qualifiedPropertyType = propertyRecord.QualifiedTypeName;
				string memberName = propertyRecord.Name;

				applyInstructions += GetApplyInstructionString(memberName);
				presetInstructions += GetPresetInstructionString(memberName);
			}

			string namespaceStr = m_namespace;
			string classPrefix = m_prefix;
			string shortTypeName = m_monoBehaviourType.Name;
			string qualifiedTypeName = m_monoBehaviourType.FullName;

			return GetApplyStyleString(namespaceStr, classPrefix, shortTypeName, qualifiedTypeName,
				applyInstructions, presetInstructions);
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