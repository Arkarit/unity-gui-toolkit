using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Writes the result of every test run to Temp/last-test-run.txt, one line per test plus a summary.
	///
	/// Exists because a run cannot be watched from outside the editor. Play mode tests reload the domain on
	/// the way in, which drops any callback registered ad hoc for a single run - so the listener has to be
	/// something that comes back by itself, which is what InitializeOnLoad buys. It also means a run
	/// started by hand in the Test Runner window leaves the same trace, which is the more useful half.
	///
	/// Temp/ is Unity's own scratch folder: not in the project, not in version control, wiped with the
	/// Library. Nothing here is an asset.
	/// </summary>
	[InitializeOnLoad]
	public static class TestRunReporter
	{
		public const string ResultFile = "Temp/last-test-run.txt";

		private class Sink : ICallbacks
		{
			private StringBuilder m_sb;

			public void RunStarted( ITestAdaptor _testsToRun )
			{
				m_sb = new StringBuilder();
			}

			public void TestStarted( ITestAdaptor _test ) { }

			public void TestFinished( ITestResultAdaptor _result )
			{
				// Suites report too, and their result is only the roll-up of the leaves below them.
				if (_result.HasChildren || m_sb == null)
					return;

				var message = (_result.Message ?? "").Replace("\r", " ").Replace("\n", " ");
				m_sb.AppendLine($"{_result.TestStatus}\t{_result.Test.FullName}\t{message}");
			}

			public void RunFinished( ITestResultAdaptor _result )
			{
				if (m_sb == null)
					return;

				m_sb.AppendLine($"DONE passed={_result.PassCount} failed={_result.FailCount} "
					+ $"skipped={_result.SkipCount} inconclusive={_result.InconclusiveCount}");

				File.WriteAllText(ResultFile, m_sb.ToString());
				m_sb = null;
			}
		}

		private static TestRunnerApi s_api;

		static TestRunReporter()
		{
			s_api = ScriptableObject.CreateInstance<TestRunnerApi>();
			s_api.RegisterCallbacks(new Sink());
		}

		/// <summary>
		/// Starts an edit mode run without the Test Runner window, so a run can be triggered from outside
		/// the editor - the result lands in the same file as always.
		///
		/// The window is not needed to run tests, only to watch them, and clicking it is the one step in
		/// "change code, compile, test" that could not be automated. Menu items can be executed from
		/// outside; the API instance behind this cannot be built there, which is why the trigger lives
		/// next to the listener that already owns one.
		/// </summary>
		[MenuItem("Gui Toolkit/Tests/Run Edit Mode Tests")]
		public static void RunEditModeTests()
		{
			s_api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
		}
	}
}
