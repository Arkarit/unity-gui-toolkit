using System;
using System.Collections.Generic;
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

		private static string s_busyWith;
		private static DateTime s_busySinceUtc;

		/// <summary>
		/// Called from the bridge's update pump. Tracking the transition rather than sampling on request is
		/// what makes "since" meaningful — a caller that gets told "importing, 40s" knows to keep waiting,
		/// whereas "importing" alone is indistinguishable from an import that just started.
		/// </summary>
		internal static void Track()
		{
			string current = CurrentBusyLabel();

			if (current == s_busyWith)
				return;

			s_busyWith = current;
			s_busySinceUtc = current != null ? DateTime.UtcNow : default;
		}

		private static string CurrentBusyLabel()
		{
			if (EditorApplication.isCompiling)
				return "compiling";

			if (EditorApplication.isUpdating)
				return "importing";

			return null;
		}

		/// <summary>
		/// True while the editor is doing something that a second heavy request would collide with. The
		/// label is sampled live, so this stays right even if the pump has not ticked since.
		/// </summary>
		internal static bool IsBusy( out string _what, out double _sinceSeconds )
		{
			_what = CurrentBusyLabel();
			_sinceSeconds = _what != null && _what == s_busyWith && s_busySinceUtc != default
				? Math.Round((DateTime.UtcNow - s_busySinceUtc).TotalSeconds, 1)
				: 0.0;

			return _what != null;
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

			if (busyWith != null)
			{
				result["busyWith"] = busyWith;
				result["busySinceSeconds"] = busySince;
			}
			else
			{
				result["busyWith"] = null;
			}

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
			result["savableByUnity"] = savable;

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
