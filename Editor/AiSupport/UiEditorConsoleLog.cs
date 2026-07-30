using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// A ring buffer of this editor session's console messages, so an external agent can read what Unity said
	/// instead of digging through Editor.log.
	///
	/// Why: Editor.log spans several sessions, mixes in a previous run's compiler errors, and has to be pattern
	/// matched from the outside — which produced two wrong conclusions in one morning (stale "error CS" lines
	/// read as current, and a substring match mistaken for a finding). Messages served from here are always the
	/// running session's, carry their severity, and can be asked for by sequence so "what happened since I
	/// triggered that" is answerable exactly.
	///
	/// A domain reload clears the buffer, being static — that is deliberate rather than worked around: after a
	/// recompile the interesting messages are the new ones anyway.
	/// </summary>
	[InitializeOnLoad]
	public static class UiEditorConsoleLog
	{
		private const int Capacity = 1000;

		public class Entry
		{
			public long sequence;
			public string type;
			public string message;
			public string stackTrace;
			public DateTime timeUtc;
		}

		private static readonly object s_lock = new();
		private static readonly Queue<Entry> s_entries = new();
		private static long s_nextSequence = 1;

		// Subscribing to a log callback is all this does at load time: no assets, no ScriptableObjects, nothing
		// that would need the asset database to be ready.
		static UiEditorConsoleLog()
		{
			Application.logMessageReceivedThreaded -= OnLogMessage;
			Application.logMessageReceivedThreaded += OnLogMessage;
		}

		private static void OnLogMessage( string _message, string _stackTrace, LogType _type )
		{
			lock (s_lock)
			{
				s_entries.Enqueue(new Entry
				{
					sequence = s_nextSequence++,
					type = _type.ToString(),
					message = _message,
					stackTrace = _stackTrace,
					timeUtc = DateTime.UtcNow,
				});

				while (s_entries.Count > Capacity)
					s_entries.Dequeue();
			}
		}

		/// <summary>
		/// Returns the buffered messages, newest last, narrowed by the given filters.
		/// <paramref name="_sinceSequence"/> is the precise way to ask "what happened since I did that": note
		/// <c>nextSequence</c> from an earlier call, act, then pass it back.
		/// </summary>
		public static JObject Query( string _severity, string _contains, long _sinceSequence, int _limit,
			bool _withStackTraces )
		{
			var selected = new List<Entry>();
			long nextSequence;
			int errors = 0, warnings = 0, logs = 0;

			lock (s_lock)
			{
				nextSequence = s_nextSequence;
				foreach (var entry in s_entries)
				{
					switch (entry.type)
					{
						case "Error": case "Exception": case "Assert": errors++; break;
						case "Warning": warnings++; break;
						default: logs++; break;
					}

					if (entry.sequence < _sinceSequence)
						continue;
					if (!MatchesSeverity(entry.type, _severity))
						continue;
					if (!string.IsNullOrEmpty(_contains) &&
					    entry.message.IndexOf(_contains, StringComparison.OrdinalIgnoreCase) < 0)
						continue;

					selected.Add(entry);
				}
			}

			// Newest are the interesting ones when a limit bites, so trim from the front.
			int limit = _limit > 0 ? _limit : 100;
			if (selected.Count > limit)
				selected.RemoveRange(0, selected.Count - limit);

			var messages = new JArray();
			foreach (var entry in selected)
			{
				var item = new JObject
				{
					["sequence"] = entry.sequence,
					["type"] = entry.type,
					["timeUtc"] = entry.timeUtc.ToString("o", CultureInfo.InvariantCulture),
					["message"] = entry.message,
				};
				if (_withStackTraces && !string.IsNullOrEmpty(entry.stackTrace))
					item["stackTrace"] = entry.stackTrace;
				messages.Add(item);
			}

			return new JObject
			{
				["messages"] = messages,
				["returned"] = messages.Count,
				// Pass this back as sinceSequence next time to get only what is new.
				["nextSequence"] = nextSequence,
				["bufferedTotals"] = new JObject
				{
					["errors"] = errors,
					["warnings"] = warnings,
					["logs"] = logs,
				},
				["bufferCapacity"] = Capacity,
			};
		}

		private static bool MatchesSeverity( string _type, string _severity )
		{
			if (string.IsNullOrEmpty(_severity) || _severity == "all")
				return true;

			switch (_severity)
			{
				case "error": return _type is "Error" or "Exception" or "Assert";
				// "warning" means "warning and worse": asking for warnings and being shown no errors would be
				// a trap, not a filter.
				case "warning": return _type is "Warning" or "Error" or "Exception" or "Assert";
				case "log": return true;
				default: return true;
			}
		}
	}
}
