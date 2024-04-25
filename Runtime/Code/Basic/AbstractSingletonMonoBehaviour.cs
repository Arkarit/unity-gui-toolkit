using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class AbstractSingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
	{
		protected static T s_instance;

		public static T Instance
		{
			get
			{
				if (s_instance == null)
				{
					GameObject go = new GameObject($"The_{nameof(T)}");
					if (!Application.isPlaying)
						go.hideFlags = HideFlags.HideAndDontSave;

					s_instance = go.AddComponent<T>();
				}
				return s_instance;
			}
		}

		protected virtual void OnUpdate() {}

		private void Update()
		{
			if (s_instance != this)
			{
				gameObject.Destroy(false);
				return;
			}

			OnUpdate();
		}
	}
}
