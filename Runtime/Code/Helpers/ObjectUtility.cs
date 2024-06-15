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
		}

		public static T SafeClone<T>(T _obj)
		{
			var original = ScriptableObject.CreateInstance<TempScriptableObject>();
			original.Value = _obj;
			var cloned = Object.Instantiate(original);
			var result = (T) cloned.Value;
			original.Destroy(false);
			cloned.Destroy(false);

			return result;
		}
	}
}