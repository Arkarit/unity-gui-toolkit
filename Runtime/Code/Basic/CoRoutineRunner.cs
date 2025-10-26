using UnityEngine;

namespace GuiToolkit
{
	public class EmptyMonoBehaviour : MonoBehaviour {}
	
	public class ResolutionWatcher : MonoBehaviour
	{
		Vector2Int m_screenSize;
		
		private void Start()
		{
			m_screenSize.x = Screen.width;
			m_screenSize.y = Screen.height;
		}

		private void Update()
		{
			if (m_screenSize.x == Screen.width && m_screenSize.y == Screen.height)
				return;
			
			Vector2Int newScreenSize = new Vector2Int(Screen.width, Screen.height);
			UiEventDefinitions.OnResolutionChanged.Invoke(m_screenSize, newScreenSize);
			m_screenSize = newScreenSize;
		}
	}

	public class CoRoutineRunner : MonoBehaviour
	{
		private static MonoBehaviour s_instance;
		
		public static MonoBehaviour Instance
		{
			get
			{
				if (s_instance == null)
					Init();
				return s_instance;
			}
		}
		
		public static void Init()
		{
			if (GeneralUtility.IsQuitting)
				UiLog.LogError("Attempting to init CoRoutineRunner while quitting");

			var go = new GameObject("GuiToolkit");
			s_instance = go.AddComponent<EmptyMonoBehaviour>();
			go.AddComponent<ResolutionWatcher>();
			Object.DontDestroyOnLoad(go);
		}
	}

}
