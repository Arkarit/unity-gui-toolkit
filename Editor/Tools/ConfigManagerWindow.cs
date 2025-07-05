using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using GuiToolkit;

// Caution, WIP!
public class ConfigManagerWindow : EditorWindow
{
    [SerializeField] private List<ScriptableObject> m_configs = new();
    [SerializeField] private ScriptableObject m_selected;

    private Vector2 _listScroll;
    private Editor _editor;

    [MenuItem("Tools/Config Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<ConfigManagerWindow>("Config Manager");
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Configs", EditorStyles.boldLabel);

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                var path = EditorUtility.OpenFilePanel("Select Config", "Assets", "asset").ToLogicalPath();
                if (!string.IsNullOrEmpty(path))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (obj && !m_configs.Contains(obj))
                        m_configs.Add(obj);
                }
            }

            if (GUILayout.Button("Save List", GUILayout.Width(80)))
            {
                EditorUtility.SetDirty(this);
            }
        }

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(150));
        for (int i = 0; i < m_configs.Count; i++)
        {
            var config = m_configs[i];
            if (config == null) continue;

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(config.name, config == m_selected ? EditorStyles.boldLabel : EditorStyles.label))
                {
                    m_selected = config;
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (m_selected == config) m_selected = null;
                    m_configs.RemoveAt(i);
                    i--;
                }
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        if (m_selected)
        {
            EditorGUILayout.LabelField("Config Inspector", EditorStyles.boldLabel);

            if (_editor == null || _editor.target != m_selected)
                _editor = Editor.CreateEditor(m_selected);

            _editor.OnInspectorGUI();
        }
    }

    // Optionale Persistenz der Liste
    private void OnEnable()
    {
        // Hier könnte man die Liste laden, falls du das möchtest
    }

    private void OnDisable()
    {
        // Hier könnte man die Liste speichern
    }
}
