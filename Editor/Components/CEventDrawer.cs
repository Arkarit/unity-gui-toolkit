using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(CEvent), true)]
	public class CEventDrawer : UnityEventDrawer
	{
		public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
		{
			base.OnGUI(_position, _property, _label);
		}
	}
}