using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Puts the editor's open scenes back the way they were after a project-wide scan.
	///
	/// Why this exists rather than a fix at each cause: a scan opens every scene in the project additively,
	/// and any [ExecuteAlways] script in a scanned scene runs its Awake while the user's scene is still the
	/// active one. Such a script may then find a Canvas across the scene boundary, create objects that land
	/// in the active scene, or delete something it believes is its own. There is no way to know in advance
	/// which third-party or dev script will do that, so the scan hands the scenes back untouched instead of
	/// trying to make every script well-behaved.
	///
	/// The one thing it must never do is throw away work. It therefore only arms itself when every open
	/// scene is clean to begin with: a scene the user has unsaved changes in is left alone, and the caller
	/// is told so via <see cref="IsArmed"/> — ask the user what to do before starting the scan, do not
	/// decide it here.
	///
	/// Restoring means re-opening the same scenes, which discards changes WITHOUT saving. Selection, the
	/// hierarchy's expanded state and other editor-side niceties do not survive.
	/// </summary>
	/// <example>
	/// <code>
	/// using (var guard = new EditorOpenSceneGuard())
	/// {
	///     if (!guard.IsArmed &amp;&amp; !AskUserToContinueAnyway(guard.NotArmedReason))
	///         return;
	///     RunTheScan();
	/// }
	/// </code>
	/// </example>
	public sealed class EditorOpenSceneGuard : IDisposable
	{
		private readonly struct SceneState
		{
			public SceneState( string _path, bool _isLoaded, bool _isActive )
			{
				Path = _path;
				IsLoaded = _isLoaded;
				IsActive = _isActive;
			}

			public string Path { get; }
			public bool IsLoaded { get; }
			public bool IsActive { get; }
		}

		// Nested scopes: only the outermost one restores, so a scan that runs several searches in a row
		// does not reload the scenes between them.
		private static int s_depth;

		private readonly List<SceneState> m_scenes = new();
		private readonly bool m_isOutermost;
		private bool m_disposed;

		public EditorOpenSceneGuard()
		{
			m_isOutermost = s_depth == 0;
			s_depth++;

			if (!m_isOutermost)
			{
				NotArmedReason = "An enclosing scene guard is already active.";
				return;
			}

			for (int i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				var scene = EditorSceneManager.GetSceneAt(i);

				if (string.IsNullOrEmpty(scene.path))
				{
					// An unsaved, never-written scene has no path to re-open it from. Restoring the others
					// around it would destroy it, so the guard stays out of the way entirely.
					NotArmedReason = $"The scene '{scene.name}' has never been saved.";
					m_scenes.Clear();
					return;
				}

				if (scene.isDirty)
				{
					NotArmedReason = $"The scene '{scene.name}' has unsaved changes.";
					m_scenes.Clear();
					return;
				}

				m_scenes.Add(new SceneState(scene.path, scene.isLoaded,
					scene == EditorSceneManager.GetActiveScene()));
			}

			IsArmed = m_scenes.Count > 0;

			if (!IsArmed && NotArmedReason == null)
			{
				NotArmedReason = "No saved scene is open.";
			}
		}

		/// <summary>Whether the scenes will actually be restored when this scope ends.</summary>
		public bool IsArmed { get; }

		/// <summary>Why the guard is not armed, for a message to the user. Null while it is armed.</summary>
		public string NotArmedReason { get; }

		public void Dispose()
		{
			if (m_disposed)
			{
				return;
			}

			m_disposed = true;
			s_depth--;

			if (!IsArmed || !AnyOpenSceneIsDirty())
			{
				return;
			}

			Restore();
		}

		private static bool AnyOpenSceneIsDirty()
		{
			for (int i = 0; i < EditorSceneManager.sceneCount; i++)
			{
				if (EditorSceneManager.GetSceneAt(i).isDirty)
				{
					return true;
				}
			}

			return false;
		}

		private void Restore()
		{
			UiLog.Log("[SceneGuard] The scan left the open scenes changed; reverting them to their saved "
				+ "state. Nothing is saved, so no work on disk is affected.");

			string activePath = null;

			for (int i = 0; i < m_scenes.Count; i++)
			{
				var state = m_scenes[i];

				// The first one replaces everything, which also disposes of whatever the scan left behind
				// in scenes that were not open before.
				var mode = i == 0
					? OpenSceneMode.Single
					: state.IsLoaded
						? OpenSceneMode.Additive
						: OpenSceneMode.AdditiveWithoutLoading;

				try
				{
					EditorSceneManager.OpenScene(state.Path, mode);
				}
				catch (Exception e)
				{
					UiLog.LogError($"[SceneGuard] Could not restore the scene '{state.Path}': {e.Message}");
					continue;
				}

				if (state.IsActive)
				{
					activePath = state.Path;
				}
			}

			if (activePath == null)
			{
				return;
			}

			var active = EditorSceneManager.GetSceneByPath(activePath);
			if (active.IsValid() && active.isLoaded)
			{
				EditorSceneManager.SetActiveScene(active);
			}
		}
	}
}
