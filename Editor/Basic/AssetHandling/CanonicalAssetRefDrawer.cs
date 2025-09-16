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


			// Header row: bold label + gray type info on the same line
			var row = new Rect(_position.x, _position.y, _position.width, kLine);

			// split the header row in two parts
			float labelWidth = EditorGUIUtility.labelWidth;
			var labelRect = new Rect(row.x, row.y, labelWidth, kLine);
			var typeRect = new Rect(labelRect.xMax + 4f, row.y, _position.width - labelWidth - 4f, kLine);

			EditorGUI.LabelField(labelRect, _label, EditorStyles.boldLabel);

			string typeNameToShow = GetDisplayTypeName(typeProp);
			if (!string.IsNullOrEmpty(typeNameToShow))
			{
				var typeContent = EditorGUIUtility.TrTextContent($"{typeNameToShow}", "Logical type saved on this reference");
				EditorGUI.LabelField(typeRect, typeContent, EditorStyles.miniLabel);
			}

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

			foreach (var provider in m_assetProviders)
			{
				DrawProviderRow(ref row, _position, provider, typeProp, idProp);
			}

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
			float labelWidth = EditorGUIUtility.labelWidth;
			var left = new Rect(_row.x, _row.y, labelWidth, kLine);
			var right = new Rect(_row.x + labelWidth + 4f, _row.y, _bounds.width - labelWidth - 4f, kLine);

			string providerResName = string.IsNullOrEmpty(_provider.ResName)
				? _provider.Name
				: _provider.ResName;
			providerResName += ":";

			EditorGUI.LabelField(left, providerResName);

			string currentId = _idProp.stringValue;
			string shownId = BelongsToProvider(_provider, currentId) ? currentId : string.Empty;

			EditorGUI.BeginChangeCheck();
			shownId = EditorGUI.TextField(right, shownId);
			bool textChanged = EditorGUI.EndChangeCheck();

			HandleDragAndDrop(right, _provider, _typeProp, _idProp);

			if (textChanged && !string.IsNullOrEmpty(shownId))
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

			if (BelongsToProvider(_provider, currentId) && string.IsNullOrEmpty(shownId))
				CommitEntry(_typeProp, _idProp, null, null);

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

					if (!SafeValidateId(_provider, id, out var typeName))
					{
						Warn($"Object not valid for provider '{_provider.Name}'.");
					}
					else
					{
						CommitEntry(_typeProp, _idProp, typeName, id);
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
				// Use the object's real type, not GameObject
				var key = _provider.NormalizeKey(_object, _object.GetType());
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
				var loadType = GetExpectedLoadType();

				// Build canonical key with the chosen type
				var key = new CanonicalAssetKey(_provider, _id, loadType);

				// Editor-only: blocking validate is ok here
				var task = _provider.LoadAssetAsync<Object>(key, CancellationToken.None);
				task.Wait();
				var handle = task.Result;
				if (handle == null || !handle.IsLoaded || handle.Asset == null)
					return false;

				var obj = handle.Asset;

				// Validate against constraints (GameObject OR plain asset)
				bool ok = MatchesConstraints(obj, out var matchedName);
				_typeName = matchedName;

				_provider.Release(handle);

				if (!ok)
				{
					Warn("Asset does not meet required component/type constraints.");
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

		// Pick expected load type from constraints; fall back to Object
		private Type GetExpectedLoadType()
		{
			// prefer a single explicit required type
			if (m_requiredTypes != null && m_requiredTypes.Length == 1 && m_requiredTypes[0] != null)
				return m_requiredTypes[0];

			// or a single base class
			if (m_requiredBaseClasses != null && m_requiredBaseClasses.Length == 1 && m_requiredBaseClasses[0] != null)
				return m_requiredBaseClasses[0];

			// unknown -> load as Object, then validate post-load
			return typeof(Object);
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
							_matchedTypeName = t.AssemblyQualifiedName;
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
							_matchedTypeName = t.AssemblyQualifiedName;
							return true;
						}
					}
				}
			}

			return false;
		}

		// Overload for generic Object
		private bool MatchesConstraints( Object _obj, out string _matchedTypeName )
		{
			_matchedTypeName = _obj ? _obj.GetType().AssemblyQualifiedName : nameof(Object);

			bool hasType = m_requiredTypes != null && m_requiredTypes.Length > 0;
			bool hasBase = m_requiredBaseClasses != null && m_requiredBaseClasses.Length > 0;
			if (!hasType && !hasBase)
				return true;

			if (!_obj)
				return false;

			// If it's a GameObject, check its components too
			if (_obj is GameObject go)
				return MatchesConstraints(go, out _matchedTypeName);

			var t = _obj.GetType();

			if (hasType)
				foreach (var rt in m_requiredTypes)
					if (rt != null && rt.IsAssignableFrom(t))
						return true;

			if (hasBase)
				foreach (var rb in m_requiredBaseClasses)
					if (rb != null && rb.IsAssignableFrom(t))
						return true;

			return false;
		}

		private string GetDisplayTypeName( SerializedProperty _typeProp )
		{
			// primary: value stored on the serialized property
			var s = _typeProp != null ? _typeProp.stringValue : null;
			if (!string.IsNullOrEmpty(s))
			{
				var type = Type.GetType(s);
				if (type == null)
					return "<Invalid Type>";
				
				return type.FullName;
			}

			// fallback: show constraints summary when nothing stored yet
			bool hasTypes = m_requiredTypes != null && m_requiredTypes.Length > 0;
			bool hasBases = m_requiredBaseClasses != null && m_requiredBaseClasses.Length > 0;

			if (hasTypes && m_requiredTypes.Length == 1 && m_requiredTypes[0] != null)
				return m_requiredTypes[0].FullName;

			if (hasBases && m_requiredBaseClasses.Length == 1 && m_requiredBaseClasses[0] != null)
				return m_requiredBaseClasses[0].FullName;

			if (hasTypes || hasBases)
				return "<Constrained>";

			return "<Type not set>";
		}

	}
}
#endif
