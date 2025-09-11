#if UNITY_EDITOR
using System;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using GuiToolkit.AssetHandling;
using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	[CustomPropertyDrawer(typeof(UiPanelConfig.PanelEntry))]
	public class UiPanelConfigEntryDrawer : PropertyDrawer
	{
		// layout
		private const float kLine = 18f;
		private const float kPadY = 2f;

		// warnings
		private string m_lastWarning;
		private double m_warningUntil; // EditorApplication.timeSinceStartup deadline

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			var typeProp = _property.FindPropertyRelative("Type");
			var idProp   = _property.FindPropertyRelative("PanelId");

			EditorGUI.BeginProperty(_position, _label, _property);

			// Title row: shows current logical type
			var row = new Rect(_position.x, _position.y, _position.width, kLine);
			EditorGUI.LabelField(row, new GUIContent(string.IsNullOrEmpty(typeProp.stringValue) ? "<Type not set>" : typeProp.stringValue));
			row.y += kLine + kPadY;

			// Get providers from manager (safe guard)
			IAssetProvider[] providers = Array.Empty<IAssetProvider>();
			try
			{
				providers = AssetManager.AssetProviders;
			}
			catch (Exception ex)
			{
				DrawWarning(ref row, _position, "AssetManager not initialized: " + ex.Message);
				EditorGUI.EndProperty();
				return;
			}

			// Draw one input per provider
			foreach (var provider in providers)
			{
				DrawProviderRow(ref row, _position, provider, typeProp, idProp);
			}

			// Optional warning below
			if (!string.IsNullOrEmpty(m_lastWarning) && EditorApplication.timeSinceStartup < m_warningUntil)
			{
				DrawWarning(ref row, _position, m_lastWarning);
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight( SerializedProperty _property, GUIContent _label )
		{
			int lines = 1; // header
			try
			{
				lines += Math.Max(1, AssetManager.AssetProviders.Length);
			}
			catch
			{
				lines += 1;
			}

			// extra line for warning when active
			if (!string.IsNullOrEmpty(m_lastWarning) && EditorApplication.timeSinceStartup < m_warningUntil)
				lines += 2;

			return lines * (kLine + kPadY);
		}

		private void DrawProviderRow
		(
			ref Rect _row,
			Rect _bounds,
			IAssetProvider _provider,
			SerializedProperty _typeProp,
			SerializedProperty _idProp
		)
		{
			// Label (provider name + ResName)
			float labelWidth = Mathf.Min(160f, _bounds.width * 0.35f);
			var left  = new Rect(_row.x, _row.y, labelWidth, kLine);
			var right = new Rect(_row.x + labelWidth + 4f, _row.y, _bounds.width - labelWidth - 4f, kLine);

			string providerLabel = string.IsNullOrEmpty(_provider.ResName)
				? _provider.Name
				: $"{_provider.Name} ({_provider.ResName})";

			EditorGUI.LabelField(left, providerLabel);

			// Prefill with current id only if it belongs to this provider
			string currentId = _idProp.stringValue;
			string shownId = BelongsToProvider(_provider, currentId) ? currentId : string.Empty;

			EditorGUI.BeginChangeCheck();
			shownId = EditorGUI.TextField(right, shownId);
			bool textChanged = EditorGUI.EndChangeCheck();

			// Drag and drop over the text field
			HandleDragAndDrop(right, _provider, _typeProp, _idProp);

			// When the user edited this provider's field, commit and validate
			if (textChanged)
			{
				if (!string.IsNullOrEmpty(shownId))
				{
					if (!SafeValidateId(_provider, shownId, out var panelTypeName))
					{
						Warn($"Invalid id for provider '{_provider.Name}': {shownId}");
					}
					else
					{
						CommitEntry(_typeProp, _idProp, panelTypeName, shownId);
					}
				}
				else
				{
					// Clearing this field clears the mapping if it belonged here
					if (BelongsToProvider(_provider, currentId))
						CommitEntry(_typeProp, _idProp, null, null);
				}
			}

			_row.y += kLine + kPadY;
		}

		private void HandleDragAndDrop
		(
			Rect _rect,
			IAssetProvider _provider,
			SerializedProperty _typeProp,
			SerializedProperty _idProp
		)
		{
			var evt = Event.current;
			if (evt == null)
				return;

			if (!(_rect.Contains(evt.mousePosition)))
				return;

			if (evt.type == EventType.DragUpdated)
			{
				var o = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0
					? DragAndDrop.objectReferences[0]
					: null;

				if (o != null && CanMakeIdFromObject(_provider, o, out _))
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					Event.current.Use();
				}
			}
			else if (evt.type == EventType.DragPerform)
			{
				var o = DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0
					? DragAndDrop.objectReferences[0]
					: null;

				if (o != null && CanMakeIdFromObject(_provider, o, out var id))
				{
					DragAndDrop.AcceptDrag();

					if (!SafeValidateId(_provider, id, out var panelTypeName))
					{
						Warn($"Object not valid for provider '{_provider.Name}'.");
					}
					else
					{
						CommitEntry(_typeProp, _idProp, panelTypeName, id);
					}

					Event.current.Use();
				}
			}
		}

		private void CommitEntry( SerializedProperty _typeProp, SerializedProperty _idProp, string _panelTypeName, string _id )
		{
			_idProp.serializedObject.Update();

			_idProp.stringValue = _id ?? string.Empty;
			_typeProp.stringValue = _panelTypeName ?? string.Empty;

			_idProp.serializedObject.ApplyModifiedProperties();
		}

		private bool BelongsToProvider( IAssetProvider _provider, string _id )
		{
			if (string.IsNullOrEmpty(_id))
				return false;

			try
			{
				return _provider.Supports(_id);
			}
			catch
			{
				return false;
			}
		}

		private void Warn( string _msg )
		{
			m_lastWarning = _msg;
			m_warningUntil = EditorApplication.timeSinceStartup + 4.0; // show for 4 seconds
		}

		private void DrawWarning( ref Rect _row, Rect _bounds, string _msg )
		{
			var r = new Rect(_row.x, _row.y, _bounds.width, kLine * 2f);
			EditorGUI.HelpBox(r, _msg, MessageType.Warning);
			_row.y += r.height + kPadY;
		}

		// Turn a dragged UnityEngine.Object into a provider-specific id string if possible.
		// For Default provider: compute "res:" path. For other providers: only allow if provider.Supports(obj) returns true
		// and NormalizeKey(obj) can produce a stable id (if your provider supports that). Otherwise return false.
		private bool CanMakeIdFromObject( IAssetProvider _provider, Object _o, out string _id )
		{
			_id = null;

			// Default provider: accept prefabs in Resources and build "res:" id
			if (_provider is DefaultAssetProvider)
			{
				if (!TryMakeResId(_o, out _id))
				{
					Warn("Asset must be inside a Resources folder for Default provider.");
					return false;
				}
				return true;
			}

			// Other providers: ask Supports(object). If false, do not accept DnD.
			// If true, try to normalize and take the canonical id (this relies on provider NormalizeKey supporting object).
			try
			{
				if (!_provider.Supports(_o))
					return false;

				// We try GameObject as panels are prefabs.
				var key = _provider.NormalizeKey<GameObject>(_o);
				_id = key.Id;
				return true;
			}
			catch
			{
				// If provider cannot normalize raw objects, we do not support DnD for it.
				return false;
			}
		}

		// Validate an id by asking the provider to load the prefab and extracting UiPanel type name.
		private bool SafeValidateId( IAssetProvider _provider, string _id, out string _panelTypeName )
		{
			_panelTypeName = null;
			try
			{
				// Build an AssetKey and resolve through provider
				var key = new AssetKey(_provider, _id, typeof(GameObject));

				// Synchronously load in editor context
				var task = _provider.LoadAssetAsync<GameObject>(key, CancellationToken.None);
				task.Wait();

				var handle = task.Result;
				if (handle == null || !handle.IsLoaded || handle.Asset == null)
					return false;

				var go = handle.Asset;
				var panel = go != null ? go.GetComponent<UiPanel>() : null;
				_panelTypeName = panel != null ? panel.GetType().Name : string.Empty;

				// Release via provider (even if default no-op)
				_provider.Release(handle);

				// Accept also when no UiPanel found? If not, treat as invalid.
				if (panel == null)
				{
					Warn("Prefab does not contain a UiPanel component.");
					return false;
				}

				return true;
			}
			catch (Exception ex)
			{
				Warn(ex.Message);
				return false;
			}
		}

		// Build a "res:" id from a dragged asset if it is under a Resources folder.
		private static bool TryMakeResId( Object _o, out string _id )
		{
			_id = null;
			if (_o == null)
				return false;

			var path = AssetDatabase.GetAssetPath(_o);
			if (string.IsNullOrEmpty(path))
				return false;

			// Must contain "/Resources/"
			int idx = path.IndexOf("/Resources/", StringComparison.Ordinal);
			if (idx < 0)
				return false;

			int start = idx + "/Resources/".Length;
			int len = path.Length - start;
			if (len <= 0)
				return false;

			// strip extension
			string sub = path.Substring(start);
			int dot = sub.LastIndexOf('.');
			if (dot >= 0)
				sub = sub.Substring(0, dot);

			_id = sub.StartsWith("res:", StringComparison.Ordinal) ? sub : "res:" + sub;
			return true;
		}
	}
}
#endif
