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


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

namespace GuiToolkit.Editor
{
	/// <summary>
	///  This class spawns and runs Doxygen in a separate thread, and could serve as an example of how to create 
	///  plugins for unity that call a command line application and then get the data back into Unity safely.	 
	/// </summary>
	public class DoxygenRunner
	{
		private DoxygenThreadSafeOutput m_output;
		private Action<int> m_onCompleteCallBack;
		private readonly List<string> m_doxyLog = new();
		private string[] m_args;
		private string m_workingFolder;

		private static string s_doxygenExecutablePath;

		private static string DoxygenPath
		{
			get
			{
				if (string.IsNullOrEmpty(s_doxygenExecutablePath))
					s_doxygenExecutablePath = EditorProcessHelper.FindExecutableByPartialName("doxygen", true);

				return s_doxygenExecutablePath;
			}
		}

		public static bool DoxygenExecutableFound => !string.IsNullOrEmpty(DoxygenPath);

		public DoxygenRunner(string[] args, DoxygenThreadSafeOutput safeoutput, Action<int> callback)
		{
			if (DoxygenPath == null)
				throw new FileNotFoundException("Doxygen Executable not found");

			m_args = args;
			m_output = safeoutput;
			m_onCompleteCallBack = callback;
			m_workingFolder = Path.GetDirectoryName(s_doxygenExecutablePath);
		}

		public void RunThreadedDoxygen()
		{
			Action<string> GetOutput = (string output) => UpdateOutputString(output);
			int ReturnCode = EditorProcessHelper.Run(GetOutput, null, "doxygen", m_workingFolder, m_args);
			m_output.WriteFullLog(m_doxyLog);
			m_output.SetFinished();
			m_onCompleteCallBack(ReturnCode);
		}

		private void UpdateOutputString(string output)
		{
			m_output.WriteLine(output);
			m_doxyLog.Add(output);
		}

	}
}
