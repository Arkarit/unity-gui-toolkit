using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleUtility
	{
		public static string GetName( Type _supportedMonoBehaviourType, string _name )
		{
			return $"{_supportedMonoBehaviourType.Name} Style: {_name}";
		}

		public static int GetKey( Type _supportedMonoBehaviourType, string _name )
		{
			return Animator.StringToHash(GetName(_supportedMonoBehaviourType, _name));
		}

		/// <summary>
		/// An independent copy of a style, belonging to another config. Used to materialise an inherited
		/// style before writing to it: without the copy the write would land in the parent asset, and for
		/// the config shipped inside the package that save is discarded without a word.
		///
		/// Values are copied raw, so an asset reference stays a reference to the same asset - a sprite is
		/// shared, not duplicated. The generated style classes all take (config, name) in their
		/// constructor, which is what makes one implementation enough for all of them.
		/// </summary>
		public static UiAbstractStyleBase CloneStyle( UiAbstractStyleBase _source, UiStyleConfig _targetConfig )
		{
			if (_source == null)
				return null;

			UiAbstractStyleBase clone;
			try
			{
				clone = (UiAbstractStyleBase) Activator.CreateInstance(_source.GetType(), _targetConfig, _source.Name);
			}
			catch (Exception ex)
			{
				UiLog.LogError($"Cannot copy style '{_source.Name}' of type '{_source.GetType().Name}': its " +
				               $"constructor does not take (UiStyleConfig, string). {ex.Message}");
				return null;
			}

			// Alias falls back to Name when unset, so copying it unconditionally would turn the name into an
			// explicit alias. Only a real alias is worth carrying over.
			if (_source.Alias != _source.Name)
				clone.Alias = _source.Alias;

			var from = _source.Values;
			var to = clone.Values;
			if (from.Length != to.Length)
			{
				UiLog.LogError($"Cannot copy style '{_source.Name}': the copy has {to.Length} values, the " +
				               $"original {from.Length}.");
				return null;
			}

			for (int i = 0; i < from.Length; i++)
			{
				if (from[i] == null || to[i] == null)
					continue;

				to[i].RawValueObj = from[i].RawValueObj;
				to[i].IsApplicable = from[i].IsApplicable;
			}

			return clone;
		}

		public static void SynchronizeApplicableness( UiAbstractStyleBase _from, UiAbstractStyleBase _to )
		{
			if (_from == null
				|| _to == null
				|| _from.GetType() != _to.GetType())
				return;

			var members = _from.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (var member in members)
			{
				ApplicableValueBase fromVal = member.GetValue(_from) as ApplicableValueBase;
				ApplicableValueBase toVal = member.GetValue(_to) as ApplicableValueBase;
				if (fromVal == null || toVal == null)
					continue;

				toVal.IsApplicable = fromVal.IsApplicable;
			}
		}

		// There's an issue if you manually change component properties, which are also handled by a style.
		// Find an example in UiDistortGroup, which sets a distort modifier component enabled or disabled according to its needs.
		// As the complete distort modifier might be disabled by the skin/style, this might interfere.
		// Thus, UiDistortGroup calls ReApplyAppliers() after handling its changes to ensure the component is not enabled if forbidden by style.
		// This is better than reapplying the complete skin, but not completely lightweight, since components need to be collected, so handle with care.
		public static void ReApplyApplier(Component component)
		{
			var appliers = component.GetComponents<UiAbstractApplyStyleBase>();
			foreach (var applier in appliers)
			{
				if (!applier.enabled)
					continue;

				if (applier.Component == component)
				{
					applier.Apply();
					return;
				}
			}
		}

		public static void ReApplyAppliers<T>(IEnumerable<T> list) where T : Component
		{
			if (!AssetReadyGate.Ready)
				return;
			
			foreach (var elem in list)
				ReApplyApplier(elem);
		}
	}
}
