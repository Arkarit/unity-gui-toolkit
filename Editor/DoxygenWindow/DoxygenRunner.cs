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
		private DoxygenThreadSafeOutput SafeOutput;
		private Action<int> m_onCompleteCallBack;
		private readonly List<string> DoxyLog = new();
		public string[] Args;
		static string WorkingFolder;

		public static string s_doxygenExecutablePath;

		public static string DoxygenPath
		{
			get
			{
				if (string.IsNullOrEmpty(s_doxygenExecutablePath))
					s_doxygenExecutablePath = FindExecutableByPartialName("doxygen", true);

				return s_doxygenExecutablePath;
			}
		}

		public static bool DoxygenExecutableFound => !string.IsNullOrEmpty(DoxygenPath);

		public DoxygenRunner(string[] args, DoxygenThreadSafeOutput safeoutput, Action<int> callback)
		{
			if (DoxygenPath == null)
				throw new FileNotFoundException("Doxygen Executable not found");

			Args = args;
			SafeOutput = safeoutput;
			m_onCompleteCallBack = callback;
			WorkingFolder = Path.GetDirectoryName(s_doxygenExecutablePath);
		}

		public void updateOutputString(string output)
		{
			SafeOutput.WriteLine(output);
			DoxyLog.Add(output);
		}

		public void RunThreadedDoxy()
		{
			Action<string> GetOutput = (string output) => updateOutputString(output);
			int ReturnCode = Run(GetOutput, null, "doxygen", Args);
			SafeOutput.WriteFullLog(DoxyLog);
			SafeOutput.SetFinished();
			m_onCompleteCallBack(ReturnCode);
		}

		/// <summary>
		/// Runs the specified executable with the provided arguments and returns the process' exit code.
		/// </summary>
		/// <param name="output">Recieves the output of either std/err or std/out</param>
		/// <param name="input">Provides the line-by-line input that will be written to std/in, null for empty</param>
		/// <param name="exe">The executable to run, may be unqualified or contain environment variables</param>
		/// <param name="args">The list of unescaped arguments to provide to the executable</param>
		/// <returns>Returns process' exit code after the program exits</returns>
		/// <exception cref="System.IO.FileNotFoundException">Raised when the exe was not found</exception>
		/// <exception cref="System.ArgumentNullException">Raised when one of the arguments is null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">Raised if an argument contains '\0', '\r', or '\n'
		public static int Run(Action<string> output, TextReader input, string exe, params string[] args)
		{
			if (string.IsNullOrEmpty(exe))
				throw new FileNotFoundException();
			if (output == null)
				throw new ArgumentNullException("output");

			ProcessStartInfo psi = new ProcessStartInfo();
			psi.UseShellExecute = false;
			psi.RedirectStandardError = true;
			psi.RedirectStandardOutput = true;
			psi.RedirectStandardInput = true;
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			psi.CreateNoWindow = true;
			psi.ErrorDialog = false;
			psi.WorkingDirectory = WorkingFolder;
			psi.FileName = FindExecutableByPartialName(exe);
			psi.Arguments = EscapeArguments(args);

			using (Process process = Process.Start(psi))
			using (ManualResetEvent mreOut = new ManualResetEvent(false),
			       mreErr = new ManualResetEvent(false))
			{
				process.OutputDataReceived += (o, e) =>
				{
					if (e.Data == null) mreOut.Set();
					else output(e.Data);
				};
				process.BeginOutputReadLine();
				process.ErrorDataReceived += (o, e) =>
				{
					if (e.Data == null) mreErr.Set();
					else output(e.Data);
				};
				process.BeginErrorReadLine();

				string line;
				while (input != null && null != (line = input.ReadLine()))
					process.StandardInput.WriteLine(line);

				process.StandardInput.Close();
				process.WaitForExit();

				mreOut.WaitOne();
				mreErr.WaitOne();
				return process.ExitCode;
			}
		}

		/// <summary>
		/// Quotes all arguments that contain whitespace, or begin with a quote and returns a single
		/// argument string for use with Process.Start().
		/// </summary>
		/// <param name="args">A list of strings for arguments, may not contain null, '\0', '\r', or '\n'</param>
		/// <returns>The combined list of escaped/quoted strings</returns>
		/// <exception cref="System.ArgumentNullException">Raised when one of the arguments is null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">Raised if an argument contains '\0', '\r', or '\n'</exception>
		public static string EscapeArguments(params string[] args)
		{
			StringBuilder arguments = new StringBuilder();
			Regex invalidChar = new Regex("[\x00\x0a\x0d]"); //  these can not be escaped
			Regex needsQuotes = new Regex(@"\s|"""); //          contains whitespace or two quote characters
			Regex escapeQuote = new Regex(@"(\\*)(""|$)"); //    one or more '\' followed with a quote or end of string
			for (int carg = 0; args != null && carg < args.Length; carg++)
			{
				if (args[carg] == null)
				{
					throw new ArgumentNullException("args[" + carg + "]");
				}

				if (invalidChar.IsMatch(args[carg]))
				{
					throw new ArgumentOutOfRangeException("args[" + carg + "]");
				}

				if (args[carg] == String.Empty)
				{
					arguments.Append("\"\"");
				}
				else if (!needsQuotes.IsMatch(args[carg]))
				{
					arguments.Append(args[carg]);
				}
				else
				{
					arguments.Append('"');
					arguments.Append(escapeQuote.Replace(args[carg], m =>
						m.Groups[1].Value + m.Groups[1].Value +
						(m.Groups[2].Value == "\"" ? "\\\"" : "")
					));
					arguments.Append('"');
				}

				if (carg + 1 < args.Length)
					arguments.Append(' ');
			}

			return arguments.ToString();
		}


		/// <summary>
		/// Expands environment variables and, if unqualified, locates the exe in the working directory
		/// or the evironment's path.
		/// </summary>
		/// <param name="fileName">The name of the executable file. Can be partial.</param>
		/// <param name="caseInsensitive"></param>
		/// <returns>The fully-qualified path to the file or null if not found</returns>
		public static string FindExecutableByPartialName(string fileName, bool caseInsensitive = false)
		{
			return caseInsensitive ? 
				FindExecutableByPartialNameInternal(fileName.ToLower(), true) :
				FindExecutableByPartialNameInternal(fileName, false);
		}

		private static string FindExecutableByPartialNameInternal(string fileName, bool caseInsensitive)
		{
			fileName = Environment.ExpandEnvironmentVariables(fileName);
			if (!File.Exists(fileName))
			{
				fileName = Path.GetFileNameWithoutExtension(fileName);
				if (Path.GetDirectoryName(fileName) == String.Empty)
				{
					string searchPattern = $"*{fileName}*";
					foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
					{
						string path = test.Trim();
						string[] files;

						try
						{
							files = Directory.GetFiles(path, searchPattern);
						}
						catch
						{
							continue;
						}

						// First round: Direct match
						foreach (var file in files)
						{
							if (Path.GetFileNameWithoutExtension(file) == fileName)
							{
								path = Path.Combine(path, file);
								return Path.GetFullPath(path);
							}
						}

						// Second round: partial match
						foreach (var file in files)
						{
							if (Path.GetFileNameWithoutExtension(file).Contains(fileName))
							{
								path = Path.Combine(path, file);
								return Path.GetFullPath(path);
							}
						}
					}
				}

				return null;
			}

			return Path.GetFullPath(fileName);
		}
	}
}
