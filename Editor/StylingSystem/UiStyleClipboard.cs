using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Copies one style's values, and pastes them into another style of the same type.
	///
	/// It travels through <see cref="GUIUtility.systemCopyBuffer"/> as JSON rather than through a static
	/// field. A static one is simpler and would keep object references perfectly - and it would also be
	/// empty most of the time, because every script recompile wipes it. Copying a style and then touching
	/// a source file must not lose the copy. Text also crosses project boundaries, which is the case this
	/// is really for: reading a value in the toolkit's own dev app and putting it into a client.
	///
	/// The price of text is that a Sprite or a Material cannot travel by value. Those are written as asset
	/// GUID plus local id, so they resolve in any project that HAS the asset and come back empty in one
	/// that does not - which is honest, and better than an instance id that silently points at something
	/// else after the next domain reload.
	///
	/// Values are matched BY NAME, never by position. A style type that gained a value since the copy was
	/// made still pastes correctly, and the ones that have no counterpart are left alone and counted.
	/// </summary>
	public static class UiStyleClipboard
	{
		private const string MarkerKey = "uitk";
		private const string MarkerValue = "style";
		private const int Version = 1;

		/// <summary>An array's elements, wrapped so an array is distinguishable from a nested struct.</summary>
		private const string ArrayKey = "$array";

		/// <summary>What the clipboard currently holds, as far as this is concerned.</summary>
		public readonly struct Content
		{
			public Content( string _styleTypeName, string _componentTypeName, string _alias, int _valueCount )
			{
				StyleTypeName = _styleTypeName;
				ComponentTypeName = _componentTypeName;
				Alias = _alias;
				ValueCount = _valueCount;
			}

			public bool IsValid => !string.IsNullOrEmpty(StyleTypeName);

			/// <summary>Full name of the style class, which is what "same type" is decided on.</summary>
			public string StyleTypeName { get; }

			/// <summary>The component the copied style styles - for saying what is in the clipboard.</summary>
			public string ComponentTypeName { get; }

			/// <summary>What the copied style was called where it came from. Display only.</summary>
			public string Alias { get; }

			public int ValueCount { get; }
		}

		// Parsing the same clipboard string again on every menu build would be wasted work, and reading the
		// system clipboard is a native call. Both are cheap enough once per click and not worth doing twice.
		private static string s_parsedFrom;
		private static JObject s_parsed;

		/// <summary>
		/// What is in the clipboard, or an invalid Content when it holds something that is not a style.
		///
		/// Anything at all can be in there - a file path, a line of code, the last thing copied out of a
		/// browser - so this never throws and never guesses. Only a document carrying this class's own
		/// marker counts.
		/// </summary>
		public static Content Peek()
		{
			var root = Parse(GUIUtility.systemCopyBuffer);
			if (root == null)
				return default;

			return new Content
			(
				(string)root["styleType"],
				(string)root["componentType"],
				(string)root["alias"],
				root["values"] is JObject values ? values.Count : 0
			);
		}

		/// <summary>
		/// Whether the clipboard can be pasted into this style, and if not, why - phrased to go straight
		/// into a disabled menu entry, where there is nowhere else to put the reason.
		/// </summary>
		public static bool CanPasteInto( UiAbstractStyleBase _style, out string _reason )
		{
			var content = Peek();
			if (!content.IsValid)
			{
				_reason = "nothing copied yet";
				return false;
			}

			if (_style == null)
			{
				_reason = "no style";
				return false;
			}

			if (_style.GetType().FullName != content.StyleTypeName)
			{
				_reason = $"the clipboard holds a {ShortName(content.ComponentTypeName)} style";
				return false;
			}

			_reason = null;
			return true;
		}

		/// <summary>Puts a style's values into the clipboard. Reading is always allowed, inherited or not.</summary>
		public static void Copy( SerializedProperty _styleProp, UiAbstractStyleBase _style )
		{
			if (_styleProp == null || _style == null)
				return;

			var values = new JObject();
			foreach (var valueProp in ValueProperties(_styleProp))
			{
				// A value that was never created has nothing to copy - see RepairMissingStyleValues.
				if (valueProp.managedReferenceValue == null)
					continue;

				var applicableProp = valueProp.FindPropertyRelative("IsApplicable");
				var innerProp = valueProp.FindPropertyRelative("m_value");
				if (applicableProp == null || innerProp == null)
					continue;

				var encoded = Encode(innerProp);
				if (encoded == null)
					continue;

				values[valueProp.name] = new JObject
				{
					["applicable"] = applicableProp.boolValue,
					["value"] = encoded,
				};
			}

			var root = new JObject
			{
				[MarkerKey] = MarkerValue,
				["version"] = Version,
				["styleType"] = _style.GetType().FullName,
				["componentType"] = _style.SupportedComponentType?.FullName,
				["alias"] = _style.Alias,
				["values"] = values,
			};

			GUIUtility.systemCopyBuffer = root.ToString(Newtonsoft.Json.Formatting.Indented);
			UiLog.Log($"Copied {values.Count} values of style '{_style.Alias}' "
				+ $"({ShortName(_style.SupportedComponentType?.FullName)}).");
		}

		/// <summary>
		/// Writes the clipboard's values into this style. Returns how many arrived.
		///
		/// Through the SerializedProperty rather than through the object, which is what puts the whole paste
		/// in one undo step and makes it show up without the inspector having to be told.
		/// </summary>
		public static int Paste( SerializedProperty _styleProp, UiAbstractStyleBase _style )
		{
			if (_styleProp == null || !CanPasteInto(_style, out _))
				return 0;

			var root = Parse(GUIUtility.systemCopyBuffer);
			if (root?["values"] is not JObject values)
				return 0;

			int written = 0;
			var skipped = new List<string>();

			foreach (var valueProp in ValueProperties(_styleProp))
			{
				if (valueProp.managedReferenceValue == null)
					continue;

				// No counterpart in the clipboard: left exactly as it was. A style that gained values since
				// the copy keeps them rather than having them cleared by something that never knew them.
				if (values[valueProp.name] is not JObject entry)
					continue;

				var applicableProp = valueProp.FindPropertyRelative("IsApplicable");
				var innerProp = valueProp.FindPropertyRelative("m_value");
				if (applicableProp == null || innerProp == null)
					continue;

				if (!Decode(innerProp, entry["value"]))
				{
					skipped.Add(valueProp.name);
					continue;
				}

				// After the value, not before: the applicable flag is what decides whether the style says
				// anything at all about this property, and it should only flip once the value behind it is
				// the copied one.
				applicableProp.boolValue = entry["applicable"]?.Value<bool>() ?? false;
				written++;
			}

			if (written > 0)
				_styleProp.serializedObject.ApplyModifiedProperties();

			string message = $"Pasted {written} values into style '{_style.Alias}'.";
			if (skipped.Count > 0)
			{
				message += $" {skipped.Count} could not be transferred and were left alone: "
					+ string.Join(", ", skipped) + ".";
				UiLog.LogWarning(message);
			}
			else
			{
				UiLog.Log(message);
			}

			return written;
		}

		#region Reading the document

		private static JObject Parse( string _text )
		{
			if (string.IsNullOrEmpty(_text))
			{
				s_parsedFrom = null;
				s_parsed = null;
				return null;
			}

			if (_text == s_parsedFrom)
				return s_parsed;

			s_parsedFrom = _text;
			s_parsed = null;

			// A clipboard holds whatever the user last copied, anywhere. Everything below is a rejection,
			// not an error: a path, a sentence, another tool's JSON all simply mean "nothing to paste".
			if (_text.Length < 2 || _text[0] != '{')
				return null;

			try
			{
				var root = JObject.Parse(_text);
				if ((string)root[MarkerKey] != MarkerValue)
					return null;

				if (root["version"]?.Value<int>() != Version)
					return null;

				s_parsed = root;
			}
			catch
			{
				// Deliberately silent: this runs whenever a menu is opened, and the normal case for a
				// clipboard that fails to parse is that it holds something else entirely.
			}

			return s_parsed;
		}

		/// <summary>The style's own value fields - not what is inside them.</summary>
		private static IEnumerable<SerializedProperty> ValueProperties( SerializedProperty _styleProp )
		{
			var iterator = _styleProp.Copy();
			var end = _styleProp.GetEndProperty();
			int depth = _styleProp.depth;

			while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
			{
				if (iterator.depth == depth + 1
				 && iterator.propertyType == SerializedPropertyType.ManagedReference)
				{
					yield return iterator.Copy();
				}
			}
		}

		private static string ShortName( string _fullName )
		{
			if (string.IsNullOrEmpty(_fullName))
				return "?";

			int dot = _fullName.LastIndexOf('.');
			return dot < 0 ? _fullName : _fullName.Substring(dot + 1);
		}

		#endregion

		#region Value encoding

		/// <summary>
		/// A value as JSON, or null when this type has no text form here.
		///
		/// Recursive, so a nested struct (EdgeGap, RectOffset) needs nothing of its own - it is walked like
		/// the style itself. A type that falls through lands in the "skipped" list on paste and is named
		/// there, because a value that quietly does not arrive is worse than one that says it did not.
		/// </summary>
		private static JToken Encode( SerializedProperty _property )
		{
			if (_property.isArray && _property.propertyType != SerializedPropertyType.String)
			{
				var elements = new JArray();
				for (int i = 0; i < _property.arraySize; i++)
					elements.Add(Encode(_property.GetArrayElementAtIndex(i)) ?? JValue.CreateNull());

				return new JObject { [ArrayKey] = elements };
			}

			switch (_property.propertyType)
			{
				case SerializedPropertyType.Boolean:
					return _property.boolValue;

				// longValue and intValue are different fields, and reading the wrong one on a 64 bit field
				// truncates silently.
				case SerializedPropertyType.Integer:
					return _property.type == "long" || _property.type == "ulong"
						? _property.longValue
						: _property.intValue;

				case SerializedPropertyType.LayerMask:
					return _property.intValue;

				case SerializedPropertyType.Float:
					return _property.type == "double" ? _property.doubleValue : _property.floatValue;

				case SerializedPropertyType.String:
					return _property.stringValue;

				case SerializedPropertyType.Character:
					return _property.intValue;

				// The stored value, not enumValueIndex: an enum whose members are not 0,1,2... would come
				// back as a different member.
				case SerializedPropertyType.Enum:
					return _property.intValue;

				case SerializedPropertyType.Color:
				{
					var c = _property.colorValue;
					return new JObject { ["r"] = c.r, ["g"] = c.g, ["b"] = c.b, ["a"] = c.a };
				}

				case SerializedPropertyType.Vector2:
					return new JObject { ["x"] = _property.vector2Value.x, ["y"] = _property.vector2Value.y };

				case SerializedPropertyType.Vector2Int:
					return new JObject { ["x"] = _property.vector2IntValue.x, ["y"] = _property.vector2IntValue.y };

				case SerializedPropertyType.Vector3:
				{
					var v = _property.vector3Value;
					return new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
				}

				case SerializedPropertyType.Vector3Int:
				{
					var v = _property.vector3IntValue;
					return new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
				}

				case SerializedPropertyType.Vector4:
				{
					var v = _property.vector4Value;
					return new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z, ["w"] = v.w };
				}

				case SerializedPropertyType.Quaternion:
				{
					var q = _property.quaternionValue;
					return new JObject { ["x"] = q.x, ["y"] = q.y, ["z"] = q.z, ["w"] = q.w };
				}

				case SerializedPropertyType.Rect:
				{
					var r = _property.rectValue;
					return new JObject { ["x"] = r.x, ["y"] = r.y, ["w"] = r.width, ["h"] = r.height };
				}

				case SerializedPropertyType.RectInt:
				{
					var r = _property.rectIntValue;
					return new JObject { ["x"] = r.x, ["y"] = r.y, ["w"] = r.width, ["h"] = r.height };
				}

				case SerializedPropertyType.Bounds:
				{
					var b = _property.boundsValue;
					return new JObject
					{
						["cx"] = b.center.x, ["cy"] = b.center.y, ["cz"] = b.center.z,
						["ex"] = b.extents.x, ["ey"] = b.extents.y, ["ez"] = b.extents.z,
					};
				}

				case SerializedPropertyType.ObjectReference:
					return EncodeObjectReference(_property.objectReferenceValue);

				case SerializedPropertyType.ManagedReference:
					if (_property.managedReferenceValue == null)
						return JValue.CreateNull();

					return EncodeChildren(_property);

				case SerializedPropertyType.Generic:
					return EncodeChildren(_property);

				default:
					return null;
			}
		}

		private static JToken EncodeChildren( SerializedProperty _property )
		{
			var result = new JObject();
			var iterator = _property.Copy();
			var end = _property.GetEndProperty();
			int depth = _property.depth;

			while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
			{
				if (iterator.depth != depth + 1)
					continue;

				var encoded = Encode(iterator);
				if (encoded != null)
					result[iterator.name] = encoded;
			}

			return result;
		}

		/// <summary>
		/// An asset reference as GUID plus local id. The name travels too, purely so a reference that does
		/// not resolve can say what it was looking for.
		/// </summary>
		private static JToken EncodeObjectReference( UnityEngine.Object _object )
		{
			if (_object == null)
				return JValue.CreateNull();

			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_object, out string guid, out long localId))
			{
				// A scene object, or something not in the asset database at all. It cannot be named in a
				// way another project could resolve, so it travels as "empty, and here is what it was".
				return new JObject { ["name"] = _object.name };
			}

			return new JObject { ["guid"] = guid, ["localId"] = localId, ["name"] = _object.name };
		}

		/// <summary>Writes a value back. False when the document holds something this cannot use.</summary>
		private static bool Decode( SerializedProperty _property, JToken _token )
		{
			if (_token == null || _token.Type == JTokenType.Null)
			{
				// Only a reference has a meaningful "nothing"; for everything else a null means the copy
				// never carried this value.
				if (_property.propertyType != SerializedPropertyType.ObjectReference)
					return false;

				_property.objectReferenceValue = null;
				return true;
			}

			if (_property.isArray && _property.propertyType != SerializedPropertyType.String)
			{
				if (_token[ArrayKey] is not JArray elements)
					return false;

				_property.arraySize = elements.Count;
				for (int i = 0; i < elements.Count; i++)
					Decode(_property.GetArrayElementAtIndex(i), elements[i]);

				return true;
			}

			try
			{
				switch (_property.propertyType)
				{
					case SerializedPropertyType.Boolean:
						_property.boolValue = _token.Value<bool>();
						return true;

					case SerializedPropertyType.Integer:
						if (_property.type == "long" || _property.type == "ulong")
							_property.longValue = _token.Value<long>();
						else
							_property.intValue = _token.Value<int>();
						return true;

					case SerializedPropertyType.LayerMask:
					case SerializedPropertyType.Character:
					case SerializedPropertyType.Enum:
						_property.intValue = _token.Value<int>();
						return true;

					case SerializedPropertyType.Float:
						if (_property.type == "double")
							_property.doubleValue = _token.Value<double>();
						else
							_property.floatValue = _token.Value<float>();
						return true;

					case SerializedPropertyType.String:
						_property.stringValue = _token.Value<string>();
						return true;

					case SerializedPropertyType.Color:
						_property.colorValue = new Color(F(_token, "r"), F(_token, "g"), F(_token, "b"), F(_token, "a"));
						return true;

					case SerializedPropertyType.Vector2:
						_property.vector2Value = new Vector2(F(_token, "x"), F(_token, "y"));
						return true;

					case SerializedPropertyType.Vector2Int:
						_property.vector2IntValue = new Vector2Int(I(_token, "x"), I(_token, "y"));
						return true;

					case SerializedPropertyType.Vector3:
						_property.vector3Value = new Vector3(F(_token, "x"), F(_token, "y"), F(_token, "z"));
						return true;

					case SerializedPropertyType.Vector3Int:
						_property.vector3IntValue = new Vector3Int(I(_token, "x"), I(_token, "y"), I(_token, "z"));
						return true;

					case SerializedPropertyType.Vector4:
						_property.vector4Value = new Vector4(F(_token, "x"), F(_token, "y"), F(_token, "z"), F(_token, "w"));
						return true;

					case SerializedPropertyType.Quaternion:
						_property.quaternionValue = new Quaternion(F(_token, "x"), F(_token, "y"), F(_token, "z"), F(_token, "w"));
						return true;

					case SerializedPropertyType.Rect:
						_property.rectValue = new Rect(F(_token, "x"), F(_token, "y"), F(_token, "w"), F(_token, "h"));
						return true;

					case SerializedPropertyType.RectInt:
						_property.rectIntValue = new RectInt(I(_token, "x"), I(_token, "y"), I(_token, "w"), I(_token, "h"));
						return true;

					case SerializedPropertyType.Bounds:
						_property.boundsValue = new Bounds
						(
							new Vector3(F(_token, "cx"), F(_token, "cy"), F(_token, "cz")),
							new Vector3(F(_token, "ex"), F(_token, "ey"), F(_token, "ez")) * 2f
						);
						return true;

					case SerializedPropertyType.ObjectReference:
						_property.objectReferenceValue = DecodeObjectReference(_token);
						return true;

					case SerializedPropertyType.ManagedReference:
					case SerializedPropertyType.Generic:
						return DecodeChildren(_property, _token);

					default:
						return false;
				}
			}
			catch (Exception e)
			{
				// A malformed entry for one value is not a reason to abandon the other thirty-three.
				UiLog.LogWarning($"Could not paste '{_property.name}': {e.Message}");
				return false;
			}
		}

		private static bool DecodeChildren( SerializedProperty _property, JToken _token )
		{
			if (_token is not JObject fields)
				return false;

			bool any = false;
			var iterator = _property.Copy();
			var end = _property.GetEndProperty();
			int depth = _property.depth;

			while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
			{
				if (iterator.depth != depth + 1)
					continue;

				if (fields[iterator.name] is { } child && Decode(iterator, child))
					any = true;
			}

			return any;
		}

		private static UnityEngine.Object DecodeObjectReference( JToken _token )
		{
			string guid = (string)_token["guid"];
			if (string.IsNullOrEmpty(guid))
				return null;

			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path))
			{
				// Expected when pasting into another project: the reference names an asset this one does
				// not have. Said out loud, because an empty slot with no explanation reads as a bug.
				UiLog.LogWarning($"The asset '{(string)_token["name"]}' referenced by the copied style does "
					+ "not exist in this project - that value stays empty.");
				return null;
			}

			long localId = _token["localId"]?.Value<long>() ?? 0;
			foreach (var candidate in AssetDatabase.LoadAllAssetsAtPath(path))
			{
				if (candidate == null)
					continue;

				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long candidateId)
				 && candidateId == localId)
				{
					return candidate;
				}
			}

			return AssetDatabase.LoadMainAssetAtPath(path);
		}

		private static float F( JToken _token, string _key ) => _token[_key]?.Value<float>() ?? 0f;
		private static int I( JToken _token, string _key ) => _token[_key]?.Value<int>() ?? 0;

		#endregion
	}
}
