using UnityEngine;

namespace GuiToolkit
{
	public class WhatEnablesMe : MonoBehaviour
	{
#if UNITY_EDITOR
		private void OnEnable()
		{
			Debug.Log($"{gameObject} enabled");
		}
		private void OnDisable()
		{
			Debug.Log($"{gameObject} disabled");
		}
#endif
	}
}