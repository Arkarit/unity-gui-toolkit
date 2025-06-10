using System.Text;
using Codice.CM.Common;
using UnityEngine;

namespace GuiToolkit.Debugging
{
	public static class DebugUtility
	{
		public static string GetGameObjectHierarchyDump(GameObject _gameObject)
		{
			StringBuilder sb = new StringBuilder();

			GetGameObjectHierarchyDump(_gameObject, ref sb, 0);
			return sb.ToString();
		}

		private static void GetGameObjectHierarchyDump(GameObject _gameObject, ref StringBuilder _sb, int _numTabs)
		{
			string tabs = new string('\t', _numTabs);
			_sb.Append($"{tabs}{_gameObject.GetPath(1)}\n");
			if (_gameObject == null)
				return;

			foreach (Transform t in _gameObject.transform)
				GetGameObjectHierarchyDump(t.gameObject, ref _sb, _numTabs + 1);
		}
	}
}