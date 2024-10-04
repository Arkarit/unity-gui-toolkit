using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit
{
	/// <summary>
	/// A simple tick manager for frame, second and minute.
	/// Unscaled time not yet implemented.
	/// </summary>
	public class TickProvider : MonoBehaviour
	{
		private static TickProvider s_instance;
		private int m_seconds;
		private int m_minutes;
		private int m_ticks;
		private float m_time;

		public static int Ticks => s_instance.m_ticks;
		public static int Seconds => s_instance.m_seconds;
		public static int Minutes => s_instance.m_minutes;
		public static float Time => s_instance.m_time;

		private void Update()
		{
			m_ticks++;
			UiEventDefinitions.OnTickPerFrame.Invoke();

			m_time = UnityEngine.Time.time;

			int currSeconds = (int) m_time;
			if (currSeconds > m_seconds)
			{
				// We don't add but only increment to keep up with the callbacks.
				m_seconds++;
				UiEventDefinitions.OnTickPerSecond.Invoke();
			}

			int currMinutes = (int) m_time / 60;
			if (currMinutes > m_minutes)
			{
				m_minutes++;
				UiEventDefinitions.OnTickPerMinute.Invoke();
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			var go = new GameObject("TickProvider");
			s_instance = go.AddComponent<TickProvider>();

			Object.DontDestroyOnLoad(go);
		}
	}
}
