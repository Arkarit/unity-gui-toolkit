using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace GuiToolkit.Debugging
{
	public static class DebugUtility
	{
		[Flags]
		public enum DumpFeatures
		{
			None			= 0,
			Components		= 0x0001,
			CallingMethod	= 0x0002,

			Default			= 0xffff
		}

		public static void Log(string _text, GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default) =>
			Debug.Log(GetLogString(_text, _gameObject, _features));

		public static string GetLogString(string _text, GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default)
		{
			string callerPrefix = HasFlag(_features, DumpFeatures.CallingMethod) ? $"[{GetCallingClassAndMethod()}]:" : string.Empty;
#if UNITY_EDITOR
			string assetPath = AssetDatabase.GetAssetPath(_gameObject);
			if (string.IsNullOrEmpty(assetPath))
				assetPath = "<null>";
#else
			string assetPath = "<N/A>";
#endif
			return $"{callerPrefix}{_text} {_gameObject.GetPath(1)}\nInstance Path:'{_gameObject.GetPath()}'\nAsset Path:'{assetPath}'\nHierarchy:\n{GetHierarchyString(_gameObject, _features)}\n";
		}

		public static string GetHierarchyString(GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default)
		{
			StringBuilder sb = new StringBuilder();
			GetHierarchyString(_gameObject, ref sb, 0, _features);
			return sb.ToString();
		}

		// Get the path of the script which calls this function. Weird and hacky but works.
		public static string GetCallingScriptPath() => new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName();
		public static string GetCallingScriptDirectory() => Path.GetDirectoryName(new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName());

		public static string GetCallingClassAndMethod(bool _includeFilename = false)
		{
			var stackTrace = new System.Diagnostics.StackTrace(true);
			var frames = stackTrace.GetFrames();

			if (frames == null)
				return "<Unknown Caller>";

			foreach (var frame in frames)
			{
				var method = frame.GetMethod();
				var declaringType = method?.DeclaringType;

				if (declaringType == null)
					continue;

				// Skip methods declared in DebugUtility itself
				if (declaringType == typeof(DebugUtility))
					continue;

				// Skip lambdas
				if (declaringType.Name.StartsWith('<'))
					continue;

				var result = $"{declaringType.Name}.{method.Name}";
				if (_includeFilename)
				{
					string fileName = frame.GetFileName();
					int lineNumber = frame.GetFileLineNumber();

					result += (fileName != null ? $" (in {fileName}:{lineNumber})" : "");
				}

				return result;
			}

			return "<External Caller Not Found>";
		}

		private static void GetHierarchyString(GameObject _gameObject, ref StringBuilder _sb, int _numTabs, DumpFeatures _features)
		{
			string tabs = new string('\t', _numTabs);
			_sb.Append($"{tabs}{_gameObject.GetPath(1)}\n");
			if (_gameObject == null)
				return;

			if (HasFlag(_features, DumpFeatures.Components))
			{
				string s = $"{tabs} Components:";
				var components = _gameObject.GetComponents<Component>();
				foreach (var component in components)
					s += $"{tabs} '{component.GetType()}'\n";
				_sb.Append(s);
			}

			foreach (Transform t in _gameObject.transform)
				GetHierarchyString(t.gameObject, ref _sb, _numTabs + 1, _features);
		}

		private static bool HasFlag(DumpFeatures _features, DumpFeatures _feature) => (_features & _feature ) != 0;


	}
}