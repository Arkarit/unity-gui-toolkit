#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

namespace GuiToolkit.Style.Editor
{
	[EditorAware]
	public class UiApplyStyleGenerator : EditorWindow
	{
		private const string GeneratedWarningComment = "// Auto-generated, please do not change!\n";

		private Component m_component;
		private Component m_lastComponent;
		private Type m_componentType;
		private List<PropertyRecord> m_PropertyRecords = new();
		private Vector2 m_ScrollPos;

		private GUIStyle[] m_alternatingRowStyles = new GUIStyle[2];
		private static readonly HashSet<string> s_filteredNames = new() { "name", "tag", "hideFlags", "runInEditMode", "useGUILayout" };
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
		// 0: short property type name, starting with upper char
		// 1: qualified property type name
		private const string ClassDefinitionTemplate =
			"		private class ApplicableValue{0} : ApplicableValue<{1}> {{}}\n";

		private string GetMemberClassDefinitionString(string _shortPropertyName, string _qualifiedPropertyTypeName, out string _className)
		{
			_shortPropertyName = UpperFirstChar(_shortPropertyName).Replace("[]", "Array");
			_className = string.Format("ApplicableValue{0}", _shortPropertyName);
			return string.Format
			(
				ClassDefinitionTemplate,
				_shortPropertyName,
				_qualifiedPropertyTypeName
			);
		}

		// Format string:
		// 0: member class name as got from GetMemberClassDefinitionString()
		// 1: member name
		private const string StyleMemberTemplate =
			"		[SerializeReference] private {0} m_{1} = new();\n";
		private string GetStyleMemberString(string _classTypeName, string _memberName)
		{
			return string.Format
			(
				StyleMemberTemplate, 
				_classTypeName, 
				_memberName
			);
		}

		// Format string:
		// 0: qualified property type name
		// 1: short property type name
		// 2: member name
		private const string StylePropertyTemplate =
			"		public ApplicableValue<{0}> {1}\n" +
			"		{{\n" +
			"			get\n" +
			"			{{\n" +
			"				#if UNITY_EDITOR\n" +
			"					if (!Application.isPlaying && m_{2} == null)\n" +
			"						m_{2} = new {3}();\n" +
			"				#endif\n" +
			"				return m_{2};\n" +
			"			}}\n" +
			"		}}\n\n";

		private string GetStylePropertyString(string _qualifiedPropertyTypeName, string _shortPropertyName, string _memberName, string _memberClassName)
		{
			_shortPropertyName = UpperFirstChar(_shortPropertyName);
			return string.Format
			(
				StylePropertyTemplate, 
				_qualifiedPropertyTypeName, 
				_shortPropertyName, 
				_memberName,
				_memberClassName
			);
		}

		// Format string:
		// 0: member name
		private const string GetterTemplate =
			"				{0},\n";

		private string GetGetterString(string _getterName)
		{
			_getterName = UpperFirstChar(_getterName);
			return string.Format
			(
				GetterTemplate,
				_getterName
			);
		}

		// Format string:
		// 0: namespace opening string
		// 1: prefix
		// 2: short type name,
		// 3: qualified type name
		// 4: members (starting with 2 tabs)
		// 5: properties (starting with 2 tabs)
		// 6: Class type definitions
		// 7: member names list (starting with 4 tabs, each member ending with ',')
		// 8: namespace closing string
		private const string StyleTemplate =
			GeneratedWarningComment
			+ "using System;\n"
			+ "using UnityEngine;\n"
			+ "using GuiToolkit;\n"
			+ "using GuiToolkit.Style;\n"
			+ "\n"
			+ "{0}"
			+ "	[Serializable]\n"
			+ "	public class UiStyle{1}{2} : UiAbstractStyle<{3}>\n"
			+ "	{{\n"
			+ "		public UiStyle{1}{2}(UiStyleConfig _styleConfig, string _name)\n"
			+ "		{{\n"
			+ "			StyleConfig = _styleConfig;\n"
			+ "			Name = _name;\n"
			+ "		}}\n"
			+ "\n"
			+ "{6}"
			+ "\n"
			+ "		protected override ApplicableValueBase[] GetValueList()\n"
			+ "		{{\n"
			+ "			return new ApplicableValueBase[]\n"
			+ "			{{\n"
			+ "{7}"
			+ "			}};\n"
			+ "		}}\n"
			+ "\n"
			+ "{4}"
			+ "\n"
			+ "{5}"
			+ "	}}\n"
			+ "{8}";

