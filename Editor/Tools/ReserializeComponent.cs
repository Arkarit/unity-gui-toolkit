using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public class ReserializeComponent : EditorWindow
	{
		private string m_ClassName = string.Empty;

		private void OnGUI()
		{
			string info = 
				"Use this tool to reserialize assets after class changes.\n" + 
				"It reserializes all assets, which contain a given class or sub class.\n" +
				"This is especially useful if you renamed members in a class.\n\n" + 
				"Process:\n" +
				"- Rename the members and mark them with [FormerlySerializedAs]\n" +
				"- Run this tool\n" + 
				"- Commit all assets, which were changed through the tool\n" +
				"- You can now safely remove the [FormerlySerializedAs]";
			EditorGUILayout.HelpBox(info, MessageType.Info);
			m_ClassName = EditorGUILayout.TextField("Component Class Name", m_ClassName);
			if (GUILayout.Button("Apply"))
				Apply();
		}

		private void Apply()
		{
			List<string> assetsToReserialize = new();
			string[] guids = AssetDatabase.FindAssets("t:prefab");
			Debug.Log($"Found {guids.Length} prefabs overall");

			for (int i=0; i<guids.Length; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
				GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				if (go == null)
					continue;

				var components = go.GetComponentsInChildren<Component>(true);
				for (int j = 0; j<components.Length; j++)
				{
					if (components[j] == null)
					{
						// This may happen on invalid components (e.g. component class deleted)
						continue;
					}
					
					for (Type componentType = components[j].GetType(); 
					     componentType != null && componentType != typeof(Component) && componentType != typeof(TypeInfo); 
					     componentType = componentType.BaseType )
					{
						if (componentType.Name == m_ClassName)
						{
							assetsToReserialize.Add(assetPath);
							goto Found;
						}
					}
				}

				Found: ;
				int numFoundAssets = assetsToReserialize.Count;
				string foundAssetInfo = numFoundAssets == 0
					? "No assets found"
					: $"Asset found: {assetsToReserialize[numFoundAssets-1]}";
				EditorUtility.DisplayProgressBar("Collecting assets", foundAssetInfo, (float) i / guids.Length);
			}

			EditorUtility.ClearProgressBar();

			string s = $"Found {assetsToReserialize.Count} assets which contain the component '{m_ClassName}':\n";
			foreach (var assetPath in assetsToReserialize)
				s += $"\t{assetPath}\n";
			Debug.Log(s);

			AssetDatabase.ForceReserializeAssets(assetsToReserialize);
			AssetDatabase.SaveAssets();
		}

		private const int MenuPriority = -849;

		[MenuItem(StringConstants.MISC_TOOLS_RESERIALIZE_COMPONENT)]
		public static ReserializeComponent GetWindow()
		{
			var window = GetWindow<ReserializeComponent>();
			window.titleContent = new GUIContent("Reserialize component");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
