using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Runs Unity Test Runner tests on request of the MCP bridge and keeps the outcome in a file, because a
	/// PlayMode run reloads the domain: everything in memory, the HTTP listener included, is gone by the time the
	/// run finishes. The file under Library/ is the one place that outlives it; <c>testStatus</c> reads it.
	///
	/// Lives in its own assembly, compiled only where com.unity.test-framework is installed (see the asmdef), and
	/// registers itself with the bridge instead of the bridge knowing about it.
	/// </summary>
	[InitializeOnLoad]
	public static class UiTestRunBridge
	{
		private const string StateFile = "Library/UiToolkit/test-run.json";
		private const int MaxOutputChars = 4000;
		private static readonly TimeSpan s_staleAfter = TimeSpan.FromMinutes(20);

		private static readonly Callbacks s_callbacks = new();
		private static readonly object s_lock = new();
		private static JObject s_state;

		static UiTestRunBridge()
		{
			// A worker has no bridge and must not touch the test runner either
			if (Environment.GetCommandLineArgs().Any(_a => _a.IndexOf("AssetImportWorker", StringComparison.OrdinalIgnoreCase) >= 0))
				return;

			UiScreenMcpBridge.RegisterMethod("runTests", StartRun, true);
			UiScreenMcpBridge.RegisterMethod("testStatus", _ => ReadStateText());

			// Registered again after every domain reload: a callback instance does not survive one, and the
			// RunFinished of a PlayMode run arrives in the NEW domain
			CreateApi().RegisterCallbacks(s_callbacks);
		}

		private static TestRunnerApi CreateApi() => UnityEngine.ScriptableObject.CreateInstance<TestRunnerApi>();

		#region Start

		private static string StartRun( string _payload )
		{
			var request = string.IsNullOrWhiteSpace(_payload) ? new JObject() : JObject.Parse(_payload);

			string modeText = (string)request["mode"] ?? "EditMode";
			TestMode mode;
			if (modeText.Equals("PlayMode", StringComparison.OrdinalIgnoreCase))
				mode = TestMode.PlayMode;
			else if (modeText.Equals("EditMode", StringComparison.OrdinalIgnoreCase))
				mode = TestMode.EditMode;
			else
				throw new Exception($"mode must be 'EditMode' or 'PlayMode', not '{modeText}'.");

			// Entering Play Mode with an unsaved scene opens a modal dialog nobody can click; refuse like the
			// other methods that reload the domain
			if (mode == TestMode.PlayMode)
				UiScreenMcpBridge.EnsureReloadSafe("runTests");

			var current = LoadState();
			bool force = (bool?)request["force"] ?? false;
			string currentState = (string)current?["state"];
			if (!force && (currentState == "starting" || currentState == "running")
				&& DateTime.UtcNow - ((DateTime?)current["startedUtc"] ?? DateTime.MinValue).ToUniversalTime() < s_staleAfter)
			{
				throw new Exception($"A test run ('{(string)current["runId"]}', {(string)current["mode"]}) is still in progress. "
					+ "Poll testStatus, or pass force:true if it is known to be dead.");
			}

			var filter = new Filter
			{
				testMode = mode,
				testNames = Strings(request["testNames"]),
				groupNames = Strings(request["groupNames"]),
				categoryNames = Strings(request["categoryNames"]),
				assemblyNames = Strings(request["assemblyNames"]),
			};

			if (filter.testNames == null && filter.groupNames == null && filter.categoryNames == null && filter.assemblyNames == null)
				throw new Exception("Refusing to run EVERYTHING: pass at least one of testNames, groupNames (regex, e.g. 'TestIcon3D'), "
					+ "categoryNames or assemblyNames.");

			string runId = Guid.NewGuid().ToString("N").Substring(0, 12);
			lock (s_lock)
			{
				s_state = new JObject
				{
					["runId"] = runId,
					["mode"] = mode.ToString(),
					["state"] = "starting",
					["startedUtc"] = DateTime.UtcNow,
					["passed"] = 0,
					["failed"] = 0,
					["skipped"] = 0,
					["inconclusive"] = 0,
					["tests"] = new JArray(),
				};
				Save();
			}

			try
			{
				CreateApi().Execute(new ExecutionSettings(filter));
			}
			catch (Exception e)
			{
				Finish("error", e.Message);
				throw;
			}

			return "{\"started\":true,\"runId\":\"" + runId + "\",\"mode\":\"" + mode + "\"}";
		}

		private static string[] Strings( JToken _token )
		{
			string[] list = null;
			if (_token is JArray array)
				list = array.Select(_t => (string)_t).Where(_s => !string.IsNullOrEmpty(_s)).ToArray();
			else if (_token?.Type == JTokenType.String)
				list = new[] { (string)_token };
			return list != null && list.Length > 0 ? list : null;
		}

		#endregion

		#region State file

		private static JObject LoadState()
		{
			lock (s_lock)
			{
				if (s_state != null)
					return s_state;
				try
				{
					if (File.Exists(StateFile))
						s_state = JObject.Parse(File.ReadAllText(StateFile));
				}
				catch
				{
					s_state = null;
				}

				return s_state;
			}
		}

		private static void Save()
		{
			Directory.CreateDirectory(Path.GetDirectoryName(StateFile));
			File.WriteAllText(StateFile, s_state.ToString(Newtonsoft.Json.Formatting.None));
		}

		private static string ReadStateText()
		{
			var state = LoadState();
			if (state == null)
				return "{\"state\":\"none\"}";
			lock (s_lock)
				return state.ToString(Newtonsoft.Json.Formatting.None);
		}

		private static void Finish( string _state, string _error = null )
		{
			lock (s_lock)
			{
				var state = LoadState();
				if (state == null)
					return;
				state["state"] = _state;
				state["finishedUtc"] = DateTime.UtcNow;
				if (_error != null)
					state["error"] = _error;
				Save();
			}
		}

		#endregion

		#region Callbacks

		private class Callbacks : ICallbacks
		{
			public void RunStarted( ITestAdaptor _testsToRun )
			{
				lock (s_lock)
				{
					var state = LoadState();
					if (state == null)
						return;
					state["state"] = "running";
					Save();
				}
			}

			public void TestStarted( ITestAdaptor _test ) { }

			public void TestFinished( ITestResultAdaptor _result )
			{
				if (_result.HasChildren)
					return;

				lock (s_lock)
				{
					var state = LoadState();
					if (state == null)
						return;

					string name = _result.FullName ?? _result.Name;
					var tests = (JArray)state["tests"];
					if (tests.Any(_t => (string)_t["name"] == name))
						return;   // delivered twice (callbacks registered by two domains)

					string output = _result.Output;
					if (output != null && output.Length > MaxOutputChars)
						output = output.Substring(0, MaxOutputChars) + " ...[cut]";

					tests.Add(new JObject
					{
						["name"] = name,
						["status"] = _result.TestStatus.ToString(),
						["durationMs"] = (int)(_result.Duration * 1000),
						["message"] = _result.Message,
						["stackTrace"] = _result.TestStatus == TestStatus.Failed ? _result.StackTrace : null,
						["output"] = string.IsNullOrEmpty(output) ? null : output,
					});

					string key;
					switch (_result.TestStatus)
					{
						case TestStatus.Passed: key = "passed"; break;
						case TestStatus.Failed: key = "failed"; break;
						case TestStatus.Skipped: key = "skipped"; break;
						default: key = "inconclusive"; break;
					}

					state[key] = (int)state[key] + 1;
					Save();
				}
			}

			public void RunFinished( ITestResultAdaptor _result ) => Finish("finished");
		}

		#endregion
	}
}
