using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class AbstractSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
	{
		protected static T m_instance;

		public static T Instance
		{
			get
			{
				if (m_instance == null)
				{
					GameObject go = new GameObject($"The_{nameof(T)}");
					if (!Application.isPlaying)
						go.hideFlags = HideFlags.HideAndDontSave;

					m_instance = go.AddComponent<T>();
				}
				return m_instance;
			}
		}

		protected virtual void OnUpdate() {}

		private void Update()
		{
			if (m_instance != this)
			{
				gameObject.Destroy(false);
				return;
			}

			OnUpdate();
		}
	}
}