		private string GetStyleString(
			string _namespace, 
			string _prefix, 
			string _shortTypeName, 
			string _qualifiedTypeName, 
			string _members, 
			string _properties, 
			string _classTypeDefinitions, 
			string _getters)
		{
			return string.Format
			(
				StyleTemplate,
				GetNamespaceOpeningString(_namespace),
				_prefix,
				_shortTypeName,
				_qualifiedTypeName,
				_members,
				_properties,
				_classTypeDefinitions,
				_getters,
				GetNamespaceClosingString(_namespace)
			);
		}

		#endregion

		#region Apply class Templates
		// Format string:
		// 0: member name
		// 1: member name, starting with upper char
		private const string ApplyInstructionTemplate =
			"			if (SpecificStyle.{1}.IsApplicable)\n" +
			"				try {{ SpecificComponent.{0} = Tweenable ? SpecificStyle.{1}.Value : SpecificStyle.{1}.RawValue; }} catch {{}}\n";


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
		// 0: member name
		// 1: member name, starting with upper char
		private const string RecordInstructionTemplate =
			"			if (SpecificStyle.{1}.IsApplicable)\n" +
			"				try {{ SpecificStyle.{1}.RawValue = SpecificComponent.{0}; }} catch {{}}\n";


		private string GetRecordInstructionString(string _memberName)
		{
			return string.Format
			(
				RecordInstructionTemplate,
				_memberName,
				UpperFirstChar(_memberName)
			);
		}

		// Format string:
		// 0: member name, starting with upper char
		// 1: member name
		private const string PresetInstructionTemplate =
			"			try {{ result.{0}.Value = SpecificComponent.{1}; }} catch {{}}\n";
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
		// 0: member name, starting with upper char
		private const string MemberCopyInstructionsTemplate =
			  "				result.{0}.Value = specificTemplate.{0}.Value;\n" 
			+ "				result.{0}.IsApplicable = specificTemplate.{0}.IsApplicable;\n";
		private string GetMemberCopyInstructionsString(string _memberName)
		{
			return string.Format
			(
				MemberCopyInstructionsTemplate,
				UpperFirstChar(_memberName)
			);
		}


		// Format string:
		// 0: Namespace string
		// 1: Prefix
		// 2: Short type name
		// 3: Qualified type name
		// 4: Apply instructions (starting with 3 tabs)
		// 5: Preset instructions (starting with 3 tabs)
		// 6: Member copy instructions (starting with 4 tabs)
		// 7: Record instructions (starting with 3 tabs)
		// 8: Namespace string closing bracket
		private const string ApplyStyleTemplate =
			GeneratedWarningComment
			+ "using UnityEngine;\n"
			+ "using GuiToolkit;\n"
			+ "using GuiToolkit.Style;\n"
			+ "\n"
			+ "{0}"
			+ "	[ExecuteAlways]\n"
			+ "	[RequireComponent(typeof({3}))]\n"
			+ "	public class UiApplyStyle{1}{2} : UiAbstractApplyStyle<{3}, UiStyle{1}{2}>\n"
			+ "	{{\n"
			+ "		protected override void ApplyImpl()\n"
			+ "		{{\n"
			+ "			if (!SpecificComponent || SpecificStyle == null)\n" 
			+ "				return;\n"
			+ "\n"
			+ "{4}"
			+ "		}}\n"
			+ "\n"
			+ "		protected override void RecordImpl()\n"
			+ "		{{\n"
			+ "			if (!SpecificComponent || SpecificStyle == null)\n" 
			+ "				return;\n"
			+ "\n"
			+ "{7}"
			+ "		}}\n"
			+ "\n"
			+ "		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)\n"
			+ "		{{\n"
			+ "			UiStyle{1}{2} result = new UiStyle{1}{2}(_styleConfig, _name);\n"
			+ "\n"
			+ "			if (!SpecificComponent)\n"
			+ "				return result;\n"
			+ "\n"
			+ "			if (_template != null)\n" 
			+ "			{{\n" 
			+ "				var specificTemplate = (UiStyle{1}{2}) _template;\n" 
			+ "\n"
			+ "{6}\n"
			+ "				return result;\n" 
			+ "			}}\n" 
			+ "\n"
			+ "{5}\n"
			+ "			return result;\n"
			+ "		}}\n"
			+ "	}}\n"
			+ "{8}";

