#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[CreateAssetMenu()]
	public class AdditionalLocaKeys : ScriptableObject, ILocaClient
	{
		[FormerlySerializedAs("Keys")] [SerializeField] private List<string> m_keys;
		[SerializeField] private string m_group;

		public bool UsesMultipleLocaKeys => true;

		public string LocaKey => null;

		public List<string> LocaKeys => m_keys;

		public string Group => m_group;
	}
}
#endif