using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaClient
	{
#if UNITY_EDITOR
		bool UsesMultipleLocaKeys {get;}
		string LocaKey {get;}
		List<string> LocaKeys {get;}
		string Group {get;}
#endif
	}
}