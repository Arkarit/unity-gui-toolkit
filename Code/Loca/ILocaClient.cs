using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaClient
	{
#if UNITY_EDITOR
		string LocaGroup {get;}
		bool UsesMultipleLocaKeys {get;}
		string LocaKey {get;}
		List<string> LocaKeys {get;}
#endif
	}
}