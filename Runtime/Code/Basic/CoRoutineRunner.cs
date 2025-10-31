using UnityEngine;

namespace GuiToolkit
{
	public class CoRoutineRunner : MonoBehaviour
	{
		private static CoRoutineRunner s_instance;
		
		private int m_seconds;
		private int m_minutes;
		private int m_ticks;
		private float m_time;
		
		private Camera m_mainCamera;
		private float m_fov;
		
		public int Ticks => m_ticks;
		public int Seconds => m_seconds;
		public int Minutes => m_minutes;
		public float Time => m_time;
		
		public Camera MainCamera => m_mainCamera;
		public float Fov => m_fov;
		
		private void Start()
		{
			InitTicks();
			InitCamera();
		}

		private void Update()
		{
			UpdateTicks();
			UpdateCamera();
		}
		
		private void InitTicks() {}

		private void InitCamera()
		{
			m_mainCamera = Camera.main;
			m_fov = m_mainCamera ? m_mainCamera.fieldOfView : -1;
		}

		private void UpdateTicks()
		{
			m_ticks++;
			UiEventDefinitions.OnTickPerFrame.Invoke(m_ticks);

			m_time = UnityEngine.Time.time;

			int currSeconds = (int) m_time;
			if (currSeconds > m_seconds)
			{
				m_seconds++;
				UiEventDefinitions.OnTickPerSecond.Invoke(m_seconds);
			}

			int currMinutes = (int) m_time / 60;
			if (currMinutes > m_minutes)
			{
				m_minutes++;
				UiEventDefinitions.OnTickPerMinute.Invoke(m_minutes);
			}
		}
		
		private void UpdateCamera()
		{
			var oldCamera = m_mainCamera;
			var oldFov = m_fov;
			
			m_mainCamera = Camera.main;
			m_fov = m_mainCamera ? m_mainCamera.fieldOfView : -1;
			
			if (oldCamera != m_mainCamera)
				UiEventDefinitions.EvMainCameraChanged.Invoke(oldCamera, m_mainCamera);
			
			if (!Mathf.Approximately(oldFov, m_fov))
				UiEventDefinitions.EvMainCameraFovChanged.Invoke(oldFov, m_fov);
		}

		public static CoRoutineRunner Instance => s_instance;
		
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			var go = new GameObject("GuiToolkit.CoRoutineRunner");
			s_instance = go.AddComponent<CoRoutineRunner>();
			DontDestroyOnLoad(go);
		}
	}

}
