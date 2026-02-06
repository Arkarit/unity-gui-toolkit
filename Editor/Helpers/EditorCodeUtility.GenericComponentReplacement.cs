#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using OwnerAndPathList = System.Collections.Generic.List<(UnityEngine.Object owner, string propertyPath)>;
using OwnerAndPathListById = System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<(UnityEngine.Object owner, string propertyPath)>>;

namespace GuiToolkit.Editor
{
	public static partial class EditorCodeUtility
	{
#if UITK_USE_ROSLYN

		public sealed class GenericArraySnapshot
		{
			public string PropertyPath;
			private readonly object[] m_elements;
			private readonly Action<SerializedProperty, object> m_applyElement;

			private GenericArraySnapshot
			(
				string _propertyPath,
				object[] _elements,
				Action<SerializedProperty, object> _applyElement
			)
			{
				PropertyPath = _propertyPath;
				m_elements = _elements ?? Array.Empty<object>();
				m_applyElement = _applyElement;
			}

			public static GenericArraySnapshot Capture(
				SerializedProperty _arrayProp,
				Func<SerializedProperty, object> _captureElement,
				Action<SerializedProperty, object> _applyElement
			)
			{
				if (_arrayProp == null)
					throw new ArgumentNullException(nameof(_arrayProp));
				if (!_arrayProp.isArray)
					throw new ArgumentException($"Property '{_arrayProp.propertyPath}' is not an array.", nameof(_arrayProp));
				if (_captureElement == null)
					throw new ArgumentNullException(nameof(_captureElement));
				if (_applyElement == null)
					throw new ArgumentNullException(nameof(_applyElement));

				int size = _arrayProp.arraySize;
				var elements = new object[size];

				for (int i = 0; i < size; i++)
					elements[i] = _captureElement(_arrayProp.GetArrayElementAtIndex(i));

				return new GenericArraySnapshot(_arrayProp.propertyPath, elements, _applyElement);
			}

			public bool Apply( SerializedObject _dstSo )
			{
				if (_dstSo == null)
					return false;

				var p = _dstSo.FindProperty(PropertyPath);
				if (p == null || !p.isArray)
					return false;

				// important: empty array is valid and should be applied
				p.arraySize = m_elements.Length;

				for (int i = 0; i < m_elements.Length; i++)
				{
					var elem = p.GetArrayElementAtIndex(i);
					m_applyElement(elem, m_elements[i]);
				}

				return true;
			}
		}

		public class GenericSnapshot
		{
			public int OldId;
			public readonly List<SerializedPropertyRecord> Records = new();
			public readonly List<GenericArraySnapshot> Arrays = new();

			public GenericSnapshot( int _oldId ) => OldId = _oldId;

			public override string ToString()
			{
				int count = Records != null ? Records.Count : 0;
				return $"GenericSnapshot(OldId={OldId}, Records={count})";
			}
		}

		public struct SerializedPropertyRecord
		{
			public string PropertyPath;
			public SerializedPropertyType Type;

			public bool BoolValue;
			public string StringValue;

			public Color ColorValue;
			public Vector2 Vector2Value;
			public Vector3 Vector3Value;
			public Vector4 Vector4Value;
			public Rect RectValue;
			public AnimationCurve CurveValue;
			public Bounds BoundsValue;
			public Quaternion QuaternionValue;

			public UnityEngine.Object ObjectReferenceValue;

			public int EnumValueIndex;
			public int LayerMaskValue;

			public long LongValue;
			public double DoubleValue;

			public Vector2Int Vector2IntValue;
			public Vector3Int Vector3IntValue;
			public RectInt RectIntValue;
			public BoundsInt BoundsIntValue;

			// ManagedReference is tricky; we only support it via CopyFromSerializedProperty if possible.
			public bool HasManagedReference;
		}

