#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Resolves directory symlinks that sit *inside* the Unity project (logical "Assets/...") but
	/// point to folders *outside* the project – the setup we use for GuiToolkit’s Runtime/Editor sources.
	/// </summary>
	/// <remarks>
	/// Implementation detail (aka the MacGyver-Hack):
	///
	/// 1.  Scan all directories below the Unity project root.
	///     Whenever we detect a symlink (FileAttributes.ReparsePoint), we drop a uniquely named temp file
	///     (.symlink_resolver_<guid>.tmp) into that directory.
	///
	/// 2.  Starting from the repo root (first ancestor that contains .git or package.json) we search for
	///     these temp files *excluding* any path that itself lies inside another symlink.
	///     The folder Directory.GetDirectoryName(file) gives us the logical Unity path; the directory in which
	///     we created the temp file is the physical target.
	///
	/// We end up with a mapping list (symlink, target) which we cache lazily on first access.
	///
	/// Why so dirty? A proper cross-platform API would be DirectoryInfo.LinkTarget, but that is absent in
	/// Unity 6. Earlier Unity versions exposed it only in "experimental" .NET 6 mode; now it’s gone. Hence this
	/// workaround until Unity ships a sane runtime. Once they do: *please delete this abomination.*
	///
	/// *Coined by my AI side-kick Elfie. She’s not wrong.*
	/// </remarks>
	public static class SymlinkResolver
	{
		private const string TempPrefix = ".symlink_resolver_";   // Unity ignores '.', no .meta
		private const string TempExt = ".tmp";

		private static List<(string symlink, string target)> s_symlinks; // cached mapping

		/// <summary>Return the physical target for a given logical Unity path (Assets/...).
		/// If the path is not a symlink, the input value is returned unchanged.</summary>
		public static string GetTarget( string _symlinkPath )
		{
			if (string.IsNullOrEmpty(_symlinkPath))
				return _symlinkPath;

			InitIfNecessary();
			var hit = s_symlinks.FirstOrDefault(t => t.symlink.Equals(_symlinkPath, StringComparison.OrdinalIgnoreCase));
			return string.IsNullOrEmpty(hit.target) ? _symlinkPath : hit.target;
		}

		/// <summary>Return the logical Unity path that points to the given physical directory.
		/// If the directory is not referenced by a symlink, the input value is returned unchanged.</summary>
		public static string GetSource( string targetPath )
		{
			if (string.IsNullOrEmpty(targetPath))
				return targetPath;

			// We don't do a path normalization here except replacing backslashes, since we don't know if a directory or file is requested
			targetPath = Path.GetFullPath(targetPath).Replace('\\', '/');

			InitIfNecessary();

			var hit = s_symlinks.FirstOrDefault(t =>
				targetPath.StartsWith(t.target, StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrEmpty(hit.symlink))
				return targetPath;

			if (!AssertNormalizedDirectoryPath(hit.target) || !AssertNormalizedDirectoryPath(hit.symlink))
				return targetPath;

			return hit.symlink + targetPath.Substring(hit.target.Length);
		}

		/// <summary>Clears the internal cache, forcing a rescan on next access.</summary>
		public static void Clear() => s_symlinks = null;

		// ---------------------------------------------------------------------
		// internal
		// ---------------------------------------------------------------------
		private static void InitIfNecessary()
		{
			if (s_symlinks != null)
				return;

			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).NormalizedDirectoryPath();
			string repoRoot = FindRepoRoot(projectRoot) ?? projectRoot;

			var linkDirs = Directory.GetDirectories(projectRoot, "*", SearchOption.AllDirectories)
									 .Select(d => new DirectoryInfo(d))
									 .Where(di => (di.Attributes & FileAttributes.ReparsePoint) != 0)
									 .ToArray();

			var guidToPhysical = new Dictionary<string, string>(linkDirs.Length);
			var results = new List<(string symlink, string target)>();

			try
			{
				// STEP 1 - drop temp files
				foreach (var di in linkDirs)
				{
					string guid = Guid.NewGuid().ToString("N");
					string tempName = TempPrefix + guid + TempExt;
					string tempPath = Path.Combine(di.FullName, tempName);

					try
					{
						File.WriteAllText(tempPath, di.FullName); // file content only for debugging purposes, not actually used
						guidToPhysical[guid] = di.FullName.Replace('\\', '/');
					}
					catch (UnauthorizedAccessException) { continue; }
					catch (IOException ioex) when (ioex.HResult == unchecked((int)0x80070005)) { continue; }
				}

				// STEP 2 - single sweep through repo to find the temp markers
				try
				{
					foreach (var file in Directory.EnumerateFiles(repoRoot, TempPrefix + "*" + TempExt, SearchOption.AllDirectories))
					{
						if (IsInsideAnotherSymlink(file, projectRoot)) continue;

						string guid = Path.GetFileNameWithoutExtension(file).Substring(TempPrefix.Length);
						if (guidToPhysical.TryGetValue(guid, out string physical))
						{
							string logicalDir = Path.GetDirectoryName(file)!.NormalizedDirectoryPath();
							string logical = logicalDir.Substring(projectRoot.Length + 1); // "Assets/..."
							results.Add((logical, physical));
						}
					}
				}
				catch (UnauthorizedAccessException) { }
				catch (IOException ioex) when (ioex.HResult == unchecked((int)0x80070005)) { }
			}
			finally
			{
				// cleanup temp files
				foreach (var kvp in guidToPhysical)
				{
					string tmp = Path.Combine(kvp.Value, TempPrefix + kvp.Key + TempExt);
					try { File.Delete(tmp); } catch { }
				}
			}

			s_symlinks = results;
		}

		private static string NormalizedDirectoryPath(this string _directory)=> _directory.Replace('\\', '/').TrimEnd('/') + '/';
		
		private static bool AssertNormalizedDirectoryPath(string _directory)
		{
			bool result = !_directory.Contains('\\') && 
			              _directory.EndsWith('/') &&
			              !_directory.EndsWith("//");
			
			Debug.Assert(result, $"Directory Path '{_directory}' is not normalized! (Mustn't contain \\ and must end with single /)");
			
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			return result;
		}
		
		private static string NormalizedFilePath(this string _filePath) => _filePath.Replace('\\', '/').TrimEnd('/');
		
		private static void AssertNormalizedFilePath(string _filePath)
		{
			bool condition = !_filePath.Contains('\\') && 
			                 !_filePath.EndsWith('/');
			
			Debug.Assert(condition, $"File Path '{_filePath}' is not normalized! (Mustn't contain \\ and mustn't end with /)");
		}
		
		// --------------------------------------------------------------------- helpers
		private static string FindRepoRoot( string startDir )
		{
			var dir = new DirectoryInfo(startDir);
			while (dir != null)
			{
				if (dir.GetDirectories(".git").Any() || dir.GetFiles("package.json").Any())
					return dir.FullName.NormalizedDirectoryPath();
				dir = dir.Parent;
			}
			return null;
		}

		private static bool IsInsideAnotherSymlink( string path, string projectRoot )
		{
			var di = new DirectoryInfo(Path.GetDirectoryName(path)!);
			while (di != null && di.FullName.Length >= projectRoot.Length)
			{
				if ((di.Attributes & FileAttributes.ReparsePoint) != 0) return true;
				di = di.Parent;
			}
			return false;
		}
	}
}

#endif
