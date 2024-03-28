using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// \brief Coroutine manager
	/// 
	/// Shitty Unity does not allow coroutines to be running on disabled objects (sigh)
	/// This is a coroutine manager, which is always enabled and thus can run
	/// the coroutines for disabled objects

	[AddComponentMenu("")]
	public class CoroutineManager : MonoBehaviour
	{
		private static CoroutineManager s_instance;
		public static CoroutineManager Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = FindObjectOfType<CoroutineManager>();
				if (s_instance == null)
				{
					GameObject go = new GameObject();
					go.name = typeof(CoroutineManager).Name;
					DontDestroyOnLoad(go);
					s_instance = go.AddComponent<CoroutineManager>();
				}

				return s_instance;
			}
		}
	}
}