		private static bool ShouldSkipPropertyPath( string _propertyPath )
		{
			if (string.IsNullOrEmpty(_propertyPath))
				return true;

			// Unity internals or irrelevant
			if (_propertyPath == "m_Script")
				return true;
			if (_propertyPath == "m_GameObject")
				return true;
			if (_propertyPath == "m_Enabled")
				return true;
			if (_propertyPath == "m_ObjectHideFlags")
				return true;

			return false;
		}

		private static GenericSnapshot CaptureGenericSnapshot( MonoBehaviour _src )
		{
			if (!_src)
				return default;

			var snapshot = new GenericSnapshot(_src.GetInstanceID());

			var so = new SerializedObject(_src);
			var it = so.GetIterator();

			while (it.NextVisible(true))
			{
				if (ShouldSkipPropertyPath(it.propertyPath))
					continue;

				if (it.propertyType == SerializedPropertyType.Generic && it.isArray)
				{
					if (it.propertyPath.EndsWith(".Array", StringComparison.Ordinal))
						continue;

					if (it.propertyType == SerializedPropertyType.Generic && it.isArray)
					{
						if (it.propertyPath.EndsWith(".Array", StringComparison.Ordinal))
							continue;

						switch (it.arrayElementType)
						{
							case "int":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.intValue,
									( property, o ) => property.intValue = (int)o));
								break;

							// long: Unity kann hier verschiedene Strings liefern, je nach Version/Backend
							case "long":
							case "Int64":
							case "SInt64":
							case "UInt64":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.longValue,
									( property, o ) => property.longValue = (long)o));
								break;

							case "float":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.floatValue,
									( property, o ) => property.floatValue = (float)o));
								break;

