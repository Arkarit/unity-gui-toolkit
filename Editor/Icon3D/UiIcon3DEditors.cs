using UnityEditor;
using UnityEditor.IMGUI.Controls;
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

	/// <summary>
	/// The bounds of a <see cref="UiIcon3DBoundsHint"/> as a box with drag handles in the scene view (like a
	/// BoxCollider's), so the framing is set by looking at it instead of by typing numbers. Every change, by handle
	/// or in the inspector, renders the icons that show this object again at once: an icon showing a scene object
	/// follows the box live, no save needed.
	/// </summary>
	[CustomEditor(typeof(UiIcon3DBoundsHint))]
	[CanEditMultipleObjects]
	public class UiIcon3DBoundsHintEditor : UnityEditor.Editor
	{
		private static readonly Color s_handleColor = new(1f, 0.6f, 0f, 0.9f);
		private readonly BoxBoundsHandle m_handle = new();

		public override void OnInspectorGUI()
		{
			EditorGUI.BeginChangeCheck();
			DrawDefaultInspector();
			bool changed = EditorGUI.EndChangeCheck();

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

				changed = true;
			}

			EditorGUILayout.HelpBox("Drag the box's handles in the scene view to frame the icon; icons showing this object follow.", MessageType.None);

			if (changed)
				foreach (var t in targets)
					RefreshIconsShowing((UiIcon3DBoundsHint)t);
		}

		private void OnSceneGUI()
		{
			var hint = (UiIcon3DBoundsHint)target;
			if (hint == null || !hint.isActiveAndEnabled)
				return;

			using (new Handles.DrawingScope(s_handleColor, hint.transform.localToWorldMatrix))
			{
				var bounds = hint.LocalBounds;
				m_handle.center = bounds.center;
				m_handle.size = bounds.size;

				EditorGUI.BeginChangeCheck();
				m_handle.DrawHandle();
				if (!EditorGUI.EndChangeCheck())
					return;

				Undo.RecordObject(hint, "Change Icon Bounds");
				hint.LocalBounds = new Bounds(m_handle.center, m_handle.size);
				EditorUtility.SetDirty(hint);
				RefreshIconsShowing(hint);
			}
		}

		/// <summary>
		/// Renders again every icon that shows _hint's object: the icon's object is the hint's GameObject or one of its
		/// parents (the renderer uses the first hint in the object's hierarchy).
		/// </summary>
		public static void RefreshIconsShowing( UiIcon3DBoundsHint _hint )
		{
			if (_hint == null)
				return;

			foreach (var icon in Object.FindObjectsByType<UiIcon3D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
			{
				var shown = icon.Prefab;
				if (shown != null && _hint.transform.IsChildOf(shown.transform))
					icon.Refresh();
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
