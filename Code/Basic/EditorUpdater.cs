#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
#endif

// Update() method of Unity MonoBehaviour's works unreliably when the app is not running, 
// even if the class is marked as [ExecuteInEditMode], since Update() is not called regularly.
// https://docs.unity3d.com/ScriptReference/ExecuteInEditMode.html
// Here's a workaround for this.
// Also, Time.deltaTime is unreliable when App is not running; so a delta is provided.
// A class can remove itself from the update, once its animation is done.

namespace GuiToolkit
{

	public interface IEditorUpdateable
	{
#if UNITY_EDITOR
		void UpdateInEditor( float _deltaTime );
		bool RemoveFromEditorUpdate();
#endif
	}

	public static class EditorUpdater
	{
#if UNITY_EDITOR
		public static float TimeScale = 1;
		private static List<IEditorUpdateable> m_updateables = new List<IEditorUpdateable>();
		private static System.Diagnostics.Stopwatch m_stopwatch = new System.Diagnostics.Stopwatch();
#endif

		static private void Update()
		{
#if UNITY_EDITOR
			float deltaTime = m_stopwatch.ElapsedMilliseconds / 1000.0f * TimeScale;
			m_stopwatch.Restart();

			List<int> moribund = new List<int>();
			for (int i = 0; i < m_updateables.Count; i++)
			{
				IEditorUpdateable updateable = m_updateables[i];
				updateable.UpdateInEditor(deltaTime);
				if (updateable.RemoveFromEditorUpdate())
					moribund.Add(i);
			}
			for (int i = moribund.Count - 1; i >= 0; i--)
				m_updateables.RemoveAt(moribund[i]);
			if (m_updateables.Count == 0)
				EditorApplication.update -= Update;
#endif
		}

		static public void StartUpdating( IEditorUpdateable _updateable )
		{
#if UNITY_EDITOR
			if (m_updateables.Count == 0)
			{
				m_stopwatch.Restart();
				EditorApplication.update += Update;
			}
			m_updateables.Add(_updateable);
#endif
		}
	}
}