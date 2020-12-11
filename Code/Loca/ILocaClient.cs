using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaClient
	{
#if UNITY_EDITOR
		string Key {get;}
#endif
	}
}