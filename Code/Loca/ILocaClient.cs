using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaClient
	{
#if UNITY_EDITOR
		LocaGroup LocaGroup {get;}
		bool UsesMultipleLocaKeys {get;}
		string LocaKey {get;}
		List<string> LocaKeys {get;}
#endif
	}
}