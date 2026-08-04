using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// What the editor currently has OPEN, what it is BUSY with, and whether a given asset can safely be
	/// written or saved. All of that is invisible from outside the editor, and an agent that cannot see it
	/// will eventually rewrite a scene the editor is holding, or fire a second heavy operation into a
	/// running import — both of which leave the editor in a state only a restart fixes.
	///
	/// Two different questions about one asset, deliberately reported separately:
	/// <list type="bullet">
	/// <item><b>safeToWriteFromOutside</b> — may a text tool rewrite this file right now? No if the editor
	/// has it open, because the in-memory copy would win and the write would be silently undone (or worse,
	/// half-merged).</item>
	/// <item><b>savableByUnity</b> — would Unity accept saving it? No if it contains components whose script
	/// cannot be loaded: Unity refuses the save, once per offending GameObject, which is how a single
	/// prefab produces hundreds of console errors.</item>
	/// </list>
	/// </summary>
	internal static class UiEditorState
	{
		/// <summary>How many offending GameObject names to name; enough to act on, not a dump.</summary>
		private const int MaxNamedGameObjects = 5;

		/// <summary>
		/// How long after the last observed compile/import the editor still counts as busy.
		///
		/// This is the whole point of the class. A package resolve is not one continuous busy phase but a
		/// CHAIN — resolve, copy, import batch, compile, domain reload, more imports — and between the links
		/// both isCompiling and isUpdating are briefly false. Sampling once and calling that "idle" is how an
		/// agent declares a resolve finished mid-chain and fires the next operation into it. Every symptom
		/// that cost a session on 2026-08-04 sits downstream of that single wrong sample.
		/// </summary>
		private const double SettleSeconds = 5.0;

		/// <summary>
		/// How long a requested resolve may stay "pending" before we stop claiming it is about to start.
		/// Generous on purpose: on a large project the resolve takes well over ten seconds to produce its
		/// first observable activity, and the old 2s head start declared "no import activity" on a resolve
		/// that demonstrably happened.
		/// </summary>
		private const double ResolveGraceSeconds = 90.0;

		// Kept in SessionState, NOT in statics. The operations this tracks END in a domain reload, and a
		// reload wipes statics — so a static tail is guaranteed to be gone exactly when it would matter.
		// Measured, not assumed: with the tail in statics and a 60s window, status reported idle immediately
		// after a recompile. SessionState survives the reload and is cleared on editor restart, which is
		// precisely the lifetime this state should have.
		private const string KeyBusyWith = "GuiToolkit.UiEditorState.BusyWith";
		private const string KeyBusySince = "GuiToolkit.UiEditorState.BusySinceTicks";
		private const string KeyLastActivity = "GuiToolkit.UiEditorState.LastActivityTicks";
		private const string KeyResolvePending = "GuiToolkit.UiEditorState.ResolvePending";
		private const string KeyResolveSawActivity = "GuiToolkit.UiEditorState.ResolveSawActivity";
		private const string KeyResolveRequested = "GuiToolkit.UiEditorState.ResolveRequestedTicks";

		private static string BusyWith
		{
			get { string v = SessionState.GetString(KeyBusyWith, string.Empty); return v.Length == 0 ? null : v; }
			set => SessionState.SetString(KeyBusyWith, value ?? string.Empty);
		}

		private static bool ResolvePending
		{
			get => SessionState.GetBool(KeyResolvePending, false);
			set => SessionState.SetBool(KeyResolvePending, value);
		}

		private static bool ResolveSawActivity
		{
			get => SessionState.GetBool(KeyResolveSawActivity, false);
			set => SessionState.SetBool(KeyResolveSawActivity, value);
		}

		private static DateTime GetStamp( string _key )
		{
			string raw = SessionState.GetString(_key, string.Empty);
			return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ticks)
				? new DateTime(ticks, DateTimeKind.Utc)
				: default;
		}

		private static void SetStamp( string _key, DateTime _value )
		{
			SessionState.SetString(_key, _value == default
				? string.Empty
				: _value.Ticks.ToString(CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Told by the bridge when a package resolve was triggered. Until the editor has actually been seen
		/// working and settling afterwards, callers are told to keep waiting — otherwise the quiet moment
		/// BEFORE the resolve starts is indistinguishable from the quiet AFTER it finished.
		/// </summary>
		internal static void MarkResolveRequested()
		{
			ResolvePending = true;
			ResolveSawActivity = false;
			SetStamp(KeyResolveRequested, DateTime.UtcNow);
		}

		/// <summary>
		/// Called from the bridge's update pump. Tracking the transition rather than sampling on request is
		/// what makes "since" meaningful — a caller that gets told "importing, 40s" knows to keep waiting,
		/// whereas "importing" alone is indistinguishable from an import that just started.
		/// </summary>
		internal static void Track()
		{
			var now = DateTime.UtcNow;
			string current = RawBusyLabel();

			if (current != null)
			{
				SetStamp(KeyLastActivity, now);

				if (BusyWith == null)
					SetStamp(KeyBusySince, now);

				BusyWith = current;

				if (ResolvePending)
					ResolveSawActivity = true;

				return;
			}

			BusyWith = null;

			if (!ResolvePending)
				return;

			// Done once the work we were waiting for has been seen AND has stayed quiet long enough. If it was
			// never seen at all, give up after the grace period rather than blocking callers forever.
			bool settledAfterWork = ResolveSawActivity
				&& (now - GetStamp(KeyLastActivity)).TotalSeconds > SettleSeconds;
			bool neverStarted = !ResolveSawActivity
				&& (now - GetStamp(KeyResolveRequested)).TotalSeconds > ResolveGraceSeconds;

			if (settledAfterWork || neverStarted)
				ResolvePending = false;
		}

		private static string RawBusyLabel()
		{
			if (EditorApplication.isCompiling)
				return "compiling";

			if (EditorApplication.isUpdating)
				return "importing";

			return null;
		}

		/// <summary>
		/// True while the editor is doing something a second heavy request would collide with — including the
		/// gaps BETWEEN the phases of one operation, which is what the hysteresis buys. The raw flags are
		/// sampled live so this stays right even if the pump has not ticked since; the tail comes from the
		/// last tick that saw activity.
		/// </summary>
		internal static bool IsBusy( out string _what, out double _sinceSeconds )
		{
			var now = DateTime.UtcNow;

			string raw = RawBusyLabel();
			if (raw != null)
			{
				var since = GetStamp(KeyBusySince);
				_what = raw;
				_sinceSeconds = since != default ? Math.Round((now - since).TotalSeconds, 1) : 0.0;
				return true;
			}

			var lastActivity = GetStamp(KeyLastActivity);
			if (lastActivity != default)
			{
				double quiet = (now - lastActivity).TotalSeconds;
				if (quiet <= SettleSeconds)
				{
					// Quiet, but not yet long enough to be sure this is the end and not a gap between phases.
					_what = "settling";
					_sinceSeconds = Math.Round(quiet, 1);
					return true;
				}
			}

			if (ResolvePending && !ResolveSawActivity)
			{
				// Triggered, not yet observably started. The dangerous window: it LOOKS idle.
				_what = "resolveStarting";
				_sinceSeconds = Math.Round((now - GetStamp(KeyResolveRequested)).TotalSeconds, 1);
				return true;
			}

			_what = null;
			_sinceSeconds = 0.0;
			return false;
		}

		/// <summary>
		/// Reasons why a domain-reloading operation (a package resolve above all) must not start right now.
		/// Empty means go ahead — deliberately so, because the alternative is asking a human before every
		/// package change, and a question that is answered "yes" nine times out of ten trains everyone to
		/// stop reading it. What actually needs a human is the tenth time, and these are those cases:
		/// unsaved editor state that the reload would put at risk, or a running app it would kill.
		/// </summary>
		internal static List<string> ReloadBlockers()
		{
			var blockers = new List<string>();

			if (EditorApplication.isPlayingOrWillChangePlaymode)
				blockers.Add("Play Mode is running, and the domain reload would end it");

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (scene.isDirty)
					blockers.Add($"scene '{scene.path}' has unsaved changes");
			}

			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.scene.isDirty)
				blockers.Add($"prefab '{stage.assetPath}' is open with unsaved changes");

			return blockers;
		}

		/// <summary>
		/// True when the editor is holding this exact asset in a stage or as an open scene — writing it from
		/// outside would lose against the in-memory copy, and saving over it from a tool would fight it.
		/// </summary>
		internal static bool EditorOwns( string _path, out string _reason )
		{
			_reason = null;

			if (string.IsNullOrWhiteSpace(_path))
				return false;

			string path = _path.Replace('\\', '/');

			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && string.Equals(stage.assetPath, path, StringComparison.OrdinalIgnoreCase))
			{
				_reason = stage.scene.isDirty
					? $"'{path}' is open in Prefab Mode WITH UNSAVED CHANGES"
					: $"'{path}' is open in Prefab Mode";
				return true;
			}

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!string.Equals(scene.path, path, StringComparison.OrdinalIgnoreCase))
					continue;

				_reason = scene.isDirty
					? $"scene '{path}' is open WITH UNSAVED CHANGES"
					: $"scene '{path}' is open in the editor";
				return true;
			}

			return false;
		}

		internal static JObject StatusJson( string _projectPath, int _port )
		{
			IsBusy(out string busyWith, out double busySince);

			var scenes = new JArray();
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				scenes.Add(new JObject
				{
					["path"] = scene.path,
					["name"] = scene.name,
					["isDirty"] = scene.isDirty,
					["isLoaded"] = scene.isLoaded,
				});
			}

			var result = new JObject
			{
				["running"] = true,
				["compiling"] = EditorApplication.isCompiling,
				["updating"] = EditorApplication.isUpdating,
				["isPlaying"] = EditorApplication.isPlaying,
				["hasFocus"] = UnityEditorInternal.InternalEditorUtility.isApplicationActive,
				["openScenes"] = scenes,
				["prefabStage"] = PrefabStageJson(),
				["projectPath"] = _projectPath,
				["port"] = _port,
			};

			result["busyWith"] = busyWith != null ? (JToken)busyWith : JValue.CreateNull();
			if (busyWith != null)
				result["busySinceSeconds"] = busySince;

			// Reported separately so a caller can wait on "the resolve I asked for is really over" without
			// having to interpret labels.
			result["resolvePending"] = ResolvePending;

			return result;
		}

		private static JToken PrefabStageJson()
		{
			// The topmost stage only. A nested prefab opened inside another still means "the editor owns
			// this file", which is all a caller needs to keep its hands off.
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage == null)
				return JValue.CreateNull();

			return new JObject
			{
				["path"] = stage.assetPath,
				["isDirty"] = stage.scene.isDirty,
			};
		}

		internal static JObject AssetStateJson( JObject _request )
		{
			var paths = _request?["paths"] as JArray;
			if (paths == null || paths.Count == 0)
				throw new Exception("assetState requires 'paths': a non-empty array of project-relative paths.");

			var openScenePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var dirtyScenePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (string.IsNullOrEmpty(scene.path))
					continue;

				openScenePaths.Add(scene.path);
				if (scene.isDirty)
					dirtyScenePaths.Add(scene.path);
			}

			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			string stagePath = stage != null ? stage.assetPath : null;
			bool stageDirty = stage != null && stage.scene.isDirty;

			var assets = new JArray();
			foreach (var token in paths)
			{
				string path = (string)token;
				if (string.IsNullOrWhiteSpace(path))
					continue;

				assets.Add(DescribeAsset(path.Replace('\\', '/'), openScenePaths, dirtyScenePaths,
					stagePath, stageDirty));
			}

			return new JObject { ["assets"] = assets };
		}

		private static JObject DescribeAsset
		(
			string _path,
			HashSet<string> _openScenePaths,
			HashSet<string> _dirtyScenePaths,
			string _stagePath,
			bool _stageDirty
		)
		{
			bool isScene = _path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
			bool isPrefab = _path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

			var result = new JObject
			{
				["path"] = _path,
				["kind"] = isScene ? "scene" : isPrefab ? "prefab" : "other",
			};

			var mainAsset = AssetDatabase.LoadMainAssetAtPath(_path);
			bool exists = mainAsset != null || System.IO.File.Exists(_path);
			result["exists"] = exists;

			bool inPrefabStage = isPrefab && _stagePath != null
				&& string.Equals(_stagePath, _path, StringComparison.OrdinalIgnoreCase);
			bool openInEditor = inPrefabStage || (isScene && _openScenePaths.Contains(_path));
			bool dirty = (inPrefabStage && _stageDirty) || (isScene && _dirtyScenePaths.Contains(_path));

			result["openInEditor"] = openInEditor;
			result["inPrefabStage"] = inPrefabStage;
			result["dirty"] = dirty;

			var reasons = new List<string>();

			if (!exists)
				reasons.Add("the asset does not exist");

			if (openInEditor)
			{
				reasons.Add(dirty
					? "the editor has it open WITH UNSAVED CHANGES — its in-memory copy would overwrite the write"
					: "the editor has it open; write it through the editor, or ask for it to be closed first");
			}

			JToken missing = JValue.CreateNull();
			bool savable = true;

			if (isPrefab && exists)
			{
				missing = MissingScriptsJson(_path, out int missingCount, out string firstName);
				if (missingCount > 0)
				{
					savable = false;
					reasons.Add($"{missingCount} component(s) have an unloadable script (first on '{firstName}'); "
						+ "Unity refuses to save such a prefab and logs one error per offending GameObject");
				}
			}

			result["missingScripts"] = missing;
			result["safeToWriteFromOutside"] = exists && !openInEditor;
			// A path that does not exist is not "savable" — reporting true there is formally defensible and
			// practically an invitation to read it as a green light.
			result["savableByUnity"] = exists && savable;

			if (reasons.Count > 0)
				result["why"] = string.Join("; ", reasons);

			return result;
		}

		private static JToken MissingScriptsJson( string _path, out int _count, out string _firstName )
		{
			_count = 0;
			_firstName = null;

			var root = AssetDatabase.LoadAssetAtPath<GameObject>(_path);
			if (root == null)
				return JValue.CreateNull();

			var names = new JArray();
			foreach (var transform in root.GetComponentsInChildren<Transform>(true))
			{
				int onThis = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
				if (onThis <= 0)
					continue;

				_count += onThis;
				_firstName ??= transform.gameObject.name;

				if (names.Count < MaxNamedGameObjects)
					names.Add(transform.gameObject.name);
			}

			if (_count == 0)
				return new JObject { ["count"] = 0 };

			var result = new JObject { ["count"] = _count, ["gameObjects"] = names };
			if (_count > names.Count)
				result["truncated"] = true;

			return result;
		}
	}
}
