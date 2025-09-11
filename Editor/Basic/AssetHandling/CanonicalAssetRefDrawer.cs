#if UNITY_EDITOR
using System;
using System.Threading;
using GuiToolkit.Exceptions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	[CustomPropertyDrawer(typeof(CanonicalAssetRef))]
	public class CanonicalAssetRefDrawer : PropertyDrawer
	{
		// layout
		private const float kLine = 18f;
		private const float kPadY = 2f;

		// warnings
		private string m_lastWarning;
		private double m_warningUntil; // EditorApplication.timeSinceStartup deadline
		private IAssetProvider[] m_assetProviders;

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			var typeProp = _property.FindPropertyRelative("Type");
			var idProp = _property.FindPropertyRelative("Id");

			EditorGUI.BeginProperty(_position, _label, _property);

			EnsureConstraints();

			// Title row: shows current logical type
			var row = new Rect(_position.x, _position.y, _position.width, kLine);
			EditorGUI.LabelField(row, new GUIContent(string.IsNullOrEmpty(typeProp.stringValue) ? "<Type not set>" : typeProp.stringValue));
			row.y += kLine + kPadY;

			if (m_assetProviders == null)
			{
				try
				{
					m_assetProviders = AssetManager.AssetProviders;
				}
				catch (NotInitializedException)
				{
					GUIUtility.ExitGUI();
				}
			}

			// Draw one input per provider
			foreach (var provider in m_assetProviders)
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
			var left = new Rect(_row.x, _row.y, labelWidth, kLine);
			var right = new Rect(_row.x + labelWidth + 4f, _row.y, _bounds.width - labelWidth - 4f, kLine);

			string providerResName = string.IsNullOrEmpty(_provider.ResName)
				? _provider.Name
				: _provider.ResName;
			providerResName += ":";

			EditorGUI.LabelField(left, providerResName);

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
		private bool CanMakeIdFromObject( IAssetProvider _provider, Object _object, out string _id )
		{
			_id = null;
			if (!_provider.Supports(_object))
				return false;

			try
			{
				var key = _provider.NormalizeKey<GameObject>(_object);
				_id = key.Id;
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Validate an id by asking the provider to load the prefab and extracting UiPanel type name.
		private bool SafeValidateId( IAssetProvider _provider, string _id, out string _typeName )
		{
			_typeName = null;

			try
			{
				// Build an AssetKey and resolve through provider
				var key = new CanonicalAssetKey(_provider, _id, typeof(GameObject));

				// Synchronously load in editor context
				var task = _provider.LoadAssetAsync<GameObject>(key, CancellationToken.None);
				task.Wait();

				var handle = task.Result;
				if (handle == null || !handle.IsLoaded || handle.Asset == null)
					return false;
				string matchedName;
				var go = handle.Asset;
				bool ok = MatchesConstraints(go, out matchedName);
				_typeName = matchedName;
				_provider.Release(handle);
				if (!ok)
				{
					Warn("Prefab does not meet required component type constraints.");
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

		// constraints (from [CanonicalAssetRef] on field)
		private Type[] m_requiredTypes;
		private Type[] m_requiredBaseClasses;

		private void EnsureConstraints()
		{
			if (m_requiredTypes != null || m_requiredBaseClasses != null)
				return;

			try
			{
				if (fieldInfo == null)
					return;

				var attrs = fieldInfo.GetCustomAttributes(typeof(CanonicalAssetRefAttribute), true);
				if (attrs != null && attrs.Length > 0)
				{
					var a = (CanonicalAssetRefAttribute)attrs[0];
					m_requiredTypes = a.RequiredTypes;
					m_requiredBaseClasses = a.RequiredBaseClasses;
				}
			}
			catch { /* ignore */ }
		}

		private bool MatchesConstraints( GameObject _go, out string _matchedTypeName )
		{
			_matchedTypeName = nameof(GameObject);

			bool hasType = m_requiredTypes != null && m_requiredTypes.Length > 0;
			bool hasBase = m_requiredBaseClasses != null && m_requiredBaseClasses.Length > 0;
			if (!hasType && !hasBase)
				return true; // no constraints -> accept anything

			if (_go == null)
				return false;

			var comps = _go.GetComponents<Component>();
			foreach (var c in comps)
			{
				if (c == null) continue;
				var t = c.GetType();

				if (hasType)
				{
					foreach (var rt in m_requiredTypes)
					{
						if (rt == null) continue;
						if (rt.IsAssignableFrom(t))
						{
							_matchedTypeName = t.Name;
							return true;
						}
					}
				}

				if (hasBase)
				{
					foreach (var rb in m_requiredBaseClasses)
					{
						if (rb == null) continue;
						if (rb.IsAssignableFrom(t))
						{
							_matchedTypeName = t.Name;
							return true;
						}
					}
				}
			}

			return false;
		}

	}
}
#endif
