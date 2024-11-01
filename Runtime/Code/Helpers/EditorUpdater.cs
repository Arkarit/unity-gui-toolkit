using System;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
#endif

// Update() method of Unity MonoBehaviour's works unreliably when the app is not running, 
// even if the class is marked as [ExecuteInEditMode], since Update() is not called regularly.
// https://docs.unity3d.com/ScriptReference/ExecuteInEditMode.html
// Here's a workaround for this.
// Also, Time.deltaTime is unreliable when App is not running; so a delta is provided.
// A class can remove itself from the update, once its animation is done.

namespace GuiToolkit
{

	[Flags]
	public enum UpdateCondition : int
	{
		Invalid = 0,
		IsPlaying = 1,
		IsNotPlaying = 2,
		Always,
	}

	public interface IEditorUpdateable
	{
#if UNITY_EDITOR
		void UpdateInEditor( float deltaTime );
		bool RemoveFromEditorUpdate();
		UpdateCondition editorUpdateCondition { get; }
#endif
	}

	public static class EditorUpdater
	{
#if UNITY_EDITOR
		public static float s_timeScale = 1;
		private static readonly List<IEditorUpdateable> s_updateables = new ();
		private static readonly System.Diagnostics.Stopwatch s_stopwatch = new ();
		private static readonly List<int> s_scheduledForDestroy = new ();
#endif

		private static void Update()
		{
#if UNITY_EDITOR
			float deltaTime = s_stopwatch.ElapsedMilliseconds / 1000.0f * s_timeScale;
			s_stopwatch.Restart();

			s_scheduledForDestroy.Clear();

			UpdateCondition updateCondition = Application.isPlaying ? UpdateCondition.IsPlaying : UpdateCondition.IsNotPlaying;

			for (int i = 0; i < s_updateables.Count; i++)
			{
				IEditorUpdateable _updateable = s_updateables[i];
				if ((_updateable as Object) == null)
				{
					s_scheduledForDestroy.Add(i);
					continue;
				}

				if ((updateCondition & _updateable.editorUpdateCondition) != 0)
					_updateable.UpdateInEditor(deltaTime);

				if (_updateable.RemoveFromEditorUpdate())
					s_scheduledForDestroy.Add(i);
			}

			for (int i = s_scheduledForDestroy.Count - 1; i >= 0; i--)
				s_updateables.RemoveAt(s_scheduledForDestroy[i]);

			if (s_updateables.Count == 0)
				EditorApplication.update -= Update;
#endif
		}

		static public void StartUpdating( IEditorUpdateable _updateable )
		{
#if UNITY_EDITOR
			if (s_updateables.Contains(_updateable))
				return;

			if (s_updateables.Count == 0)
			{
				s_stopwatch.Restart();
				EditorApplication.update += Update;
			}

			s_updateables.Add(_updateable);
#endif
		}

		static public void StopUpdating( IEditorUpdateable _updateable )
		{
#if UNITY_EDITOR
			if (!s_updateables.Contains(_updateable))
				return;

			s_updateables.Remove(_updateable);
			if (s_updateables.Count == 0)
				EditorApplication.update -= Update;
#endif
		}
	}
}
