using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

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
			Hierarchy		= 0x0004,
			AssetPath		= 0x0008,
			InstancePath	= 0x0010,

			Default			= 0xffff
		}

		public static void Log(string _text, GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default) =>
			Debug.Log(GetLogString(_text, _gameObject, _features));

		public static void Log(string _text, List<GameObject> _gameObjects, DumpFeatures _features = DumpFeatures.Default) =>
			Debug.Log(GetLogString(_text, _gameObjects, _features));

		public static void Log(string _text, GameObject[] _gameObjects, DumpFeatures _features = DumpFeatures.Default) =>
			Debug.Log(GetLogString(_text, _gameObjects.ToList(), _features));

		public static string GetLogString(string _text, List<GameObject> _gameObjects, DumpFeatures _features = DumpFeatures.Default)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"{_text}:");
			foreach (var gameObject in _gameObjects)
				sb.Append(GetLogString(null, gameObject, _features));

			return sb.ToString();
		}

		public static string GetLogString(string _text, GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default)
		{
			if (!string.IsNullOrEmpty(_text))
				_text += " ";

			string assetPathString = string.Empty;
			if (HasFlag(_features, DumpFeatures.AssetPath))
			{
#if UNITY_EDITOR
				var assetPath = AssetDatabase.GetAssetPath(_gameObject);
				if (string.IsNullOrEmpty(assetPath))
					assetPath = "<null>";
#else
				string assetPath = "<N/A>";
#endif
				assetPathString = $"Asset Path:'{assetPath}'\n";
			}

			string callerPrefixString = HasFlag(_features, DumpFeatures.CallingMethod) ? $"[{GetCallingClassAndMethod()}]:" : string.Empty;
			string hierarchyString = HasFlag(_features, DumpFeatures.Hierarchy) ? $"Hierarchy:{GetHierarchyString(_gameObject, _features)}\n" : string.Empty;
			string instancePathString = HasFlag(_features, DumpFeatures.InstancePath) ? $"Instance Path:'{_gameObject.GetPath()}'\n" : string.Empty;

			return $"{callerPrefixString}{_text}{_gameObject.GetPath(1)}\n{instancePathString}{assetPathString}{hierarchyString}";
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

		public static string DumpOverridesString(GameObject _asset, string _what)
		{
			if (!PrefabUtility.IsPartOfVariantPrefab(_asset) && !PrefabUtility.IsAnyPrefabInstanceRoot(_asset))
				return string.Empty;

			var sourcePropertyModifications = PrefabUtility.GetPropertyModifications(_asset);
			var sourceObjectOverrides = PrefabUtility.GetObjectOverrides(_asset);
			var addedComponents = PrefabUtility.GetAddedComponents(_asset);
			var addedGameObjects = PrefabUtility.GetAddedGameObjects(_asset);
			var removedComponents = PrefabUtility.GetRemovedComponents(_asset);
			var removedGameObjects = PrefabUtility.GetRemovedGameObjects(_asset);

			string result = $"{_what}: '{AssetDatabase.GetAssetPath(_asset)}':\n\n";
			
			result += GetHeadline($"{_what} Property Modifications ({sourcePropertyModifications.Length})");
			foreach (var modification in sourcePropertyModifications)
				result += $"\t\t'{modification.value}':'{modification.propertyPath}':'{modification.objectReference}':'{modification.target}'\n";
			result += "\n";
			
			result += GetHeadline($"{_what} Object Overrides");
			foreach (var sourceObjectOverride in sourceObjectOverrides)
				result += $"\t\t'{sourceObjectOverride.coupledOverride}':'{sourceObjectOverride.instanceObject}'\n";

			result += "\n";
			
			result += GetHeadline($"{_what} Added Components");
			foreach (var addedComponent in addedComponents)
				result += $"\t\t'{addedComponent.instanceComponent.name}'\n";
			result += "\n";
			
			result += GetHeadline($"{_what} Added Game Objects");
			foreach (var addedGameObject in addedGameObjects)
				result += $"\t\t'{addedGameObject.instanceGameObject.name}':'{addedGameObject.siblingIndex}'\n";
			result += "\n";
			
			result += GetHeadline($"{_what} Removed Components");
			foreach (var removedComponent in removedComponents)
				result += $"\t\t'{removedComponent.assetComponent.name}'\n";
			result += "\n";
			
			result += GetHeadline($"{_what} Removed Game Objects");
			foreach (var removedGameObject in removedGameObjects)
				result += $"\t\t'{removedGameObject.assetGameObject.name}'\n";
			result += "\n";
			
			return result;
		}

		private static string GetHeadline(string _text) => $"\t{_text}\n\t{new string('-', 80)}\n";

		public static string DumpCorrespondingObjectFromSource(Object _obj)
		{
			string result = string.Empty;
			if (_obj == null)
				return "<null>";

			var cofs = PrefabUtility.GetCorrespondingObjectFromSource(_obj);
			if (cofs != null)
			{
				result = $" cofs:{cofs.name} type:{cofs.GetType()}";
				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(cofs, out string guid, out long id))
					result += $" guid:{guid} fileid:{id}";
			}

			return result;
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