using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Finds style appliers that write the same property of the same component, where only the last one
	/// to apply wins. That is never intended, so the inspector shows it as an error: it is a duplicated
	/// applier, or a second style that was added next to the first instead of replacing it.
	///
	/// Several appliers on one component are fine as long as they set different properties - one applier
	/// for the text colour and one for the font size is a normal setup. So the check compares the
	/// properties each resolved style has switched on (IsApplicable), not the appliers or their styles.
	///
	/// Only enabled appliers count, because a disabled one applies nothing. Swapping between two appliers
	/// by enabling one of them is a legitimate setup and must not be flagged.
	/// </summary>
	public static class UiStyleApplierOverlap
	{
		public readonly struct Overlap
		{
			public readonly UiAbstractApplyStyleBase Other;
			public readonly IReadOnlyList<string> PropertyNames;

			public Overlap( UiAbstractApplyStyleBase _other, IReadOnlyList<string> _propertyNames )
			{
				Other = _other;
				PropertyNames = _propertyNames;
			}
		}

		// Field name of every applicable value, per style type. Built once per type by reflection.
		private static readonly Dictionary<Type, FieldInfo[]> s_valueFieldsByStyleType = new();

		/// <summary>
		/// The other enabled appliers on the same GameObject that target the same component as _applier
		/// and switch on at least one of the same properties. Empty when there are none, or when _applier
		/// itself is disabled or resolves no style.
		/// </summary>
		public static List<Overlap> Find( UiAbstractApplyStyleBase _applier )
		{
			var result = new List<Overlap>();
			if (_applier == null || !_applier.enabled)
				return result;

			var component = _applier.Component;
			if (component == null)
				return result;

			var own = ApplicablePropertyNames(_applier.Style);
			if (own.Count == 0)
				return result;

			foreach (var other in _applier.GetComponents<UiAbstractApplyStyleBase>())
			{
				if (other == _applier || !other.enabled || other.Component != component)
					continue;

				var shared = new List<string>();
				foreach (var name in ApplicablePropertyNames(other.Style))
				{
					if (own.Contains(name))
						shared.Add(name);
				}

				if (shared.Count > 0)
					result.Add(new Overlap(other, shared));
			}

			return result;
		}

		/// <summary>
		/// Display names of the properties a style has switched on, e.g. "Color", "Font Size". Names, not
		/// indices, because two appliers of different types can target the same component - a TMP_Text
		/// applier and a TextMeshProUGUI applier both write "Color" - and their value lists differ.
		/// </summary>
		public static HashSet<string> ApplicablePropertyNames( UiAbstractStyleBase _style )
		{
			var result = new HashSet<string>();
			if (_style == null)
				return result;

			// Reading Values first also creates any value field that is still null, so the fields read
			// below are the instances Values holds.
			var values = _style.Values;
			if (values == null)
				return result;

			foreach (var field in ValueFields(_style.GetType()))
			{
				if (field.GetValue(_style) is ApplicableValueBase value && value.IsApplicable)
					result.Add(ObjectNames.NicifyVariableName(field.Name));
			}

			return result;
		}

		private static FieldInfo[] ValueFields( Type _styleType )
		{
			if (s_valueFieldsByStyleType.TryGetValue(_styleType, out var cached))
				return cached;

			var fields = new List<FieldInfo>();
			for (var type = _styleType; type != null && type != typeof(object); type = type.BaseType)
			{
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (typeof(ApplicableValueBase).IsAssignableFrom(field.FieldType))
						fields.Add(field);
				}
			}

			var array = fields.ToArray();
			s_valueFieldsByStyleType[_styleType] = array;
			return array;
		}
	}
}
