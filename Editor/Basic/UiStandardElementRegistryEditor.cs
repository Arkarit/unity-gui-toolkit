using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Read-only inspector for the GENERATED <see cref="UiStandardElementRegistry"/>: shows a warning that
	/// the asset is machine-built and draws its contents disabled, so a hand edit (which the next generate
	/// would silently overwrite) is discouraged.
	/// </summary>
	[CustomEditor(typeof(UiStandardElementRegistry))]
	public class UiStandardElementRegistryEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox(
				"Generated asset — do not edit by hand.\n" +
				"It is rebuilt from the UiStandardElement markers on your prefabs by " +
				"'Gui Toolkit → AI → Generate Screen Catalog'. Manual edits are lost on the next generate.",
				MessageType.Warning);

			using (new EditorGUI.DisabledScope(true))
			{
				DrawDefaultInspector();
			}
		}
	}
}
