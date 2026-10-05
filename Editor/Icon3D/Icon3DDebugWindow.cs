using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// What the 3D icon renderer holds right now: every distinct icon with its size, memory, users and state, the
	/// other managed render textures, and what the last ticks rendered. For answering "why is this icon blank",
	/// "why is that one rendered twice" and "where does the memory go" without a debugger.
	/// </summary>
	[EditorAware]
	public class Icon3DDebugWindow : EditorWindow
	{
		private enum ESort
		{
			Memory,
			Name,
			State,
		}

		private const double RefreshInterval = 0.25;

		private readonly List<Icon3DInfo> m_icons = new();
		private readonly List<RenderTextureInfo> m_otherTextures = new();
		private Icon3DSummary m_summary;
		private double m_lastRefresh;
		private Vector2 m_scroll;
		[SerializeField] private ESort m_sort = ESort.Memory;
		[SerializeField] private string m_filter = "";

		[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Debug View...")]
		public static Icon3DDebugWindow Open()
		{
			var window = GetWindow<Icon3DDebugWindow>("3D Icon Debug");
			window.minSize = new Vector2(520, 240);
			window.Show();
			return window;
		}

		/// <summary>The data the window shows, as text: one line per icon. Also what the "Copy" button puts on the clipboard.</summary>
		public static string BuildReport()
		{
			var icons = new List<Icon3DInfo>();
			var others = new List<RenderTextureInfo>();
			var summary = Icon3DDiagnostics.Collect(icons, others);

			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"3D icons: {summary.Requests} requests ({summary.Pending} pending, {summary.Animated} animated, {summary.Failed} failed), " +
				$"{summary.Textures} textures, {Icon3DDiagnostics.FormatBytes(summary.TotalBytes)} (icons {Icon3DDiagnostics.FormatBytes(summary.IconBytes)})");
			sb.AppendLine($"Renders: last tick {summary.LastTickRenders}, peak {summary.PeakTickRenders}, total {summary.TotalRenders}; static last tick {summary.LastStaticRenderMilliseconds:F2} ms, " +
				$"{summary.DeferredByTimeBudget} deferred by time. Budget {summary.RendersPerFrame} renders / {summary.RenderMilliseconds:F1} ms, MSAA {summary.MsaaSamples}x, layer {summary.Layer}");
			foreach (var icon in icons.OrderByDescending(_i => _i.Bytes))
			{
				sb.AppendLine($"{(icon.Prefab != null ? icon.Prefab.name : "(missing)")} | {(icon.Preset != null ? icon.Preset.name : "default")} | {icon.Size.x}x{icon.Size.y} | " +
					$"{Icon3DDiagnostics.FormatBytes(icon.Bytes)} | refs {icon.References} (shown {icon.VisibleReferences}) | {State(icon)}");
			}

			foreach (var other in others)
				sb.AppendLine($"[other] {other.Key} | {other.Width}x{other.Height} {other.Format} | {Icon3DDiagnostics.FormatBytes(other.Bytes)} | producers {other.Producers}, consumers {other.Consumers}");

			return sb.ToString();
		}

		private static string State( Icon3DInfo _icon )
		{
			var parts = new List<string>();
			if (_icon.HasFailed)
				parts.Add("FAILED");
			else if (_icon.IsDirty)
				parts.Add("pending");
			else if (_icon.IsRendered)
				parts.Add("rendered");
			else
				parts.Add("no image");

			if (_icon.IsAnimated)
				parts.Add(_icon.IsPlaying ? "animated" : "frozen");
			if (_icon.ExactAlpha)
				parts.Add("2 renders");
			return string.Join(", ", parts);
		}

		private void OnEnable()
		{
			EditorApplication.update += Tick;
			Refresh();
		}

		private void OnDisable() => EditorApplication.update -= Tick;

		private void Tick()
		{
			if (EditorApplication.timeSinceStartup - m_lastRefresh < RefreshInterval)
				return;

			Refresh();
			Repaint();
		}

		private void Refresh()
		{
			m_lastRefresh = EditorApplication.timeSinceStartup;
			m_summary = Icon3DDiagnostics.Collect(m_icons, m_otherTextures);
		}

		private void OnGUI()
		{
			DrawToolbar();
			DrawSummary();

			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
			DrawIcons();
			DrawOthers();
			EditorGUILayout.EndScrollView();
		}

		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				GUILayout.Label("Sort", EditorStyles.miniLabel);
				m_sort = (ESort)EditorGUILayout.EnumPopup(m_sort, EditorStyles.toolbarPopup, GUILayout.Width(80));
				GUILayout.Space(6);
				m_filter = EditorGUILayout.TextField(m_filter, EditorStyles.toolbarSearchField, GUILayout.MinWidth(120));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(new GUIContent("Reset Peak", "Forget the peak of renders per tick."), EditorStyles.toolbarButton))
					UiIcon3DRenderer.ResetPeakTickRenders();
				if (GUILayout.Button(new GUIContent("Render Again", "Marks every icon for a new render."), EditorStyles.toolbarButton))
					UiIcon3DRenderer.Invalidate();
				if (GUILayout.Button(new GUIContent("Copy", "Copies the list as text, e.g. for a bug report."), EditorStyles.toolbarButton))
					EditorGUIUtility.systemCopyBuffer = BuildReport();
			}
		}

		private void DrawSummary()
		{
			var s = m_summary;
			EditorGUILayout.LabelField($"{s.Requests} icons ({s.Pending} pending, {s.Animated} animated, {s.Failed} failed)   " +
				$"{s.Textures} textures, {Icon3DDiagnostics.FormatBytes(s.TotalBytes)}", EditorStyles.boldLabel);
			EditorGUILayout.LabelField($"Renders: last tick {s.LastTickRenders}, peak {s.PeakTickRenders}, total {s.TotalRenders}   |   " +
				$"static {s.LastStaticRenderMilliseconds:F2} ms, {s.DeferredByTimeBudget} deferred by time   |   " +
				$"budget {s.RendersPerFrame} / {s.RenderMilliseconds:F1} ms   |   MSAA {s.MsaaSamples}x   |   layer {s.Layer}", EditorStyles.miniLabel);

			if (s.Failed > 0)
				EditorGUILayout.HelpBox("Some icons failed to render and are not retried until they are marked dirty. See the console for the reason.", MessageType.Warning);
		}

		private void DrawIcons()
		{
			if (m_icons.Count == 0)
			{
				EditorGUILayout.HelpBox("No 3D icons are alive. Icons exist while a UiIcon3D is enabled or a handle is held.", MessageType.Info);
				return;
			}

			IEnumerable<Icon3DInfo> rows = m_icons;
			if (!string.IsNullOrEmpty(m_filter))
			{
				rows = rows.Where(_i => (_i.Prefab != null && _i.Prefab.name.IndexOf(m_filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
					|| (_i.Preset != null && _i.Preset.name.IndexOf(m_filter, System.StringComparison.OrdinalIgnoreCase) >= 0));
			}

			switch (m_sort)
			{
				case ESort.Name:
					rows = rows.OrderBy(_i => _i.Prefab != null ? _i.Prefab.name : "");
					break;
				case ESort.State:
					rows = rows.OrderByDescending(_i => _i.HasFailed).ThenByDescending(_i => _i.IsDirty).ThenBy(_i => _i.Prefab != null ? _i.Prefab.name : "");
					break;
				default:
					rows = rows.OrderByDescending(_i => _i.Bytes);
					break;
			}

			DrawHeaderRow("Object", "Preset", "Size", "Memory", "Refs", "State");
			foreach (var icon in rows)
			{
				var previous = GUI.color;
				if (icon.HasFailed)
					GUI.color = new Color(1f, 0.55f, 0.55f);
				else if (icon.IsDirty)
					GUI.color = new Color(1f, 0.9f, 0.5f);

				using (new EditorGUILayout.HorizontalScope())
				{
					DrawAssetCell(icon.Prefab, icon.Prefab != null ? icon.Prefab.name : "(missing)", 170);
					DrawAssetCell(icon.Preset, icon.Preset != null ? icon.Preset.name : "default", 150);
					GUILayout.Label($"{icon.Size.x}x{icon.Size.y}", GUILayout.Width(70));
					GUILayout.Label(Icon3DDiagnostics.FormatBytes(icon.Bytes), GUILayout.Width(70));
					GUILayout.Label(new GUIContent($"{icon.References} ({icon.VisibleReferences})", "References (of which shown). Identical icons share one render."), GUILayout.Width(60));
					GUILayout.Label(State(icon));
				}

				GUI.color = previous;
			}
		}

		private void DrawOthers()
		{
			if (m_otherTextures.Count == 0)
				return;

			GUILayout.Space(8);
			EditorGUILayout.LabelField("Other managed render textures", EditorStyles.boldLabel);
			foreach (var other in m_otherTextures.OrderByDescending(_t => _t.Bytes))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label(other.Key, GUILayout.Width(250));
					GUILayout.Label(other.HasTexture ? $"{other.Width}x{other.Height} {other.Format}" : "no texture", GUILayout.Width(140));
					GUILayout.Label(Icon3DDiagnostics.FormatBytes(other.Bytes), GUILayout.Width(70));
					GUILayout.Label($"producers {other.Producers}, consumers {other.Consumers}");
				}
			}
		}

		private static void DrawHeaderRow( params string[] _columns )
		{
			float[] widths = { 170, 150, 70, 70, 60, 0 };
			using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				for (int i = 0; i < _columns.Length; i++)
				{
					if (widths[i] > 0)
						GUILayout.Label(_columns[i], EditorStyles.miniBoldLabel, GUILayout.Width(widths[i]));
					else
						GUILayout.Label(_columns[i], EditorStyles.miniBoldLabel);
				}
			}
		}

		/// <summary>Name as a button: click pings the asset, double click opens it.</summary>
		private static void DrawAssetCell( Object _asset, string _label, float _width )
		{
			if (GUILayout.Button(new GUIContent(_label, _asset != null ? AssetDatabase.GetAssetPath(_asset) : ""), EditorStyles.linkLabel, GUILayout.Width(_width)) && _asset != null)
			{
				if (Event.current.clickCount >= 2)
					AssetDatabase.OpenAsset(_asset);
				else
					EditorGUIUtility.PingObject(_asset);
			}
		}
	}
}
