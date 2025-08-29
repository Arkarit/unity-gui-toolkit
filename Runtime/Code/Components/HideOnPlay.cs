using System;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A very simple mono behaviour, which hides a game object when playing
	/// </summary>
	public class HideOnPlay : MonoBehaviour
	{
		private void OnEnable() => gameObject.SetActive(false);
	}
}