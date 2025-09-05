/*
Original copyright notice by Jacob Pennock:

/// <summary>
/// <para> A Editor Plugin for automatic doc generation through Doxygen</para>
/// <para> Author: Jacob Pennock (http://Jacobpennock.com)</para>
/// <para> Version: 1.0</para>	 
/// </summary>

Permission is hereby granted, free of charge, to any person  obtaining a copy of this software and associated documentation  files (the "Software"), to deal in the Software without  restriction, including without limitation the rights to use,  copy, modify, merge, publish, distribute, sublicense, and/or sell  copies of the Software, and to permit persons to whom the  Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// <para> An Editor Plugin for automatic doc generation through Doxygen</para>
	/// <para> Original Author: Jacob Pennock (http://Jacobpennock.com)</para>
	/// </summary>
	public class DoxygenWindow : EditorWindow, IEditorAware
	{
		private static DoxygenWindow s_window;

		public static DoxygenWindow Instance => s_window;

		[MenuItem("Window/Documentation with Doxygen")]
		public static void Init()
		{
			s_window = (DoxygenWindow)GetWindow(typeof(DoxygenWindow), false, "Doxygen");
			s_window.minSize = new Vector2(420, 245);
			s_window.maxSize = new Vector2(420, 720);
		}

		void OnGUI()
		{
			if (!AssetReadyGate.Ready(DoxygenConfig.AssetPath))
				GUIUtility.ExitGUI();
			
			EditorDisplayHelper.Draw(DoxygenConfig.Instance, "DoxygenConfig instance is null. Please create one.");
			var editor = EditorDisplayHelper.GetTargetHelperEditor<DoxygenConfigEditor>();
			if (editor && editor.IsDoxygenExeWorking)
				Repaint();
		}
	}
}
