#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GuiToolkit
{
	// Caution, WIP!

	
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
		public static string GetTarget( string _sourcePath ) => Get(_sourcePath, true);

		/// <summary>Return the logical Unity path that points to the given physical directory.
		/// If the directory is not referenced by a symlink, the input value is returned unchanged.</summary>
		public static string GetSource( string _targetPath ) => Get(_targetPath, false);

		private static string Get( string _path, bool _symlinkToTarget )
		{
			if (string.IsNullOrEmpty(_path))
				return _path;

			FileAttributes attr;
			try
			{
				attr = File.GetAttributes(_path);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"Exception: {e.Message}");
				return _path;
			}

			bool isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;
			_path = isDirectory ? _path.NormalizedDirectoryPath() : _path.NormalizedFilePath();

			InitIfNecessary();

			var hit = s_symlinks.FirstOrDefault(t =>
				_path.StartsWith(_symlinkToTarget ? t.symlink : t.target, StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrEmpty(hit.symlink))
				return _path;

			if (!EditorFileUtility.AssertNormalizedDirectoryPath(hit.target) || !EditorFileUtility.AssertNormalizedDirectoryPath(hit.symlink))
				return _path;

			if (_symlinkToTarget)
				return hit.target + _path.Substring(hit.symlink.Length);

			return hit.symlink + _path.Substring(hit.target.Length);
		}

		/// <summary>Clears the internal cache, forcing a rescan on next access.</summary>
		public static void Clear() => s_symlinks = null;
		
		public static string DumpSymlinks()
		{
			InitIfNecessary();
			string result = "Symlinks in project:\n";
			foreach (var valueTuple in s_symlinks)
				result += $"{valueTuple.symlink} -> {valueTuple.target}\n";
			
			return result;
		}

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

			var symlinkByGuid = new Dictionary<string, string>(linkDirs.Length);
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
						symlinkByGuid[guid] = di.FullName.NormalizedDirectoryPath();
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
						if (symlinkByGuid.TryGetValue(guid, out string symlink))
						{
							string target = Path.GetDirectoryName(file)!.NormalizedDirectoryPath();
							results.Add((symlink, target));
						}
					}
				}
				catch (UnauthorizedAccessException) { }
				catch (IOException ioex) when (ioex.HResult == unchecked((int)0x80070005)) { }
			}
			finally
			{
				// cleanup temp files
				foreach (var kvp in symlinkByGuid)
				{
					string tmp = Path.Combine(kvp.Value, TempPrefix + kvp.Key + TempExt);
					try { File.Delete(tmp); } catch { }
				}
			}

			s_symlinks = results;
		}

		private static string FindRepoRoot( string _startDir )
		{
			var dir = new DirectoryInfo(_startDir);
			while (dir != null)
			{
				if (dir.GetDirectories(".git").Any() || dir.GetFiles("package.json").Any())
					return dir.FullName.NormalizedDirectoryPath();
				dir = dir.Parent;
			}
			return null;
		}

		private static bool IsInsideAnotherSymlink( string _path, string _projectRoot )
		{
			var di = new DirectoryInfo(Path.GetDirectoryName(_path)!);
			while (di != null && di.FullName.Length >= _projectRoot.Length)
			{
				if ((di.Attributes & FileAttributes.ReparsePoint) != 0) return true;
				di = di.Parent;
			}
			return false;
		}
	}
}

#endif
