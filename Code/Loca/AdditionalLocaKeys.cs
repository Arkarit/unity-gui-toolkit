#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	[CreateAssetMenu()]
	public class AdditionalLocaKeys : ScriptableObject, ILocaClient
	{
		public List<string> Keys;

		public bool UsesMultipleLocaKeys => true;

		public string LocaKey => null;

		public List<string> LocaKeys => Keys;
	}
}
#endif