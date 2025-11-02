using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public class LocaProviderList
	{
		public const string RESOURCES_SUB_PATH = "LocaJson/";
		private const string PATH = "Assets/Resources/" + RESOURCES_SUB_PATH + "_locaProviders.json";

		public List<string> Paths = new();

		public static void Load()
		{

		}

#if UNITY_EDITOR
		public void Save()
		{
			string json = JsonUtility.ToJson(this, true);
			File.WriteAllText(PATH, json, new UTF8Encoding(false));
		}
#endif
	}
}