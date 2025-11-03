using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[Serializable]
	public sealed class LocaJson
	{
		public string Group;
		public List<LocaJsonEntry> Entries;
	}

	[Serializable]
	public sealed class LocaJsonEntry
	{
		public string LanguageId;
		public string Key;
		public string Text;       // optional
		public string[] Forms;    // optional, length up to 6
	}

	public interface ILocaProvider
	{
		public LocaJson Localization { get; }

#if UNITY_EDITOR
		public void CollectData();
#endif
	}
}