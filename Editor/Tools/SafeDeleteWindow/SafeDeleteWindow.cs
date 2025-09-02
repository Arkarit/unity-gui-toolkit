// Assets/Editor/SafeDelete/SafeDeleteWindow.cs
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Minimal UI for main-asset-only deletion.
	/// - Stage GameObjects are shown.
	/// - Only main assets (LocalId == 0) are shown; subassets are hidden.
	/// - Black rows = deletable (pre-checked). Red rows = have external referrers.
	/// </summary>
	public class SafeDeleteWindow : EditorWindow
	{
		private Vector2 _scroll;
		private readonly List<RootPanelModel> _models = new List<RootPanelModel>();

//		[MenuItem("Tools/Safe Delete/Analyze Selection")]
		public static void OpenForSelection()
		{
			var wnd = GetWindow<SafeDeleteWindow>("Safe Delete");
			wnd.Rebuild();
			wnd.Show();
		}

		private void OnGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Asset(s) to delete:", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Refresh", GUILayout.Width(80)))
					Rebuild();
			}

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			if (_models.Count == 0)
				EditorGUILayout.HelpBox("Select objects in Project or Hierarchy and click Refresh.", MessageType.Info);

			foreach (var model in _models)
			{
				DrawRootPanel(model);
				GUILayout.Space(12);
			}

			EditorGUILayout.EndScrollView();
		}

		private void Rebuild()
		{
			_models.Clear();
			foreach (var obj in Selection.objects)
			{
				var model = BuildModelForRoot(obj);
				if (model != null) _models.Add(model);
			}
		}

		private static RootPanelModel BuildModelForRoot( Object root )
		{
			if (!root) return null;

			var closure = DependencyUtility.CollectClosure(root);

			var entries = new List<Entry>();
			var seenKeys = new HashSet<string>();

			foreach (var node in closure)
			{
				// Show only stage GOs and main assets (LocalId == 0)
				if (!node.IsSceneObject && node.LocalId != 0)
					continue;

				// Dedup: stage by GOID; assets by GUID
				string key = node.IsSceneObject ? node.Goid.ToString() : node.Guid;
				if (!seenKeys.Add(key))
					continue;

				if (node.IsScript)
				{
					entries.Add(new Entry { Node = node, IsBlocker = true, Checked = false, ForceDisabled = true });
					continue;
				}

				bool isBlocker = DependencyUtility.HasExternalReferrers(node, closure);

				entries.Add(new Entry
				{
					Node = node,
					IsBlocker = isBlocker,
					Checked = !isBlocker,
					ForceDisabled = false
				});
			}

			// Sort: stage first, then assets; then by path/name
			entries.Sort(( a, b ) =>
			{
				int sa = a.Node.IsSceneObject ? 0 : 1;
				int sb = b.Node.IsSceneObject ? 0 : 1;
				int s = sa.CompareTo(sb);
				if (s != 0) return s;
				string la = string.IsNullOrEmpty(a.Node.Path) ? a.Node.Name : a.Node.Path;
				string lb = string.IsNullOrEmpty(b.Node.Path) ? b.Node.Name : b.Node.Path;
				return string.Compare(la, lb, StringComparison.OrdinalIgnoreCase);
			});

			return new RootPanelModel
			{
				RootNode = DependencyUtility.MakeNode(root),
				Entries = entries
			};
		}

		private void DrawRootPanel( RootPanelModel model )
		{
			var box = new GUIStyle(GUI.skin.box) { padding = new RectOffset(10, 10, 6, 6) };
			EditorGUILayout.BeginVertical(box);

			// Header (click to ping)
			using (new EditorGUILayout.HorizontalScope())
			{
				var header = string.IsNullOrEmpty(model.RootNode.Path) ? model.RootNode.Name : model.RootNode.Path;
				var style = new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };
				if (GUILayout.Button(header, style))
				{
					var rootObj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(model.RootNode.Goid);
					if (rootObj) EditorGUIUtility.PingObject(rootObj);
				}
			}

			GUILayout.Space(4);
			EditorGUILayout.LabelField("Dependencies:", EditorStyles.boldLabel);

			foreach (var e in model.Entries)
			{
				string label = BuildRowLabel(e.Node);

				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUI.BeginDisabledGroup(e.ForceDisabled || e.Node.IsScript);

					e.Checked = EditorGUILayout.Toggle(e.Checked, GUILayout.Width(18));

					var style = new GUIStyle(EditorStyles.label);
					if (e.IsBlocker)
						style.normal.textColor = Color.red;

					EditorGUILayout.LabelField(label, style);
					EditorGUI.EndDisabledGroup();
				}
			}

			GUILayout.Space(8);
			EditorGUI.BeginDisabledGroup(!AnyChecked(model));
			if (GUILayout.Button("Destroy Selected", GUILayout.Width(160)))
				ExecuteDestroySelected(model);
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndVertical();
		}

		private static string BuildRowLabel( DependencyNode node )
		{
			string baseText = string.IsNullOrEmpty(node.Path) ? node.Name : node.Path;
			string suffix = node.IsSceneObject ? " (Stage • GameObject)" : "";
			if (node.IsInResources) suffix += " (Resources)";
			return baseText + suffix;
		}

		private static bool AnyChecked( RootPanelModel model )
		{
			foreach (var e in model.Entries)
				if (e.Checked && !e.ForceDisabled && !e.Node.IsScript)
					return true;
			return false;
		}

		private static void ExecuteDestroySelected( RootPanelModel model )
		{
			int risky = 0;
			foreach (var e in model.Entries)
				if (e.Checked && e.IsBlocker) risky++;

			if (risky > 0)
			{
				bool ok = EditorUtility.DisplayDialog(
					"Confirm risky delete",
					$"{risky} selected item(s) appear to be referenced outside the selected closure.\nProceed anyway?",
					"Delete anyway",
					"Cancel");
				if (!ok) return;
			}

			int deleted = 0;

			foreach (var e in model.Entries)
			{
				if (!e.Checked || e.ForceDisabled || e.Node.IsScript)
					continue;

				if (e.Node.IsSceneObject)
				{
					var go = DependencyUtility.ResolveStageGameObject(e.Node.Goid);
					if (go != null)
					{
						Object.DestroyImmediate(go, true); // no Undo
						deleted++;
					}
					continue;
				}
				
				// Only main assets (LocalId == 0)
				if (e.Node.LocalId != 0) continue;

				if (!string.IsNullOrEmpty(e.Node.Path))
				{
					if (AssetDatabase.DeleteAsset(e.Node.Path))
						deleted++;
				}
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog("Safe Delete", $"Deleted {deleted} item(s).", "OK");
		}

		// View model
		private class RootPanelModel
		{
			public DependencyNode RootNode;
			public List<Entry> Entries = new List<Entry>();
		}

		private class Entry
		{
			public DependencyNode Node;
			public bool IsBlocker;
			public bool Checked;
			public bool ForceDisabled; // scripts only in this main-asset-only MVP
		}
	}
}
