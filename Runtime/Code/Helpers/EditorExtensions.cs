#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace GuiToolkit
{
	public static class EditorExtensions
	{
		public static IEnumerable<SerializedProperty> GetVisibleChildren( this SerializedProperty _serializedProperty, bool _hideScript = true )
		{
			SerializedProperty currentProperty = _serializedProperty.Copy();

			if (currentProperty.NextVisible(true))
			{
				do
				{
					if (_hideScript && currentProperty.name == "m_Script")
						continue;

					yield return currentProperty;
				}
				while (currentProperty.NextVisible(false));
			}
		}

		public static void DisplayProperties( this SerializedObject _this )
		{
			var props = _this.GetIterator().GetVisibleChildren();
			foreach (var prop in props)
				EditorGUILayout.PropertyField(prop, true);
		}
		
		public static string ToLogicalPath(this string _s) => string.IsNullOrEmpty(_s) ? _s : FileUtil.GetLogicalPath(_s);
		public static string ToPhysicalPath(this string _s) => string.IsNullOrEmpty(_s) ? _s : FileUtil.GetPhysicalPath(_s);

	}
}
#endif