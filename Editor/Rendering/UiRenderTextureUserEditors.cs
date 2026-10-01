using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	internal static class RenderTextureUserEditorUtility
	{
		public static void DrawTextureInfo( RenderTexture _texture )
		{
			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(true))
			{
				if (_texture == null)
				{
					EditorGUILayout.LabelField("Texture", "none");
					return;
				}

				EditorGUILayout.ObjectField("Texture", _texture, typeof(RenderTexture), false);
				EditorGUILayout.LabelField("Size", $"{_texture.width} x {_texture.height}");
				EditorGUILayout.LabelField("Format", $"{_texture.format}, depth {_texture.depth}, MSAA {_texture.antiAliasing}");
			}
		}
	}

	[CustomEditor(typeof(UiRenderTextureProducer), true)]
	public class UiRenderTextureProducerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			RenderTextureUserEditorUtility.DrawTextureInfo(((UiRenderTextureProducer)target).Texture);
		}

		public override bool RequiresConstantRepaint() => true;
	}

	[CustomEditor(typeof(UiRenderTextureConsumer), true)]
	public class UiRenderTextureConsumerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var consumer = (UiRenderTextureConsumer)target;
			RenderTextureUserEditorUtility.DrawTextureInfo(consumer.Texture);
			using (new EditorGUI.DisabledScope(true))
			{
				var requested = consumer.RequestedSize;
				EditorGUILayout.LabelField("Requested", $"{requested.x} x {requested.y}");
			}
		}

		public override bool RequiresConstantRepaint() => true;
	}
}
