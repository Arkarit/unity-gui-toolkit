using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiClickCatcher : Graphic
	{
		public override bool Raycast( Vector2 _, Camera __ ) => true;
		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			_vh.Clear();
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiClickCatcher))]
	public class UiClickCatcherEditor : Editor
	{
		public override void OnInspectorGUI() {}
	}
#endif

}
