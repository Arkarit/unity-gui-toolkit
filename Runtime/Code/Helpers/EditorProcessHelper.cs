#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// General static helper class for processes
	/// </summary>
	public static class EditorProcessHelper
	{
		/// <summary>
		/// Runs the specified executable with the provided arguments and returns the process' exit code.
		/// </summary>
		/// <param name="output">Recieves the output of either std/err or std/out</param>
		/// <param name="input">Provides the line-by-line input that will be written to std/in, null for empty</param>
		/// <param name="exe">The executable to run, may be unqualified or contain environment variables</param>
		/// <param name="workingFolder">Working folder</param>
		/// <param name="args">The list of unescaped arguments to provide to the executable</param>
		/// <returns>Returns process' exit code after the program exits</returns>
		/// <exception cref="System.IO.FileNotFoundException">Raised when the exe was not found</exception>
		/// <exception cref="System.ArgumentNullException">Raised when one of the arguments is null</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">Raised if an argument contains '\0', '\r', or '\n'
		public static int Run(TextReader input, string exe, string workingFolder, Action<string> output, params string[] args)
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
			psi.WorkingDirectory = workingFolder;
			psi.FileName = FindExecutableByPartialName(exe);
			psi.Arguments = EscapeArguments(args);

			using (Process process = Process.Start(psi))
			using (ManualResetEvent mreOut = new ManualResetEvent(false), mreErr = new ManualResetEvent(false))
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
			//TODO: Mac/Linux implementation

			var baseName = Environment.ExpandEnvironmentVariables(fileName);
			if (!File.Exists(baseName))
			{
				baseName = Path.GetFileNameWithoutExtension(baseName);
				if (Path.GetDirectoryName(baseName) == String.Empty)
				{
					string result = string.Empty;
					if (TryEvaluateEnvironment(fileName, out result, (file, searchedFor) => Path.GetFileName(file) == searchedFor))
						return result;
					if (TryEvaluateEnvironment(fileName, out result, (file, searchedFor) => Path.GetFileNameWithoutExtension(file) == searchedFor))
						return result;
					if (TryEvaluateEnvironment(fileName, out result, (file, searchedFor) => Path.GetFileNameWithoutExtension(file).Contains(searchedFor)))
						return result;
				}

				return null;
			}

			return Path.GetFullPath(baseName);
		}

		private static bool TryEvaluateEnvironment( string _fileName, out string _path, Func<string, string, bool> _fn )
		{
			_path = null;
			string searchPattern = $"*";
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

				// First round: Direct match with extension
				foreach (var file in files)
				{
					if (_fn(file, _fileName))
					{
						path = Path.Combine(path, file);
						_path = Path.GetFullPath(path);
						return true;
					}
				}
			}

			return false;
		}

	}
}
#endif