		private string GetApplyStyleString
		(
			string _namespace, 
			string _prefix, 
			string _typeName, 
			string _qualifiedTypeName, 
			string _applyInstructions, 
			string _presetInstructions, 
			string _memberCopyInstructions,
			string _recordInstructions
		)
		{
			return string.Format
			(
				ApplyStyleTemplate,
				GetNamespaceOpeningString(_namespace),
				_prefix,
				_typeName,
				_qualifiedTypeName,
				_applyInstructions,
				_presetInstructions,
				_memberCopyInstructions,
				_recordInstructions,
				GetNamespaceClosingString(_namespace)
			);
		}
		#endregion

		#region Drawing

		private void OnGUI()
		{
			if (!AssetReadyGate.Ready(UiToolkitConfiguration.AssetPath))
				GUIUtility.ExitGUI();
			
			if (m_component == null)
			{
				EditorGUILayout.HelpBox("Drag a component into this field to generate", MessageType.Info);
			}

			m_component = EditorGUILayout.ObjectField(m_component, typeof(Component), true) as Component;

			if (!m_component)
				return;

			m_alternatingRowStyles[0] = GUIStyle.none;
			m_alternatingRowStyles[1] = new GUIStyle();
			m_alternatingRowStyles[1].normal.background = MakeTex(1, 1, EditorUiUtility.ColorPerSkin(new Color(1.0f, 1.0f, 1.0f, 0.15f), new Color(1.0f, 1.0f, 1.0f, 0.03f)));

			bool isInternal = EditorGeneralUtility.IsInternal;

			InitIfNecessary(isInternal);
			DrawHeader(isInternal);
			DrawProperties();

			EditorGUILayout.BeginHorizontal();

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

		private void DrawHeader(bool _internal)
		{
			EditorGUILayout.BeginHorizontal();

			List<string> typeNames = new();
			List<Type> types = new();
			for (var type = m_component.GetType(); type != typeof(Component); type = type.BaseType)
			{
				types.Add(type);
				typeNames.Add(type.FullName);
			}

			EditorGUILayout.LabelField("Type:", GUILayout.Width(40));
			int idx = EditorUiUtility.StringPopup(null, typeNames, m_componentType.FullName,
				out string newSelection);

			if (idx != -1)
			{
				m_componentType = types[idx];
				CollectProperties(_internal, false);
			}

			EditorGUILayout.LabelField("Namespace:", GUILayout.Width(75));
			m_namespace = EditorGUILayout.TextField(m_namespace, GUILayout.ExpandWidth(true));
			EditorGUILayout.Space(1);
			EditorGUILayout.LabelField("Prefix:", GUILayout.Width(40));
			m_prefix = EditorGUILayout.TextField(m_prefix, GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(5);
		}

		private void DrawProperties()
		{
			DrawPropertyHeader();
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
			int count = 0;
			foreach (var propertyRecord in m_PropertyRecords)
				DrawProperty(propertyRecord, m_alternatingRowStyles[count++ & 1], propertyRecord.Used);

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
			EditorUiUtility.DrawLine();
			EditorGUILayout.Space(2);
		}

		private void DrawProperty(PropertyRecord _propertyRecord, GUIStyle _guiStyle, bool _isUsed)
		{
			var savedColor = GUI.contentColor;
			var color = savedColor;
			color.a = _isUsed ? 1 : 0.3f;
			GUI.contentColor = color;
			EditorGUILayout.BeginHorizontal(_guiStyle);
			EditorGUILayout.Space(10, false);
			_propertyRecord.Used = GUILayout.Toggle(_propertyRecord.Used, "", GUILayout.Width(40));
			EditorGUILayout.LabelField($"{_propertyRecord.Name}", GUILayout.Width(200));
			EditorGUILayout.LabelField($"({_propertyRecord.QualifiedTypeName})", GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();
			GUI.contentColor = savedColor;
		}
		#endregion

		#region Calculations
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

			string outputDir = _internal ?
				UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir + "Type-Json" :
				UiToolkitConfiguration.Instance.GeneratedAssetsDir;
			EditorFileUtility.EnsureUnityFolderExists(EditorFileUtility.GetUnityPath(outputDir));
			
			string path = outputDir + $"/{m_componentType.FullName}.json";

			
			try
			{
				string json = UnityEngine.JsonUtility.ToJson(jsonClass, true);
				File.WriteAllText(path, json);
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not write Json, reason:'{e.Message}'");
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		}

		private bool TryReadJson(bool _downcastTypes)
		{
			for (var type = m_componentType; type != typeof(Component); type = type.BaseType)
			{
				string path = UiToolkitConfiguration.Instance.GeneratedAssetsDir +
				              $"{type.FullName}.json";
				if (TryReadJson(path))
				{
					m_componentType = type;
					return true;
				}

				string pathInternal = UiToolkitConfiguration.Instance.InternalGeneratedAssetsDir +
				                      $"Type-Json/{type.FullName}.json";

				if (TryReadJson(pathInternal))
				{
					m_componentType = type;
					return true;
				}

				if (!_downcastTypes)
					return false;
			}

			return false;
		}

		private bool TryReadJson(string path)
		{
			try
			{
				var content = File.ReadAllText(path);
				if (string.IsNullOrEmpty(content))
					return false;

				var propertyRecordsJson = UnityEngine.JsonUtility.FromJson<PropertyRecordsJson>(content);
				if (propertyRecordsJson.Version < JsonVersion)
					return false;

				m_namespace = propertyRecordsJson.Namespace;
				m_prefix = propertyRecordsJson.Prefix;
				m_PropertyRecords = propertyRecordsJson.Records.ToList();

				if (!JsonPropertiesMatching())
				{
					m_PropertyRecords.Clear();
					return false;
				}

				m_PropertyRecords.Sort((a,b) => a.Name.CompareTo(b.Name));
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Examine if component has changed in the meantime;
		// Remove unused properties and add newly created properties
		private bool JsonPropertiesMatching()
		{
			Dictionary<string, PropertyRecord> existingPropertyRecords = new();
			foreach (var propertyRecord in m_PropertyRecords)
				existingPropertyRecords.Add(propertyRecord.Name, propertyRecord);

			Dictionary<string, PropertyInfo> existingComponentProperties = new();
			var propertyInfos = m_componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var propertyInfo in propertyInfos)
			{
				if (!propertyInfo.CanRead || !propertyInfo.CanWrite || !AllAccessorsPublic(propertyInfo))
				{
					if (existingPropertyRecords.ContainsKey(propertyInfo.Name))
						return false;

					continue;
				}

				if (!existingPropertyRecords.TryGetValue(propertyInfo.Name, out PropertyRecord record))
					return false;

				if (record.TypeName != propertyInfo.PropertyType.Name)
					return false;

				existingComponentProperties.Add(propertyInfo.Name, propertyInfo);
			}

			foreach (var kv in existingPropertyRecords)
			{
				if (!existingComponentProperties.TryGetValue(kv.Key, out PropertyInfo propertyInfo))
					return false;
				if (propertyInfo.PropertyType.Name != kv.Value.TypeName)
					return false;
			}

			return true;
		}

		#endregion

		#region Data
		private void InitIfNecessary(bool _internal)
		{
			if (m_lastComponent == m_component && m_PropertyRecords.Count > 0 && m_componentType != null)
				return;

			m_lastComponent = m_component;
			m_componentType = m_component.GetType();

			CollectProperties(_internal, true);
		}

		private void CollectProperties(bool _internal, bool _downcastTypes)
		{
			m_PropertyRecords.Clear();
			if (TryReadJson(_downcastTypes))
			{
				return;
			}

			m_namespace = _internal ? "GuiToolkit.Style" : string.Empty;
			m_prefix = string.Empty;

			var propertyInfos = m_componentType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var propertyInfo in propertyInfos)
			{
				if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
					continue;
				
				if (!AllAccessorsPublic(propertyInfo))
					continue;

				m_PropertyRecords.Add(new PropertyRecord()
				{
					Used = !s_filteredNames.Contains(propertyInfo.Name),
					Name = propertyInfo.Name,
					QualifiedTypeName = propertyInfo.PropertyType.FullName.Replace("+", "."),
					TypeName = propertyInfo.PropertyType.Name
				});
			}

			m_PropertyRecords.Sort((a,b) => a.Name.CompareTo(b.Name));
		}

		private static bool AllAccessorsPublic(PropertyInfo propertyInfo)
		{
			string getterName = $"get_{propertyInfo.Name}";
			string setterName = $"set_{propertyInfo.Name}";
			bool foundGetter = false;
			bool foundSetter = false;
			var accessors = propertyInfo.GetAccessors(false);
			foreach (var accessor in accessors)
			{
				if (accessor.Name == getterName)
					foundGetter = true;
				if (accessor.Name == setterName)
					foundSetter = true;

				if (foundGetter && foundSetter)
					break;
			}

			return foundGetter && foundSetter;
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
			string shortTypeName = m_componentType.Name;

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
			WriteJson(_internal);
			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void ValidateConfigs()
		{
			var configs = EditorAssetUtility.FindAllScriptableObjects<UiStyleConfig>(new EditorAssetUtility.AssetSearchOptions() {Folders = new []{"Assets", "Packages"}});
			foreach (var config in configs)
				config.Validate();
		}

		private string GenerateStyleClass()
		{
			string members = string.Empty;
			string properties = string.Empty;
			string classTypeDefinitions = string.Empty;
			string getters = string.Empty;
			bool foundSome = false;

			Dictionary<string, string> typeStringByTypeName = new();

			foreach (var propertyRecord in m_PropertyRecords)
			{
				if (!propertyRecord.Used)
					continue;

				string qualifiedPropertyType = propertyRecord.QualifiedTypeName;
				string shortPropertyType = propertyRecord.TypeName;
				string memberName = propertyRecord.Name;
				string memberClassName;
				string memberClassDefinitionString = GetMemberClassDefinitionString(shortPropertyType, qualifiedPropertyType, out memberClassName);

				if (!typeStringByTypeName.ContainsKey(qualifiedPropertyType))
				{
					classTypeDefinitions += memberClassDefinitionString;
					typeStringByTypeName.Add(qualifiedPropertyType, memberClassName);
				}

				string className = typeStringByTypeName[qualifiedPropertyType];

				members += GetStyleMemberString(className, memberName);
				properties += GetStylePropertyString(qualifiedPropertyType, memberName, memberName, memberClassName);
				getters += GetGetterString(memberName);
				
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
			string shortTypeName = m_componentType.Name;
			string qualifiedTypeName = m_componentType.FullName;

			return GetStyleString(namespaceStr, classPrefix, shortTypeName, qualifiedTypeName,
				members, properties, classTypeDefinitions, getters);
		}

		private string GenerateApplicationClass()
		{
			string applyInstructions = string.Empty;
			string presetInstructions = string.Empty;
			string memberCopyInstructions = string.Empty;
			string recordInstructions = string.Empty;

			foreach (var propertyRecord in m_PropertyRecords)
			{
				if (!propertyRecord.Used)
					continue;

				string qualifiedPropertyType = propertyRecord.QualifiedTypeName;
				string memberName = propertyRecord.Name;

				applyInstructions += GetApplyInstructionString(memberName);
				presetInstructions += GetPresetInstructionString(memberName);
				memberCopyInstructions += GetMemberCopyInstructionsString(memberName);
				recordInstructions += GetRecordInstructionString(memberName);
			}

			string namespaceStr = m_namespace;
			string classPrefix = m_prefix;
			string shortTypeName = m_componentType.Name;
			string qualifiedTypeName = m_componentType.FullName;

			return GetApplyStyleString(namespaceStr, classPrefix, shortTypeName, qualifiedTypeName,
				applyInstructions, presetInstructions, memberCopyInstructions, recordInstructions);
		}

		#endregion

		#region Helper
		
		private static string GetNamespaceOpeningString(string _namespace)
		{
			if (!string.IsNullOrEmpty(_namespace))
				return $"namespace {_namespace}\n{{\n";
			return string.Empty;
		}
		
		private static string GetNamespaceClosingString(string _namespace)
		{
			if (!string.IsNullOrEmpty(_namespace))
				return "}\n";
			return string.Empty;
		}
		
		[MenuItem(StringConstants.APPLY_STYLE_GENERATOR_MENU_NAME)]
		public static UiApplyStyleGenerator GetWindow()
		{
			var window = GetWindow<UiApplyStyleGenerator>();
			window.titleContent = new GUIContent("'UI Apply Style' Generator");
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
