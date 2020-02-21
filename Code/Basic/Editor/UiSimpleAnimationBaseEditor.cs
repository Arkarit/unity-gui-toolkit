using Unity;
using UnityEditor;

namespace GuiToolkit
{

	[CustomEditor(typeof(UiSimpleAnimationBase))]
	public class UiSimpleAnimationBaseEditor : Editor
	{
		public virtual void OnEnable()
		{
		}

		public virtual void EditSubClass() { }

		public override void OnInspectorGUI()
		{
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;

			DrawDefaultInspector();

			EditSubClass();

			serializedObject.ApplyModifiedProperties();
		}

	}
}