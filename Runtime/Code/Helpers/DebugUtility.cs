using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;
using System.Diagnostics;
using System.Runtime.CompilerServices;


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
			None = 0,
			Components = 0x0001,
			CallingMethod = 0x0002,
			Hierarchy = 0x0004,
			AssetPath = 0x0008,
			InstancePath = 0x0010,

			Default = 0xffff
		}

		public static void Log( string _text, GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default ) =>
			Debug.Log(GetLogString(_text, _gameObject, _features));

		public static void Log( string _text, List<GameObject> _gameObjects, DumpFeatures _features = DumpFeatures.Default ) =>
			Debug.Log(GetLogString(_text, _gameObjects, _features));

		public static void Log( string _text, GameObject[] _gameObjects, DumpFeatures _features = DumpFeatures.Default ) =>
			Debug.Log(GetLogString(_text, _gameObjects.ToList(), _features));

		public static string GetLogString( string _text, List<GameObject> _gameObjects, DumpFeatures _features = DumpFeatures.Default )
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"{_text}:");
			foreach (var gameObject in _gameObjects)
				sb.Append(GetLogString(null, gameObject, _features));

			return sb.ToString();
		}

		public static string GetLogString( string _text, GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default )
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

		public static string GetHierarchyString( GameObject _gameObject, DumpFeatures _features = DumpFeatures.Default )
		{
			StringBuilder sb = new StringBuilder();
			GetHierarchyString(_gameObject, ref sb, 0, _features);
			return sb.ToString();
		}

		// Get the path of the script which calls this function. Weird and hacky but works.
		public static string GetCallingScriptPath() => new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName();
		public static string GetCallingScriptDirectory() => Path.GetDirectoryName(new System.Diagnostics.StackTrace(true).GetFrame(1).GetFileName());

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static string GetCallingClassAndMethod
		(
			bool _includeFilename = false,
			bool _includeClass = true,
			int _callerOffset = 0,
			bool _skipAccessors = true,
			bool _skipCompilerGenerated = true 
		)
		{
			if (_callerOffset < 0) _callerOffset = 0;

			var trace = new StackTrace(true);
			var frames = trace.GetFrames();
			if (frames == null || frames.Length == 0)
				return "<Unknown Caller>";

			int hits = -1;
			foreach (var frame in frames)
			{
				var method = frame.GetMethod();
				var declaringType = method?.DeclaringType;
				if (declaringType == null)
					continue;

				// Skip frames originating from this utility itself
				if (declaringType == typeof(DebugUtility))
					continue;

				// Skip compiler-generated types/methods (lambdas, async state machines, etc.)
				if (_skipCompilerGenerated)
				{
					if (declaringType.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
						continue;
					if (method.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
						continue;
					// Also skip angle-bracket type names as a heuristic
					if (declaringType.Name.Length > 0 && declaringType.Name[0] == '<')
						continue;
				}

				// Optionally skip property/field accessor boilerplate
				if (_skipAccessors && method.IsSpecialName &&
					(method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
					continue;

				// This frame qualifies
				hits++;
				if (hits < _callerOffset)
					continue;

				string result = method.Name;
				if (_includeClass)
					result = declaringType.Name + "." + result; // use FullName if you prefer namespaces

				if (_includeFilename)
				{
					string file = frame.GetFileName();
					int line = frame.GetFileLineNumber();
					if (!string.IsNullOrEmpty(file))
						result += " (in " + file + ":" + line + ")";
				}

				return result;
			}

			return "<External Caller Not Found>";
		}

#if UNITY_EDITOR
		public static string DumpOverridesString( GameObject _asset, string _what )
		{
			if (!PrefabUtility.IsPartOfVariantPrefab(_asset) && !PrefabUtility.IsAnyPrefabInstanceRoot(_asset))
				return string.Empty;

			var sourcePropertyModifications = PrefabUtility.GetPropertyModifications(_asset);
			var sourceObjectOverrides = PrefabUtility.GetObjectOverrides(_asset);
			var addedComponents = PrefabUtility.GetAddedComponents(_asset);
			var addedGameObjects = PrefabUtility.GetAddedGameObjects(_asset);
			var removedComponents = PrefabUtility.GetRemovedComponents(_asset);
			var removedGameObjects = PrefabUtility.GetRemovedGameObjects(_asset);

			string result = $"{_what}: Asset Path:'{AssetDatabase.GetAssetPath(_asset)}':\n\n";

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
				result += $"\t\t'{addedComponent.instanceComponent.name}':'{addedComponent.instanceComponent.GetType().Name}'\n";
			result += "\n";

			result += GetHeadline($"{_what} Added Game Objects");
			foreach (var addedGameObject in addedGameObjects)
				result += $"\t\t'{addedGameObject.instanceGameObject.name}':'{addedGameObject.siblingIndex}'\n";
			result += "\n";

			result += GetHeadline($"{_what} Removed Components");
			foreach (var removedComponent in removedComponents)
				result += $"\t\t'{removedComponent.assetComponent.name}':'{removedComponent.assetComponent.GetType().Name}'\n";
			result += "\n";

			result += GetHeadline($"{_what} Removed Game Objects");
			foreach (var removedGameObject in removedGameObjects)
				result += $"\t\t'{removedGameObject.assetGameObject.name}'\n";
			result += "\n";

			return result;
		}

		private static string GetHeadline( string _text ) => $"\t{_text}\n\t{new string('-', 80)}\n";


		public static string GetCorrespondingObjectFromSourceString( Object _obj )
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

		public static string GetAllPropertiesString( SerializedObject _serObj )
		{
			string result = $"Properties for '{_serObj.targetObject.name}':\n{new string('-', 80)}\n";
			EditorGeneralUtility.ForeachPropertyHierarchical(_serObj, property =>
			{
				result += $"\t{property.propertyPath}:{property.prefabOverride}\n";
			});

			return result;
		}

		public static string GetAllGuidsString( List<string> _guids, string _text = null )
		{
			string result = $"{_text}:\n{new string('-', 80)}\n";
			for (int i = 0; i < _guids.Count; i++)
			{
				var guid = _guids[i];
				result += $"\t{i:D3} {AssetDatabase.GUIDToAssetPath(guid)} : {guid}\n";
			}

			return result;
		}

#endif


		private static void GetHierarchyString( GameObject _gameObject, ref StringBuilder _sb, int _numTabs, DumpFeatures _features )
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

		private static bool HasFlag( DumpFeatures _features, DumpFeatures _feature ) => (_features & _feature) != 0;


	}
}