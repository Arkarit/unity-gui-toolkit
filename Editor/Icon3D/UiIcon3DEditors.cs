using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiIcon3D), true)]
	[CanEditMultipleObjects]
	public class UiIcon3DEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (targets.Length != 1)
				return;

			var icon = (UiIcon3D)target;
			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(true))
			{
				string state = icon.HasLoadFailed ? "load failed (see console)"
					: icon.IsLoading ? "loading"
					: icon.IsRendered ? "rendered"
					: "pending";
				EditorGUILayout.LabelField("State", state);
				var texture = icon.RawImage != null ? icon.RawImage.texture : null;
				// 'is' alone also matches a destroyed texture
				if (texture is RenderTexture rt && rt != null)
					EditorGUILayout.LabelField("Texture", $"{rt.width} x {rt.height}");
				EditorGUILayout.LabelField("Shared icons", UiIcon3DRenderer.RequestCount.ToString());
			}

			if (icon.RawImage != null && icon.RawImage.material != null
			    && icon.RawImage.material.shader != null && icon.RawImage.material.shader.name != "UIToolkit/UI_Icon3D")
			{
				EditorGUILayout.HelpBox("The RawImage should use the UI_Icon3D material (premultiplied alpha), otherwise edges get dark fringes.",
					MessageType.Warning);
			}

			if (GUILayout.Button("Render again"))
				foreach (var t in targets)
					((UiIcon3D)t).Refresh();
		}

		public override bool RequiresConstantRepaint() => true;
	}

	[CustomEditor(typeof(UiIcon3DBoundsHint))]
	[CanEditMultipleObjects]
	public class UiIcon3DBoundsHintEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space();
			if (GUILayout.Button(new GUIContent("Fit to Renderers", "Set the bounds to what the renderers below this GameObject cover now (particles, trails and lines excluded)")))
			{
				foreach (var t in targets)
				{
					var hint = (UiIcon3DBoundsHint)t;
					Undo.RecordObject(hint, "Fit Bounds to Renderers");
					if (!hint.FitToRenderers())
						UiLog.LogWarning("No active renderers found below this GameObject", hint);
					EditorUtility.SetDirty(hint);
				}
			}
		}
	}

	/// <summary>
	/// 3D icons are rendered once and cached; when a prefab, material or texture they may use changes on disk,
	/// all of them are rendered again (in the editor only - assets do not change in a build).
	/// </summary>
	internal class Icon3DAssetWatcher : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets( string[] _imported, string[] _deleted, string[] _moved, string[] _movedFrom )
		{
			if (UiIcon3DRenderer.RequestCount == 0)
				return;

			foreach (var path in _imported)
			{
				if (IsRelevant(path))
				{
					UiIcon3DRenderer.Invalidate();
					return;
				}
			}
		}

		private static bool IsRelevant( string _path )
		{
			string extension = System.IO.Path.GetExtension(_path).ToLowerInvariant();
			switch (extension)
			{
				case ".prefab":
				case ".mat":
				case ".asset":
				case ".cubemap":
				case ".png":
				case ".jpg":
				case ".tga":
				case ".psd":
				case ".exr":
				case ".hdr":
				case ".fbx":
				case ".obj":
				case ".blend":
				case ".anim":
				case ".controller":
				case ".shader":
					return true;
				default:
					return false;
			}
		}
	}
}