							// double: dito, Strings koennen variieren
							case "double":
							case "Double":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.doubleValue,
									( property, o ) => property.doubleValue = (double)o));
								break;

							case "bool":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.boolValue,
									( property, o ) => property.boolValue = (bool)o));
								break;

							case "string":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.stringValue,
									( property, o ) => property.stringValue = (string)o));
								break;

							// Unity-intern
							case "ColorRGBA":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.colorValue,
									( property, o ) => property.colorValue = (Color)o));
								break;

							case "Vector2f":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.vector2Value,
									( property, o ) => property.vector2Value = (Vector2)o));
								break;

							case "Vector3f":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.vector3Value,
									( property, o ) => property.vector3Value = (Vector3)o));
								break;

							case "Vector4f":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.vector4Value,
									( property, o ) => property.vector4Value = (Vector4)o));
								break;

							case "Rectf":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.rectValue,
									( property, o ) => property.rectValue = (Rect)o));
								break;

							case "Quaternionf":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.quaternionValue,
									( property, o ) => property.quaternionValue = (Quaternion)o));
								break;

							// Diese Strings sind ebenfalls Unity-abhängig; je nach Version können sie anders heißen.
							case "Vector2Int":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.vector2IntValue,
									( property, o ) => property.vector2IntValue = (Vector2Int)o));
								break;

							case "Vector3Int":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.vector3IntValue,
									( property, o ) => property.vector3IntValue = (Vector3Int)o));
								break;

							case "RectInt":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.rectIntValue,
									( property, o ) => property.rectIntValue = (RectInt)o));
								break;

							case "Bounds":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.boundsValue,
									( property, o ) => property.boundsValue = (Bounds)o));
								break;

							case "BoundsInt":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.boundsIntValue,
									( property, o ) => property.boundsIntValue = (BoundsInt)o));
								break;

							case "AnimationCurve":
								snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
									( property ) => property.animationCurveValue,
									( property, o ) => property.animationCurveValue = (AnimationCurve)o));
								break;

							default:
								{
									SerializedProperty firstElem = it.arraySize > 0 ? it.GetArrayElementAtIndex(0) : null;
									if (firstElem == null)
										break;

									switch (firstElem.propertyType)
									{
										case SerializedPropertyType.ObjectReference:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.objectReferenceValue,
												( property, o ) => property.objectReferenceValue = (UnityEngine.Object)o));
											break;

										case SerializedPropertyType.Enum:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.enumValueIndex,
												( property, o ) => property.enumValueIndex = (int)o));
											break;

										case SerializedPropertyType.Vector3:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.vector3Value,
												( property, o ) => property.vector3Value = (Vector3)o));
											break;

										case SerializedPropertyType.Color:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.colorValue,
												( property, o ) => property.colorValue = (Color)o));
											break;

										case SerializedPropertyType.Vector2:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.vector2Value,
												( property, o ) => property.vector2Value = (Vector2)o));
											break;

										case SerializedPropertyType.Vector4:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.vector4Value,
												( property, o ) => property.vector4Value = (Vector4)o));
											break;

										case SerializedPropertyType.Rect:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.rectValue,
												( property, o ) => property.rectValue = (Rect)o));
											break;

										case SerializedPropertyType.Quaternion:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.quaternionValue,
												( property, o ) => property.quaternionValue = (Quaternion)o));
											break;

										case SerializedPropertyType.Vector2Int:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.vector2IntValue,
												( property, o ) => property.vector2IntValue = (Vector2Int)o));
											break;

										case SerializedPropertyType.Vector3Int:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.vector3IntValue,
												( property, o ) => property.vector3IntValue = (Vector3Int)o));
											break;

										case SerializedPropertyType.Bounds:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.boundsValue,
												( property, o ) => property.boundsValue = (Bounds)o));
											break;

										case SerializedPropertyType.BoundsInt:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.boundsIntValue,
												( property, o ) => property.boundsIntValue = (BoundsInt)o));
											break;

										case SerializedPropertyType.AnimationCurve:
											snapshot.Arrays.Add(GenericArraySnapshot.Capture(it,
												( property ) => property.animationCurveValue,
												( property, o ) => property.animationCurveValue = (AnimationCurve)o));
											break;

										default:
											// UiLog.LogInternal($"Array element type not supported: '{it.arrayElementType}' ({firstElem.propertyType}) at '{it.propertyPath}'");
											break;
									}

									break;
								}
						}
					}
				}

				// We'll record only "leaf-ish" values; for complex types, we still attempt CopyFrom on apply,
				// but we also keep typed data for the common primitives.
				var r = new SerializedPropertyRecord
				{
					PropertyPath = it.propertyPath,
					Type = it.propertyType
				};

				switch (it.propertyType)
				{
					case SerializedPropertyType.Integer:
						r.LongValue = it.longValue;
						break;
					case SerializedPropertyType.Boolean:
						r.BoolValue = it.boolValue;
						break;
					case SerializedPropertyType.Float:
						r.DoubleValue = it.doubleValue;
						break;
					case SerializedPropertyType.String:
						r.StringValue = it.stringValue;
						break;
					case SerializedPropertyType.Color:
						r.ColorValue = it.colorValue;
						break;
					case SerializedPropertyType.ObjectReference:
						r.ObjectReferenceValue = it.objectReferenceValue;
						break;
					case SerializedPropertyType.Enum:
						r.EnumValueIndex = it.enumValueIndex;
						break;
					case SerializedPropertyType.LayerMask:
						r.LayerMaskValue = it.intValue;
						break;
					case SerializedPropertyType.Vector2:
						r.Vector2Value = it.vector2Value;
						break;
					case SerializedPropertyType.Vector3:
						r.Vector3Value = it.vector3Value;
						break;
					case SerializedPropertyType.Vector4:
						r.Vector4Value = it.vector4Value;
						break;
					case SerializedPropertyType.Rect:
						r.RectValue = it.rectValue;
						break;
					case SerializedPropertyType.AnimationCurve:
						r.CurveValue = it.animationCurveValue;
						break;
					case SerializedPropertyType.Bounds:
						r.BoundsValue = it.boundsValue;
						break;
					case SerializedPropertyType.Quaternion:
						r.QuaternionValue = it.quaternionValue;
						break;
					case SerializedPropertyType.Vector2Int:
						r.Vector2IntValue = it.vector2IntValue;
						break;
					case SerializedPropertyType.Vector3Int:
						r.Vector3IntValue = it.vector3IntValue;
						break;
					case SerializedPropertyType.RectInt:
						r.RectIntValue = it.rectIntValue;
						break;
					case SerializedPropertyType.BoundsInt:
						r.BoundsIntValue = it.boundsIntValue;
						break;
					case SerializedPropertyType.ManagedReference:
						r.HasManagedReference = true;
						break;
					default:
						// For Generic, Gradient, ExposedReference etc.
						// We'll rely on CopyFromSerializedProperty attempt in apply.
						break;
				}

				snapshot.Records.Add(r);
			}

			return snapshot;
		}

		private static bool TryApplyRecordToProperty
		(
			SerializedProperty _dst,
			SerializedPropertyRecord _r,
			MonoBehaviour _dstComponent
		)
		{
			if (_dst == null)
				return false;

			// For most types, require exact propertyType match.
			// Exception: ObjectReference can be assignable in runtime even if "type" differs.
			if (_dst.propertyType != _r.Type)
			{
				// Allow ObjectReference->ObjectReference only (already ensured by type check)
				return false;
			}

			switch (_r.Type)
			{
				case SerializedPropertyType.Integer:
					_dst.longValue = _r.LongValue;
					return true;
				case SerializedPropertyType.Boolean:
					_dst.boolValue = _r.BoolValue;
					return true;
				case SerializedPropertyType.Float:
					_dst.doubleValue = _r.DoubleValue;
					return true;
				case SerializedPropertyType.String:
					_dst.stringValue = _r.StringValue;
					return true;
				case SerializedPropertyType.Color:
					_dst.colorValue = _r.ColorValue;
					return true;
				case SerializedPropertyType.Enum:
					_dst.enumValueIndex = _r.EnumValueIndex;
					return true;
				case SerializedPropertyType.LayerMask:
					_dst.intValue = _r.LayerMaskValue;
					return true;
				case SerializedPropertyType.Vector2:
					_dst.vector2Value = _r.Vector2Value;
					return true;
				case SerializedPropertyType.Vector3:
					_dst.vector3Value = _r.Vector3Value;
					return true;
				case SerializedPropertyType.Vector4:
					_dst.vector4Value = _r.Vector4Value;
					return true;
				case SerializedPropertyType.Rect:
					_dst.rectValue = _r.RectValue;
					return true;
				case SerializedPropertyType.AnimationCurve:
					_dst.animationCurveValue = _r.CurveValue;
					return true;
				case SerializedPropertyType.Bounds:
					_dst.boundsValue = _r.BoundsValue;
					return true;
				case SerializedPropertyType.Quaternion:
					_dst.quaternionValue = _r.QuaternionValue;
					return true;
				case SerializedPropertyType.Vector2Int:
					_dst.vector2IntValue = _r.Vector2IntValue;
					return true;
				case SerializedPropertyType.Vector3Int:
					_dst.vector3IntValue = _r.Vector3IntValue;
					return true;
				case SerializedPropertyType.RectInt:
					_dst.rectIntValue = _r.RectIntValue;
					return true;
				case SerializedPropertyType.BoundsInt:
					_dst.boundsIntValue = _r.BoundsIntValue;
					return true;

				case SerializedPropertyType.ObjectReference:
					{
						// Be conservative: only assign if it is compatible with the destination field type.
						// SerializedProperty doesn't expose the exact expected type reliably in all cases;
						// Unity will validate on ApplyModifiedProperties anyway.
						_dst.objectReferenceValue = _r.ObjectReferenceValue;
						return true;
					}

				case SerializedPropertyType.ManagedReference:
					{
						// Best effort: try to keep it via CopyFrom if possible.
						// If this fails, we silently skip.
						return false;
					}

				default:
					return false;
			}
		}

		private static void ApplyGenericSnapshot
		(
			GenericSnapshot _snapshot,
			MonoBehaviour _dst
		)
		{
			if (!_dst || _snapshot.Records == null || _snapshot.Records.Count == 0)
				return;

			var dstSo = new SerializedObject(_dst);

			int applied = _snapshot.Arrays.Count;

			foreach (var genericArraySnapshot in _snapshot.Arrays)
				genericArraySnapshot.Apply(dstSo);

			for (int i = 0; i < _snapshot.Records.Count; i++)
			{
				var r = _snapshot.Records[i];

				if (ShouldSkipPropertyPath(r.PropertyPath))
					continue;

				var dstProp = dstSo.FindProperty(r.PropertyPath);
				if (dstProp == null)
					continue;

				// Prefer Unity's own CopyFromSerializedProperty if we can get a source prop again.
				// But source object is gone at this point. So: we apply typed values for primitives.
				// If you want deep struct/array copy, do it BEFORE destroy: see ApplyGenericSnapshotImmediate below.
				if (TryApplyRecordToProperty(dstProp, r, _dst))
					applied++;
			}

			if (applied > 0)
			{
				dstSo.ApplyModifiedProperties();
				EditorUtility.SetDirty(_dst);
			}
		}

		// Better version: deep copy using CopyFromSerializedProperty BEFORE you destroy the source component.
		private static int CopySharedSerializedPropertiesImmediate( MonoBehaviour _src, MonoBehaviour _dst )
		{
			if (!_src || !_dst)
				return 0;

			var srcSo = new SerializedObject(_src);
			var dstSo = new SerializedObject(_dst);

			var it = srcSo.GetIterator();
			var enterChildren = true;

			int copied = 0;

			while (it.NextVisible(enterChildren))
			{
				enterChildren = false;

				if (ShouldSkipPropertyPath(it.propertyPath))
					continue;

				var dstProp = dstSo.FindProperty(it.propertyPath);
				if (dstProp == null)
					continue;

				// Must match category-type. Unity is categorical here (int/long => Integer, float/double => Float).
				if (dstProp.propertyType != it.propertyType)
					continue;

				if (TryCopyPropertyValue(it, dstProp))
					copied++;
			}

			if (copied > 0)
			{
				dstSo.ApplyModifiedProperties();
				EditorUtility.SetDirty(_dst);
			}

			return copied;
		}
		private static bool TryCopyPropertyValue( SerializedProperty _src, SerializedProperty _dst )
		{
			if (_src == null || _dst == null)
				return false;

			// Safety: never touch m_Script
			if (_dst.propertyPath == "m_Script")
				return false;

			switch (_src.propertyType)
			{
				case SerializedPropertyType.Integer:
					// Works for int AND long.
					_dst.longValue = _src.longValue;
					return true;

				case SerializedPropertyType.Boolean:
					_dst.boolValue = _src.boolValue;
					return true;

				case SerializedPropertyType.Float:
					// Works for float AND double.
					_dst.doubleValue = _src.doubleValue;
					return true;

				case SerializedPropertyType.String:
					_dst.stringValue = _src.stringValue;
					return true;

				case SerializedPropertyType.Color:
					_dst.colorValue = _src.colorValue;
					return true;

				case SerializedPropertyType.ObjectReference:
					_dst.objectReferenceValue = _src.objectReferenceValue;
					return true;

				case SerializedPropertyType.LayerMask:
					_dst.intValue = _src.intValue;
					return true;

				case SerializedPropertyType.Enum:
					_dst.enumValueIndex = _src.enumValueIndex;
					return true;

				case SerializedPropertyType.Vector2:
					_dst.vector2Value = _src.vector2Value;
					return true;

				case SerializedPropertyType.Vector3:
					_dst.vector3Value = _src.vector3Value;
					return true;

				case SerializedPropertyType.Vector4:
					_dst.vector4Value = _src.vector4Value;
					return true;

				case SerializedPropertyType.Rect:
					_dst.rectValue = _src.rectValue;
					return true;

				case SerializedPropertyType.RectInt:
					_dst.rectIntValue = _src.rectIntValue;
					return true;

				case SerializedPropertyType.Bounds:
					_dst.boundsValue = _src.boundsValue;
					return true;

				case SerializedPropertyType.BoundsInt:
					_dst.boundsIntValue = _src.boundsIntValue;
					return true;

				case SerializedPropertyType.Quaternion:
					_dst.quaternionValue = _src.quaternionValue;
					return true;

				case SerializedPropertyType.Vector2Int:
					_dst.vector2IntValue = _src.vector2IntValue;
					return true;

				case SerializedPropertyType.Vector3Int:
					_dst.vector3IntValue = _src.vector3IntValue;
					return true;

				case SerializedPropertyType.AnimationCurve:
					_dst.animationCurveValue = _src.animationCurveValue;
					return true;

				case SerializedPropertyType.ExposedReference:
					_dst.exposedReferenceValue = _src.exposedReferenceValue;
					return true;

				case SerializedPropertyType.ManagedReference:
					// Best-effort: Unity often allows direct assignment.
					// If your version doesn't support it, skip.
					try
					{
						_dst.managedReferenceValue = _src.managedReferenceValue;
						return true;
					}
					catch
					{
						return false;
					}

				case SerializedPropertyType.Gradient:
					// No public setter in many Unity versions.
					return false;

				case SerializedPropertyType.Generic:
					// Generic containers are handled by their leaf properties (including Array.size).
					// Returning false is fine; leaves will still be copied.
					return false;

				default:
					return false;
			}
		}
		private static OwnerAndPathListById CollectRefGroupsToComponentInActiveScene<TA>()
			where TA : MonoBehaviour
		{
			var result = new OwnerAndPathListById();

			var allComponents = EditorAssetUtility.FindObjectsInCurrentEditedPrefabOrScene<MonoBehaviour>();

			foreach (var comp in allComponents)
			{
				if (!comp)
					continue;

				var so = new SerializedObject(comp);
				var it = so.GetIterator();
				var enterChildren = true;

				while (it.NextVisible(enterChildren))
				{
					enterChildren = false;

					if (it.propertyType != SerializedPropertyType.ObjectReference)
						continue;

					var obj = it.objectReferenceValue;
					if (!obj)
						continue;

					var old = obj as TA;
					if (!old)
						continue;

					var id = old.GetInstanceID();
					if (!result.TryGetValue(id, out var list))
					{
						list = new OwnerAndPathList();
						result.Add(id, list);
					}

					list.Add((comp, it.propertyPath));
				}
			}

			return result;
		}

		private static void RewireRefsForOldId
		(
			OwnerAndPathListById _groups,
			int _oldId,
			MonoBehaviour _newTarget
		)
		{
			if (_newTarget == null)
				return;

			if (_groups == null)
				return;

			if (!_groups.TryGetValue(_oldId, out var props) || props == null || props.Count == 0)
				return;

			foreach (var (owner, path) in props)
			{
				if (!owner)
					continue;

				var so = new SerializedObject(owner);
				var sp = so.FindProperty(path);
				if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (sp.objectReferenceValue != _newTarget)
				{
					Undo.RecordObject(owner, "Rewire component reference");
					sp.objectReferenceValue = _newTarget;
					so.ApplyModifiedProperties();
					EditorUtility.SetDirty(owner);
				}
			}
		}

		public static List<(GenericSnapshot Snapshot, TB NewComp)> ReplaceMonoBehavioursInActiveSceneGeneric<TA, TB>()
			where TA : MonoBehaviour
			where TB : MonoBehaviour
		{
			// collect all object-reference properties that currently point to TA
			var refGroups = CollectRefGroupsToComponentInActiveScene<TA>();

			return ReplaceMonoBehavioursInActiveSceneWithMapping<TA, TB, GenericSnapshot>
			(
				_capture: ( TA a ) => CaptureGenericSnapshot(a),
				_apply: ( GenericSnapshot s, TB b ) =>
				{
					// Deep copy (arrays/structs) should happen BEFORE destroy.
					// But we are already after destroy here. So we do best-effort typed apply.
					// If you want deep copy, we do the immediate copy in the main replace loop (see below).
					ApplyGenericSnapshot(s, b);
					RewireRefsForOldId(refGroups, s.OldId, b);
				}
			);
		}

#endif
	}
}
