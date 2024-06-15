using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public static class ObjectUtility
	{
		private class TempScriptableObject : ScriptableObject
		{
			[SerializeReference] public object Value;
			[SerializeField] public UnityEngine.Object ValueObj;
		}

		public static T SafeClone<T>(T _obj)
		{
			var unityObject = _obj as Object;

			bool isUnityObject = _obj is UnityEngine.Object;

			if (_obj == null)
				return _obj;

			var original = ScriptableObject.CreateInstance<TempScriptableObject>();

			if (isUnityObject)
				original.ValueObj = _obj as Object;
			else
				original.Value = _obj;

			var cloned = Object.Instantiate(original);
			object result = isUnityObject ? cloned.ValueObj : cloned.Value;

			original.Destroy(false);
			cloned.Destroy(false);

			return (T) result;
		}
	}
}