using UnityEngine;

namespace GuiToolkit
{
	public static class LocaHelper
	{
		public static void AddKeyFromClient(ILocaClient _locaClient)
		{
			if (_locaClient.UsesMultipleLocaKeys)
			{
				var keys = _locaClient.LocaKeys;
				foreach (var key in keys)
					AddKey(_locaClient, key);
			}
			else
			{
				AddKey(_locaClient, _locaClient.LocaKey);
			}
		}

		private static void AddKey(ILocaClient _locaClient, string _key)
		{
			string groupToken = "";
			if (_locaClient.LocaGroup != null)
				groupToken = _locaClient.LocaGroup.Token;

			UiMain.LocaManager.AddKey(groupToken, _key);
		}
	}
}