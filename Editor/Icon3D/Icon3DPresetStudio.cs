using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Authoring tool for <see cref="UiIcon3DPreset"/>s: a grid of sample objects (rows) rendered with presets
	/// (columns), so a preset is judged on several objects at once and next to its siblings, not on the one object
	/// that happens to be open.
	///
	/// It renders through the same <see cref="UiIcon3DRenderer"/> as every icon - what is seen here is what the game
	/// shows. Changed presets, prefabs, materials and textures render again as soon as they are saved (the renderer
	/// watches imports). Objects and presets are dropped into the window or taken from the selection.
	/// </summary>
	[EditorAware]
	public class Icon3DPresetStudio : EditorWindow
	{
		private enum EBackground
		{
			Checker,
			Light,
			Mid,
			Dark,
		}

		private const float HeaderWidth = 150f;
		private const float HeaderHeight = 22f;
		private const float Gap = 4f;
		private const int MinCell = 64;
		private const int MaxCell = 320;

		[SerializeField] private List<GameObject> m_objects = new();
		[SerializeField] private List<UiIcon3DPreset> m_presets = new();
		[SerializeField] private int m_cellSize = 160;
		[SerializeField] private EBackground m_background = EBackground.Checker;
		[SerializeField] private bool m_sameView;
		[SerializeField] private Vector3 m_viewEuler = new(25, 150, 0);
		[SerializeField] private bool m_initialized;

		private Icon3DHandle[,] m_handles = new Icon3DHandle[0, 0];
		private bool m_dirty = true;
		private Vector2 m_scroll;
		private Texture2D m_checker;
		private Material m_premultiplied;

		[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Preset Studio...")]
		public static Icon3DPresetStudio Open()
		{
			var window = GetWindow<Icon3DPresetStudio>("3D Icon Preset Studio");
			window.minSize = new Vector2(420, 260);
			window.Show();
			return window;
		}

		#region Content (also the test API)

		public IReadOnlyList<GameObject> Objects => m_objects;
		public IReadOnlyList<UiIcon3DPreset> Presets => m_presets;

		/// <summary>Number of cells that currently hold a render request.</summary>
		public int CellCount => m_handles.Cast<Icon3DHandle>().Count(_h => _h != null);

		/// <summary>Number of cells whose icon has been rendered.</summary>
		public int RenderedCount => m_handles.Cast<Icon3DHandle>().Count(_h => _h != null && _h.IsRendered);

		public void SetContent( IEnumerable<GameObject> _objects, IEnumerable<UiIcon3DPreset> _presets )
		{
			m_objects = _objects.Where(_o => _o != null).Distinct().ToList();
			m_presets = _presets.Where(_p => _p != null).Distinct().ToList();
			m_initialized = true;
			m_dirty = true;
		}

		/// <summary>Creates the render requests now instead of on the next repaint.</summary>
		public void Rebuild()
		{
			ReleaseHandles();

			int size = Mathf.Max(16, Mathf.RoundToInt(m_cellSize * EditorGUIUtility.pixelsPerPoint / 16f) * 16);
			Quaternion? view = m_sameView ? Quaternion.Euler(m_viewEuler) : null;

			m_handles = new Icon3DHandle[m_objects.Count, m_presets.Count];
			for (int o = 0; o < m_objects.Count; o++)
			{
				for (int p = 0; p < m_presets.Count; p++)
				{
					if (m_objects[o] == null || m_presets[p] == null)
						continue;

					m_handles[o, p] = UiIcon3DRenderer.RenderStatic(m_objects[o], m_presets[p], new Vector2Int(size, size), view);
					m_handles[o, p].IsVisible = true;
				}
			}

			m_dirty = false;
		}

		public void ReleaseHandles()
		{
			foreach (var handle in m_handles)
				handle?.Release();

			m_handles = new Icon3DHandle[0, 0];
		}

		private bool Add( GameObject _asset )
		{
			if (_asset == null || !EditorUtility.IsPersistent(_asset))
				return false;

			if (_asset.TryGetComponent<UiIcon3DPreset>(out var preset))
			{
				if (m_presets.Contains(preset))
					return false;
				m_presets.Add(preset);
			}
			else
			{
				if (m_objects.Contains(_asset))
					return false;
				m_objects.Add(_asset);
			}

			m_dirty = true;
			return true;
		}

		/// <summary>Looks for preset prefabs by name only: loading every prefab of a big project to look for a component would take minutes.</summary>
		private void AddPresetsByName()
		{
			foreach (var guid in AssetDatabase.FindAssets("Icon3DPreset t:Prefab"))
				Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)));
		}

		/// <summary>The thorough search: every prefab in the project is opened. Slow in a large one, hence a button.</summary>
		private void AddAllPresetsInProject()
		{
			var guids = AssetDatabase.FindAssets("t:Prefab");
			try
			{
				for (int i = 0; i < guids.Length; i++)
				{
					if (i % 50 == 0 && EditorUtility.DisplayCancelableProgressBar("Looking for presets", $"{i} / {guids.Length}", i / (float)guids.Length))
						break;

					Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[i])));
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		#endregion

		#region Window

		private void OnEnable()
		{
			if (!m_initialized)
			{
				AddPresetsByName();
				m_initialized = true;
			}

			m_dirty = true;
			Undo.undoRedoPerformed += Repaint;
		}

		private void OnDisable()
		{
			Undo.undoRedoPerformed -= Repaint;
			ReleaseHandles();

			if (m_checker != null)
				DestroyImmediate(m_checker);
			if (m_premultiplied != null)
				DestroyImmediate(m_premultiplied);
		}

		private void OnGUI()
		{
			DrawToolbar();

			if (m_dirty && Event.current.type == EventType.Layout)
				Rebuild();

			if (m_objects.Count == 0 || m_presets.Count == 0)
			{
				string hint = m_presets.Count == 0
					? "No presets. Drop preset prefabs (with a UiIcon3DPreset) here, select them and press 'Add Selected', or 'Find Presets'."
					: "No sample objects. Drop prefabs or models here, or select them in the Project window and press 'Add Selected'.\n\nUse objects that show what the preset has to handle: something shiny, something transparent, something dark, something tall and something flat.";
				EditorGUILayout.HelpBox(hint, MessageType.Info);
			}
			else
			{
				DrawGrid();
			}

			HandleDrop(new Rect(0, 0, position.width, position.height));
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Button(new GUIContent("Add Selected", "Adds the selected prefabs and models: presets become columns, everything else a row."), EditorStyles.toolbarButton))
				{
					foreach (var go in Selection.GetFiltered<GameObject>(SelectionMode.Assets))
						Add(go);
				}

				if (GUILayout.Button(new GUIContent("Find Presets", "Adds the preset prefabs named Icon3DPreset... (Shift: opens every prefab in the project to look for the component - slow)."), EditorStyles.toolbarButton))
				{
					if (Event.current.shift)
						AddAllPresetsInProject();
					else
						AddPresetsByName();
					m_dirty = true;
				}

				if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
				{
					m_objects.Clear();
					m_presets.Clear();
					m_dirty = true;
				}

				GUILayout.Space(8);
				GUILayout.Label("Size", EditorStyles.miniLabel);
				int cell = (int)GUILayout.HorizontalSlider(m_cellSize, MinCell, MaxCell, GUILayout.Width(90));
				if (cell != m_cellSize)
				{
					m_cellSize = cell;
					m_dirty = true;
				}

				GUILayout.Space(8);
				m_background = (EBackground)EditorGUILayout.EnumPopup(m_background, EditorStyles.toolbarPopup, GUILayout.Width(70));

				GUILayout.Space(8);
				bool sameView = GUILayout.Toggle(m_sameView, new GUIContent("Same view", "Renders every preset from the same view rotation instead of its own, to compare lighting alone."), EditorStyles.toolbarButton);
				if (sameView != m_sameView)
				{
					m_sameView = sameView;
					m_dirty = true;
				}

				if (m_sameView)
				{
					EditorGUI.BeginChangeCheck();
					m_viewEuler = EditorGUILayout.Vector3Field(GUIContent.none, m_viewEuler, GUILayout.Width(190));
					if (EditorGUI.EndChangeCheck())
						m_dirty = true;
				}

				GUILayout.FlexibleSpace();
				if (GUILayout.Button(new GUIContent("Refresh", "Renders everything again."), EditorStyles.toolbarButton))
					UiIcon3DRenderer.Invalidate();
			}
		}

		private void DrawGrid()
		{
			float cell = m_cellSize;
			float width = HeaderWidth + m_presets.Count * (cell + Gap);
			float height = HeaderHeight + m_objects.Count * (cell + Gap);

			m_scroll = GUI.BeginScrollView(new Rect(0, EditorStyles.toolbar.fixedHeight, position.width, position.height - EditorStyles.toolbar.fixedHeight),
				m_scroll, new Rect(0, 0, width, height));

			// Column headers: presets
			for (int p = 0; p < m_presets.Count; p++)
			{
				var rect = new Rect(HeaderWidth + p * (cell + Gap), 0, cell, HeaderHeight);
				if (DrawHeader(rect, m_presets[p], Describe(m_presets[p])))
				{
					m_presets.RemoveAt(p);
					m_dirty = true;
					GUIUtility.ExitGUI();
				}
			}

			// Rows: objects
			for (int o = 0; o < m_objects.Count; o++)
			{
				float y = HeaderHeight + o * (cell + Gap);
				if (DrawHeader(new Rect(0, y, HeaderWidth - Gap, HeaderHeight), m_objects[o], m_objects[o] != null ? AssetDatabase.GetAssetPath(m_objects[o]) : ""))
				{
					m_objects.RemoveAt(o);
					m_dirty = true;
					GUIUtility.ExitGUI();
				}

				for (int p = 0; p < m_presets.Count; p++)
					DrawCell(new Rect(HeaderWidth + p * (cell + Gap), y, cell, cell), o, p);
			}

			GUI.EndScrollView();
		}

		/// <summary>Name button (click pings, double click opens) with a remove button; returns true when removed.</summary>
		private static bool DrawHeader( Rect _rect, Object _asset, string _tooltip )
		{
			var nameRect = new Rect(_rect.x, _rect.y, _rect.width - 18, _rect.height);
			var removeRect = new Rect(_rect.xMax - 18, _rect.y, 18, _rect.height);

			string label = _asset != null ? _asset.name : "(missing)";
			if (GUI.Button(nameRect, new GUIContent(label, _tooltip), EditorStyles.miniButton) && _asset != null)
			{
				if (Event.current.clickCount >= 2)
					AssetDatabase.OpenAsset(_asset);
				else
					EditorGUIUtility.PingObject(_asset);
			}

			return GUI.Button(removeRect, new GUIContent("x", "Remove from the studio"), EditorStyles.miniButtonRight);
		}

		private void DrawCell( Rect _rect, int _object, int _preset )
		{
			DrawBackground(_rect);

			var handle = _object < m_handles.GetLength(0) && _preset < m_handles.GetLength(1) ? m_handles[_object, _preset] : null;
			if (handle != null && handle.IsRendered && handle.Texture != null)
			{
				if (m_premultiplied == null)
				{
					var shader = Shader.Find("UIToolkit/UI_Icon3D");
					m_premultiplied = shader != null ? new Material(shader) { hideFlags = HideFlags.HideAndDontSave } : null;
				}

				if (Event.current.type == EventType.Repaint)
				{
					if (m_premultiplied != null)
						EditorGUI.DrawPreviewTexture(_rect, handle.Texture, m_premultiplied, ScaleMode.ScaleToFit);
					else
						GUI.DrawTexture(_rect, handle.Texture, ScaleMode.ScaleToFit, true);
				}
			}
			else
			{
				GUI.Label(_rect, handle == null ? "-" : "...", EditorStyles.centeredGreyMiniLabel);
			}
		}

		private void DrawBackground( Rect _rect )
		{
			if (Event.current.type != EventType.Repaint)
				return;

			switch (m_background)
			{
				case EBackground.Checker:
					if (m_checker == null)
					{
						m_checker = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
						var light = new Color(0.62f, 0.62f, 0.62f);
						var dark = new Color(0.5f, 0.5f, 0.5f);
						m_checker.SetPixels(new[] { light, dark, dark, light });
						m_checker.Apply();
					}

					GUI.DrawTextureWithTexCoords(_rect, m_checker, new Rect(0, 0, _rect.width / 16f, _rect.height / 16f));
					break;
				case EBackground.Light:
					EditorGUI.DrawRect(_rect, new Color(0.85f, 0.85f, 0.85f));
					break;
				case EBackground.Mid:
					EditorGUI.DrawRect(_rect, new Color(0.45f, 0.45f, 0.45f));
					break;
				default:
					EditorGUI.DrawRect(_rect, new Color(0.12f, 0.12f, 0.12f));
					break;
			}
		}

		/// <summary>One line of what the preset does, for the column header's tooltip.</summary>
		private static string Describe( UiIcon3DPreset _preset )
		{
			if (_preset == null)
				return "";

			var shadow = _preset.ShadowCatcher;
			return $"{AssetDatabase.GetAssetPath(_preset)}\nView {_preset.ViewRotation.eulerAngles}, {_preset.Projection}, FOV {_preset.FieldOfView}, fit {_preset.FitMode}, padding {_preset.Padding}\n" +
				$"Alpha {_preset.AlphaMode}, shadow {(shadow.Enabled ? shadow.Strength.ToString("F2") : "off")}";
		}

		private void HandleDrop( Rect _area )
		{
			var current = Event.current;
			if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform)
				return;

			if (!_area.Contains(current.mousePosition) || !DragAndDrop.objectReferences.Any(_o => _o is GameObject go && EditorUtility.IsPersistent(go)))
				return;

			DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
			if (current.type == EventType.DragPerform)
			{
				DragAndDrop.AcceptDrag();
				foreach (var dropped in DragAndDrop.objectReferences)
					if (dropped is GameObject go)
						Add(go);
			}

			current.Use();
		}

		#endregion
	}
}
