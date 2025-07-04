#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GuiToolkit
{

	public static class SymlinkResolver
	{
		private const string TempPrefix = ".symlink_resolver_";   // Unity ignores ".", no .meta
		private const string TempExt = ".tmp";

		/// <summary>
		/// Scans the project for directory symlinks and returns
		/// (physicalTarget, logicalUnityPath) tuples.
		/// Works even if the link target is *outside* the Unity project folder,
		/// as long as it lives inside the repo (ancestor containing .git or package.json).
		/// </summary>
		public static List<(string physical, string logical)> ResolveAll()
		{
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
									 .Replace('\\', '/');
			string repoRoot = FindRepoRoot(projectRoot) ?? projectRoot;

			// --- collect all directories flagged as ReparsePoint ---
			var linkDirs = Directory.GetDirectories(projectRoot, "*", SearchOption.AllDirectories)
									.Select(d => new DirectoryInfo(d))
									.Where(di => (di.Attributes & FileAttributes.ReparsePoint) != 0)
									.ToArray();

			// Map GUID -> physical path so we can pair them later
			var guidToPhysical = new Dictionary<string, string>(linkDirs.Length);

			try
			{
				// --- step 1: drop temp-files into every symlink dir ---
				foreach (var di in linkDirs)
				{
					string guid = Guid.NewGuid().ToString("N");
					string tempName = TempPrefix + guid + TempExt;
					string tempPath = Path.Combine(di.FullName, tempName);

					try
					{
						// some readonly package installs (or weird ACLs) may block this
						File.WriteAllText(tempPath, di.FullName);   // content irrelevant; we handle this by guid table
						guidToPhysical[guid] = di.FullName.Replace('\\', '/');
					}
					catch (UnauthorizedAccessException uaex)
					{
						Debug.LogWarning(
							$"SymlinkResolver: No write permission in '{di.FullName}'. Skipping. ({uaex.Message})");
						continue;   // ignore link – can’t create marker
					}
					catch (IOException ioex) when (ioex.Message.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
												   ioex.HResult == unchecked((int)0x80070005))  // E_ACCESSDENIED
					{
						Debug.LogWarning(
							$"SymlinkResolver: Write-protected directory '{di.FullName}'. Skipping. ({ioex.Message})");
						continue;
					}
				}
				
				// --- step 2: one single sweep through the *repo* to find those temp files ---
				var results = new List<(string physical, string logical)>();

				foreach (var file in Directory.EnumerateFiles(repoRoot, TempPrefix + "*" + TempExt,
															  SearchOption.AllDirectories))
				{
					if (IsInsideAnotherSymlink(file, projectRoot))
						continue; // ignore temp files that sit in nested symlinks

					string guid = Path.GetFileNameWithoutExtension(file)
									  .Substring(TempPrefix.Length); // strip prefix
					if (guidToPhysical.TryGetValue(guid, out string physical))
					{
						
						
//						// logical directory = directory that *contains* the temp file
//						string logicalDir = Path.GetDirectoryName(file)!.Replace('\\', '/');
//Debug.Log($"--:: Logical:{logicalDir} root:{projectRoot}");						
//						// turn it into project-relative "Assets/…" (projectRoot ends with slash)
//						string logical = logicalDir.Substring(projectRoot.Length + 1);
						results.Add((physical, logical));
					}
				}

				return results;
			}
			finally
			{
				// --- cleanup no matter what ---
				foreach (var kvp in guidToPhysical)
				{
					string tmp = Path.Combine(kvp.Value, TempPrefix + kvp.Key + TempExt);
					if (File.Exists(tmp))
						File.Delete(tmp);
				}
			}
		}

		// ----------------------------------------------------------- helpers
		private static string FindRepoRoot( string startDir )
		{
			var dir = new DirectoryInfo(startDir);
			while (dir != null)
			{
				if (dir.GetDirectories(".git").Any() ||
					dir.GetFiles("package.json").Any())
					return dir.FullName.Replace('\\', '/');
				dir = dir.Parent;
			}
			return null;
		}

		private static bool IsInsideAnotherSymlink( string path, string projectRoot )
		{
			var di = new DirectoryInfo(Path.GetDirectoryName(path)!);
			while (di != null && di.FullName.Length >= projectRoot.Length)
			{
				if ((di.Attributes & FileAttributes.ReparsePoint) != 0)
					return true;
				di = di.Parent;
			}
			return false;
		}
	}
}

#endif