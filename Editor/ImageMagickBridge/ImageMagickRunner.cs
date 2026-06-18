using System;
using System.Collections.Generic;
using System.IO;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// <para>Bridges Unity Editor to an external ImageMagick installation via Process.Start.</para>
	/// <para>
	/// Two usage modes:
	/// <list type="bullet">
	/// <item>Static <see cref="RunSync"/> — blocking call for short operations (single rasterize, version check).</item>
	/// <item>Instance <see cref="RunThreaded"/> — for long operations or when GUI must stay responsive.
	///       Mirrors <see cref="DoxygenRunner"/>.</item>
	/// </list>
	/// </para>
	/// <para>
	/// Executable path resolution: <see cref="ImageMagickConfig.ExecutableOverride"/> takes precedence
	/// when set; otherwise PATH is searched via <see cref="EditorProcessHelper.FindExecutableByPartialName"/>.
	/// The PATH search result is cached (call <see cref="RefreshExecutablePath"/> to invalidate).
	/// </para>
	/// </summary>
	public class ImageMagickRunner
	{
		private const string MagickExecutableName = "magick";

		private static string s_cachedAutoDetectedPath;
		private static bool s_autoDetectedPathCached;

		private readonly string[] m_args;
		private readonly string m_workingFolder;
		private readonly ImageMagickThreadSafeOutput m_output;
		private readonly Action<int> m_onCompleteCallback;
		private readonly List<string> m_fullLog = new();

		/// <summary>
		/// Fully-qualified path to the magick executable, or null if not found.
		/// Override from <see cref="ImageMagickConfig"/> takes precedence over PATH search.
		/// </summary>
		public static string MagickExecutablePath
		{
			get
			{
				var overridePath = GetExecutableOverride();
				if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
					return overridePath;

				if (!s_autoDetectedPathCached)
				{
					s_cachedAutoDetectedPath = EditorProcessHelper.FindExecutableByPartialName(MagickExecutableName, true);
					s_autoDetectedPathCached = true;
				}

				return s_cachedAutoDetectedPath;
			}
		}

		public static bool MagickExecutableFound => !string.IsNullOrEmpty(MagickExecutablePath);

		/// <summary>
		/// Invalidate the cached auto-detected path. Call after the user changes
		/// <see cref="ImageMagickConfig.ExecutableOverride"/> or installs/removes ImageMagick.
		/// </summary>
		public static void RefreshExecutablePath()
		{
			s_cachedAutoDetectedPath = null;
			s_autoDetectedPathCached = false;
		}

		/// <summary>
		/// Blocking call to magick with the given args. Returns the process exit code.
		/// Each line of stdout/stderr is delivered to <paramref name="outputHandler"/>.
		/// </summary>
		/// <exception cref="FileNotFoundException">Raised when magick is not installed or not found.</exception>
		public static int RunSync(string[] args, Action<string> outputHandler, string workingFolder = null)
		{
			if (!MagickExecutableFound)
				throw new FileNotFoundException("ImageMagick executable 'magick' not found.");

			var folder = workingFolder ?? Path.GetDirectoryName(MagickExecutablePath);
			return EditorProcessHelper.Run(null, MagickExecutablePath, folder, outputHandler, args);
		}

		public ImageMagickRunner(string[] args, ImageMagickThreadSafeOutput output, Action<int> callback)
		{
			if (!MagickExecutableFound)
				throw new FileNotFoundException("ImageMagick executable 'magick' not found.");

			m_args = args;
			m_output = output;
			m_onCompleteCallback = callback;
			m_workingFolder = Path.GetDirectoryName(MagickExecutablePath);
		}

		/// <summary>
		/// Run magick on the calling thread. Designed to be invoked from a worker
		/// thread; routes stdout/stderr through the provided <see cref="ImageMagickThreadSafeOutput"/>.
		/// </summary>
		public void RunThreaded()
		{
			Action<string> outputHandler = UpdateOutputString;
			int exitCode = EditorProcessHelper.Run(null, MagickExecutablePath, m_workingFolder, outputHandler, m_args);
			m_output.WriteFullLog(m_fullLog);
			m_output.SetFinished();
			m_onCompleteCallback?.Invoke(exitCode);
		}

		private void UpdateOutputString(string output)
		{
			m_output.WriteLine(output);
			m_fullLog.Add(output);
		}

		private static string GetExecutableOverride()
		{
			try
			{
				if (!ImageMagickConfig.IsInitialized)
					ImageMagickConfig.Initialize();

				return ImageMagickConfig.Instance?.ExecutableOverride;
			}
			catch
			{
				// Singleton not ready (e.g., very early Editor lifecycle); fall back to PATH search.
				return null;
			}
		}
	}
}
