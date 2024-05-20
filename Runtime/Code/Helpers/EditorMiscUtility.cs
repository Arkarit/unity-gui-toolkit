using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	/// \brief General Editor Utility
	/// 
	/// This is a collection of miscellaneous editor helper functions.
	/// 
	/// Note: This file must reside outside of an "Editor" folder, since it must be accessible
	/// from mixed game/editor classes (even though all accesses are in #if UNITY_EDITOR clauses)
	/// See https://answers.unity.com/questions/426184/acces-script-in-the-editor-folder.html for reasons.
	public static class EditorMiscUtility
	{
		public static bool MultipleBitsSet( int _n )
		{
			return (_n & (_n - 1)) != 0;
		}

		public static bool ValidateListAndIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateList(_list))
				return false;

			return ValidateListIndex(_idx, _list);
		}

		public static bool ValidateList( SerializedProperty _list )
		{
			if (!_list.isArray)
			{
				Debug.LogError("Attempt to access array element from a SerializedProperty which isn't an array");
				return false;
			}
			return true;
		}

		public static bool ValidateListIndex( int _idx, SerializedProperty _list )
		{
			if (_idx < 0 || _idx >= _list.arraySize)
			{
				Debug.LogError("Out of Bounds when accessing an array element");
				return false;
			}
			return true;
		}
	}
}
