using UnityEditor;
using UnityEngine;


namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(DoxygenConfig), true)]
	public class DoxygenConfigEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			EditorUiUtility.WithHeadline("Doxygen Settings", () => DrawDefaultInspector());

			EditorUiUtility.WithHeadline("Doxygen Tools", () =>
			{
				if (!DoxygenRunner.DoxygenExecutableFound)
				{
					GUILayout.Space(20);
	
					EditorUiUtility.LabelCentered("Tools not available; it seems that Doxygen is not installed.", EditorStyles.boldLabel);
					
					GUILayout.Space(10);
					EditorUiUtility.LabelCentered("DoxygenWindow needs an external Doxygen");
					GUILayout.Space(-4);
					EditorUiUtility.LabelCentered("installation to work. Download it at the link below");
	
					GUILayout.Space(10);
	
					EditorUiUtility.Centered(() =>
					{
						if (GUILayout.Button("   Download Doxygen   ", GUILayout.Height(40)))
						{
							Application.OpenURL("https://www.doxygen.nl/download.html");
						}
					});
	
					GUILayout.Space(10);
	
					EditorUiUtility.LabelCentered("Note: It might be necessary to log in and out");
					GUILayout.Space(-4);
					EditorUiUtility.LabelCentered("of your Operating system, after you installed Doxygen");
					GUILayout.Space(-4);
					EditorUiUtility.LabelCentered("to detect it.");
	
					return;
				}
	
				GUILayout.Space(10);
				if (!DoxygenConfig.Instance.DocsGenerated)
					GUI.enabled = false;

				EditorUiUtility.Centered(() =>
				{
					if (GUILayout.Button("   Browse Documentation   ", GUILayout.Height(40)))
					{
						Application.OpenURL("File://" + DoxygenConfig.Instance.DocumentDirectory.FullPath + "/html/annotated.html");
					}
				});


				GUI.enabled = true;

			});

			

		}
	}
}
