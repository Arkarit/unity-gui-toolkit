using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Menu front end for the agent-tools installer in <c>Editor/.mcp/install.mjs</c>, which wires this
	/// package's MCP proxy and CLI tooling into a consuming project.
	///
	/// It exists because of one fact a shell script cannot work around: a package pulled from a git URL
	/// lives in <c>Library/PackageCache</c> under a HASHED folder name that is rewritten on every version
	/// bump. Nobody can put that path in a config file, and anything installed into it is lost on the next
	/// bump. Only code running inside the Editor can resolve where the package actually is — which is the
	/// whole reason the entry point sits in <c>Editor/</c> while the payload sits beside it in <c>.mcp/</c>,
	/// a dot folder the asset pipeline ignores (same trick as <c>mcp~</c>).
	///
	/// The real work stays in Node: it is a hard requirement for the proxy anyway, <c>.mcp.json</c> needs a
	/// genuine JSON merge because it holds servers this package knows nothing about, and keeping the logic
	/// there lets the installer also run from a terminal without Unity.
	/// </summary>
	public static class UiAgentToolsInstaller
	{
		private const string MenuRoot = StringConstants.AI_HEADER + "Agent Tools/";
		private const string InstallerRelative = ".mcp/install.mjs";

		[MenuItem(MenuRoot + "Install Into This Project", false, 300)]
		public static void Install()
		{
			if (!TryLocateInstaller(out string installer))
				return;

			bool ok = EditorUtility.DisplayDialog(
				"Install agent tools",
				$"This writes into the project:\n\n" +
				$"  • tools/  (Codex CLI wrappers)\n" +
				$"  • .mcp.json  (merged — existing servers are kept)\n" +
				$"  • .codex/config.toml  (only if absent)\n\n" +
				$"An existing install is refreshed. Everything written is recorded in a manifest so it can " +
				$"be removed again.\n\nInstaller:\n{installer}",
				"Install", "Cancel");

			if (!ok)
				return;

			Run(installer, "install", true);
		}

		[MenuItem(MenuRoot + "Check Status", false, 301)]
		public static void Status()
		{
			if (TryLocateInstaller(out string installer))
				Run(installer, "status", false);
		}

		[MenuItem(MenuRoot + "Uninstall", false, 302)]
		public static void Uninstall()
		{
			if (!TryLocateInstaller(out string installer))
				return;

			bool ok = EditorUtility.DisplayDialog(
				"Uninstall agent tools",
				"Removes only what the manifest recorded. Files you edited since installing are kept.",
				"Uninstall", "Cancel");

			if (ok)
				Run(installer, "uninstall", true);
		}

		/// <summary>
		/// Finds install.mjs by walking up from this script's own asset path.
		///
		/// One rule covers both layouts, because both put the file at <c>&lt;something&gt;/.mcp/install.mjs</c>:
		/// a package in the cache (<c>&lt;pkg&gt;/Editor/AiSupport</c> → <c>&lt;pkg&gt;/Editor/.mcp</c>) and the
		/// library's own dev app, where <c>Editor/</c> is symlinked into Assets and PackageInfo would return
		/// nothing at all. Probing the file system lets the OS resolve the symlink for us.
		/// </summary>
		private static bool TryLocateInstaller(out string installer)
		{
			installer = null;

			string scriptPath = FindThisScriptFolder();
			if (string.IsNullOrEmpty(scriptPath))
			{
				Debug.LogError("[AgentTools] Could not locate this script in the asset database.");
				return false;
			}

			for (DirectoryInfo dir = new DirectoryInfo(scriptPath); dir != null; dir = dir.Parent)
			{
				string candidate = Path.Combine(dir.FullName, InstallerRelative);
				if (File.Exists(candidate))
				{
					installer = candidate.Replace('\\', '/');
					return true;
				}
			}

			Debug.LogError($"[AgentTools] {InstallerRelative} not found above {scriptPath}. " +
				"Is the package incomplete? Dot folders are skipped by some archive exports.");
			return false;
		}

		private static string FindThisScriptFolder()
		{
			string[] guids = AssetDatabase.FindAssets($"{nameof(UiAgentToolsInstaller)} t:MonoScript");
			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (!assetPath.EndsWith($"{nameof(UiAgentToolsInstaller)}.cs", StringComparison.Ordinal))
					continue;

				// Asset paths are project-relative; the project folder is the parent of Assets/.
				string projectFolder = Path.GetDirectoryName(Application.dataPath);
				return Path.GetDirectoryName(Path.GetFullPath(Path.Combine(projectFolder, assetPath)));
			}
			return null;
		}

		private static void Run(string installer, string command, bool refreshAfter)
		{
			// The Unity project folder, passed explicitly for both arguments: --project is walked upward by
			// the installer to find the repo root (a repo may CONTAIN a Unity project rather than be one),
			// while --unity-project spares it a scan that could find several candidates and refuse.
			string unityProject = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');

			var psi = new ProcessStartInfo
			{
				FileName = "node",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = unityProject,
			};
			psi.ArgumentList.Add(installer);
			psi.ArgumentList.Add(command);
			psi.ArgumentList.Add("--project");
			psi.ArgumentList.Add(unityProject);
			psi.ArgumentList.Add("--unity-project");
			psi.ArgumentList.Add(unityProject);

			var stdout = new StringBuilder();
			var stderr = new StringBuilder();

			try
			{
				using (var process = new Process { StartInfo = psi })
				{
					process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
					process.ErrorDataReceived  += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

					process.Start();
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();

					if (!process.WaitForExit(120000))
					{
						process.Kill();
						Debug.LogError("[AgentTools] Installer timed out after 120 s.");
						return;
					}

					string report = stdout.ToString().TrimEnd();
					if (process.ExitCode == 0)
						Debug.Log($"[AgentTools] {command}\n{report}");
					else
						Debug.LogError($"[AgentTools] {command} exited with {process.ExitCode}\n{report}\n{stderr}");
				}
			}
			catch (Exception e)
			{
				// Overwhelmingly the "node is not installed / not on PATH" case, and worth saying so plainly:
				// the proxy needs Node regardless, so this is a prerequisite rather than a bug.
				Debug.LogError($"[AgentTools] Could not run Node ({e.Message}). " +
					"Node 18+ must be installed and on PATH — the MCP proxy needs it too.");
				return;
			}

			if (refreshAfter)
				AssetDatabase.Refresh();
		}
	}
}
