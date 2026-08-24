using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>What a comparison found about one style.</summary>
	public enum EStyleDriftState
	{
		/// <summary>Same values on both sides - a copy that carries no information and could be inherited.</summary>
		Identical,

		/// <summary>Present on both sides with different values - a real override, and worth keeping as one.</summary>
		Differs,

		/// <summary>Only in the config being examined. Stays whatever happens; nothing to inherit it from.</summary>
		OnlyHere,

		/// <summary>Only in the other config. Nothing to do either: inheriting would simply gain it.</summary>
		OnlyThere,
	}

	public class UiStyleValueDrift
	{
		public string Name;
		public string Here;
		public string There;

		/// <summary>
		/// The values differ, but not visibly - float noise from a copy, typically. Worth marking, because
		/// such a difference is not a decision anybody made and would otherwise be pinned as an override.
		/// </summary>
		public bool DisplayEqual;
	}

	public class UiStyleDrift
	{
		public string Alias;
		public string TypeName;

		/// <summary>
		/// Type and name in one number, which is how a style is identified everywhere else. Carried so a
		/// conversion can act on a finding without matching it up by name again.
		/// </summary>
		public int Key;

		public EStyleDriftState State;
		public readonly List<UiStyleValueDrift> Values = new();
	}

	public class UiSkinDrift
	{
		/// <summary>The skin these findings are about - what a conversion has to write to.</summary>
		public UiSkin Skin;

		public string SkinName;

		/// <summary>The skin it was compared against, or null when there is no counterpart.</summary>
		public string OtherSkinName;

		/// <summary>
		/// Which config that skin belongs to. Needed since a skin may build on a sibling: then the name alone
		/// does not say which of the two configs is meant.
		/// </summary>
		public string OtherConfigName;

		/// <summary>Whether the counterpart is a skin of the same config - a sibling, not an ancestor.</summary>
		public bool OtherIsSibling;

		/// <summary>How to refer to the counterpart in a sentence.</summary>
		public string OtherDescription => OtherSkinName == null
			? null
			: OtherIsSibling
				? $"'{OtherSkinName}' (this config)"
				: $"'{OtherSkinName}' of '{OtherConfigName}'";

		public readonly List<UiStyleDrift> Styles = new();

		public int Count( EStyleDriftState _state )
		{
			int result = 0;
			foreach (var style in Styles)
			{
				if (style.State == _state)
					result++;
			}

			return result;
		}
	}

	/// <summary>
	/// How far one style config has drifted from another, style by style and value by value.
	///
	/// This is the half of the conversion tool that changes nothing, and it is the half worth having first:
	/// it answers a question nobody can answer about an existing clone - how much of it is actually its own,
	/// and how much is a copy that has been silently diverging from the original ever since it was made.
	/// The counts also say what a conversion would buy before anything is converted.
	/// </summary>
	public class UiStyleConfigDrift
	{
		public string Name;
		public string OtherName;

		/// <summary>Whether the two are already in an inheritance relation, which changes how to read it.</summary>
		public bool AlreadyInherits;

		public readonly List<UiSkinDrift> Skins = new();

		/// <summary>Skins of the other config that no skin here maps to - so nothing of them is used.</summary>
		public readonly List<string> UnusedOtherSkins = new();

		public int Count( EStyleDriftState _state )
		{
			int result = 0;
			foreach (var skin in Skins)
				result += skin.Count(_state);

			return result;
		}

		public int ComparedStyles => Count(EStyleDriftState.Identical) + Count(EStyleDriftState.Differs);

		public string ToText()
		{
			var sb = new StringBuilder();
			int identical = Count(EStyleDriftState.Identical);
			int differs = Count(EStyleDriftState.Differs);
			int onlyHere = Count(EStyleDriftState.OnlyHere);
			int onlyThere = Count(EStyleDriftState.OnlyThere);

			sb.AppendLine($"Style config drift: '{Name}' against '{OtherName}'");
			sb.AppendLine(AlreadyInherits
				? $"'{Name}' already inherits from '{OtherName}', so what is listed here is what it stores itself."
				: $"'{Name}' does not inherit from '{OtherName}' - this is what inheriting would change.");
			sb.AppendLine();
			sb.AppendLine($"On both sides: {identical + differs}  ->  {identical} identical, {differs} differing.");
			sb.AppendLine($"Only in '{Name}': {onlyHere}.");
			sb.AppendLine($"Only in '{OtherName}': {onlyThere}"
				+ (AlreadyInherits ? " - inherited, nothing to do." : " - would be gained by inheriting."));

			if (identical > 0)
			{
				sb.AppendLine();
				sb.AppendLine(AlreadyInherits
					? $"{identical} of the styles stored here carry no difference and could be reverted to inherited."
					: $"Inheriting would drop {identical} copies and keep {differs} as overrides.");
			}

			foreach (var skin in Skins)
			{
				sb.AppendLine();
				sb.AppendLine(skin.OtherSkinName != null
					? $"Skin '{skin.SkinName}'  ->  {skin.OtherDescription}"
					: $"Skin '{skin.SkinName}'  ->  nothing: '{OtherName}' has no skin of that name, so this "
						+ "skin can inherit nothing until it is mapped to one.");

				sb.AppendLine($"   identical {skin.Count(EStyleDriftState.Identical)}"
					+ $" | differing {skin.Count(EStyleDriftState.Differs)}"
					+ $" | only here {skin.Count(EStyleDriftState.OnlyHere)}"
					+ $" | only there {skin.Count(EStyleDriftState.OnlyThere)}");

				foreach (var style in skin.Styles)
				{
					// Only the two states that carry a decision are listed. An identical style is a copy to
					// drop and a style that exists only on the other side is simply inherited - both are
					// answered by their count, and listing them buries the few lines that matter under
					// dozens that do not.
					if (style.State == EStyleDriftState.Identical || style.State == EStyleDriftState.OnlyThere)
						continue;

					sb.AppendLine($"   {Label(style.State)}  {style.Alias}  ({style.TypeName})");
					foreach (var value in style.Values)
					{
						sb.AppendLine($"        {value.Name}:  {value.Here}   vs   {value.There}"
							+ (value.DisplayEqual ? "   (equal as displayed - copy noise, not a decision)" : ""));
					}
				}
			}

			if (UnusedOtherSkins.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine($"Skins of '{OtherName}' nothing maps to: {string.Join(", ", UnusedOtherSkins)}.");
			}

			return sb.ToString();
		}

		private static string Label( EStyleDriftState _state ) => _state switch
		{
			EStyleDriftState.Differs => "differs   ",
			EStyleDriftState.OnlyHere => "only here ",
			EStyleDriftState.OnlyThere => "only there",
			_ => "identical ",
		};
	}

	public static class UiStyleDriftAnalyzer
	{
		/// <summary>
		/// Compares what each config declares ITSELF, skin by skin.
		///
		/// Own styles on both sides, deliberately: the comparison has to work before the two are related at
		/// all - that is the point in time when the question is asked - and afterwards it answers a second
		/// question, namely which of the overrides that are stored still carry a difference.
		///
		/// Skins are matched the way inheritance matches them: by 'inherit from' where one is set, by name
		/// otherwise. So the report shows the mapping that a conversion would actually use, including the
		/// skins that map to nothing.
		/// </summary>
		public static UiStyleConfigDrift Analyze( UiStyleConfig _config, UiStyleConfig _other )
		{
			var result = new UiStyleConfigDrift();
			if (_config == null || _other == null)
				return result;

			// Both sides are called UiMainStyleConfig in the case this exists for - a project's clone keeps
			// the name it was cloned from - and a report saying "'X' against 'X'" tells the reader nothing.
			bool sameName = _config.name == _other.name;
			result.Name = Identify(_config, sameName, false);
			result.OtherName = Identify(_other, sameName, false);

			// The short form is the folder, which is enough to tell a project config from the package's and
			// short enough to repeat in every line. Only if even that collides does the full path come out.
			if (result.Name == result.OtherName)
			{
				result.Name = Identify(_config, sameName, true);
				result.OtherName = Identify(_other, sameName, true);
			}

			result.AlreadyInherits = _config.Parent == _other;

			var matchedOtherSkins = new HashSet<string>();

			foreach (var skin in _config.Skins)
			{
				var otherSkin = Counterpart(skin, _other);
				var skinDrift = Analyze(skin, otherSkin);
				result.Skins.Add(skinDrift);

				if (otherSkin != null && otherSkin.StyleConfig == _other)
					matchedOtherSkins.Add(otherSkin.Name);
			}

			foreach (var otherSkin in _other.Skins)
			{
				if (!matchedOtherSkins.Contains(otherSkin.Name))
					result.UnusedOtherSkins.Add(otherSkin.Name);
			}

			return result;
		}

		/// <summary>
		/// How a config is named in the report: by its asset name, and only when both sides share that name
		/// by its path as well - which is long, and says nothing as long as the names already differ.
		/// </summary>
		private static string Identify( UiStyleConfig _config, bool _sameName, bool _fullPath )
		{
			if (!_sameName)
				return _config.name;

			var path = AssetDatabase.GetAssetPath(_config);
			if (string.IsNullOrEmpty(path))
				return $"{_config.name} (#{_config.GetInstanceID()})";

			return $"{_config.name} ({(_fullPath ? path : ShortFolder(path))})";
		}

		/// <summary>The last two folders of a path - '__Funatics/Resources' rather than all of it.</summary>
		private static string ShortFolder( string _path )
		{
			var parts = _path.Split('/');
			if (parts.Length < 3)
				return _path;

			return $"{parts[parts.Length - 3]}/{parts[parts.Length - 2]}";
		}

		/// <summary>
		/// Which skin a skin would be compared against.
		///
		/// A mapping that is already set wins - including one that points at a sibling - because the report
		/// is meant to show what a conversion would really do, and a conversion follows the mapping. Only
		/// when there is none does the name decide, which is what a conversion without a mapping would do.
		/// </summary>
		private static UiSkin Counterpart( UiSkin _skin, UiStyleConfig _other )
		{
			var mapped = _skin.ParentSkin;
			if (mapped != null)
				return mapped;

			return _other.GetOwnSkinByNameOrAlias(_skin.EffectiveInheritFromSkinName, false);
		}

		/// <summary>
		/// One skin against one skin - which is a question of its own: "which of these should this skin build
		/// on?" is answered by running this for each candidate and comparing the counts.
		/// </summary>
		public static UiSkinDrift Analyze( UiSkin _skin, UiSkin _other )
		{
			var result = new UiSkinDrift();
			if (_skin == null)
				return result;

			result.Skin = _skin;
			result.SkinName = _skin.Name;

			if (_other == null)
			{
				// Nothing to compare against, so everything here is its own - which is exactly the finding:
				// this skin would inherit nothing at all.
				foreach (var style in _skin.Styles)
					result.Styles.Add(Describe(style, EStyleDriftState.OnlyHere));

				return result;
			}

			result.OtherSkinName = _other.Name;
			result.OtherConfigName = _other.StyleConfig != null ? _other.StyleConfig.name : null;
			result.OtherIsSibling = _other.StyleConfig == _skin.StyleConfig;

			var otherByKey = new Dictionary<int, UiAbstractStyleBase>();
			foreach (var style in _other.Styles)
				otherByKey[style.Key] = style;

			foreach (var style in _skin.Styles)
			{
				if (!otherByKey.TryGetValue(style.Key, out var otherStyle))
				{
					result.Styles.Add(Describe(style, EStyleDriftState.OnlyHere));
					continue;
				}

				result.Styles.Add(Compare(style, otherStyle));
			}

			var ownKeys = new HashSet<int>();
			foreach (var style in _skin.Styles)
				ownKeys.Add(style.Key);

			foreach (var otherStyle in _other.Styles)
			{
				if (!ownKeys.Contains(otherStyle.Key))
					result.Styles.Add(Describe(otherStyle, EStyleDriftState.OnlyThere));
			}

			return result;
		}

		private static UiStyleDrift Describe( UiAbstractStyleBase _style, EStyleDriftState _state )
		{
			return new UiStyleDrift
			{
				Alias = _style.Alias,
				TypeName = _style.GetType().Name,
				Key = _style.Key,
				State = _state,
			};
		}

		private static UiStyleDrift Compare( UiAbstractStyleBase _style, UiAbstractStyleBase _other )
		{
			var result = Describe(_style, EStyleDriftState.Identical);

			var here = _style.Values;
			var there = _other.Values;

			// Two styles of the same type with a different number of values means the type changed under one
			// of the assets. Reported rather than assumed away, because a comparison by index would then be
			// comparing unrelated things.
			if (here.Length != there.Length)
			{
				result.State = EStyleDriftState.Differs;
				result.Values.Add(new UiStyleValueDrift
				{
					Name = "number of values",
					Here = here.Length.ToString(),
					There = there.Length.ToString(),
				});

				return result;
			}

			var names = ValueNames(_style);

			for (int i = 0; i < here.Length; i++)
			{
				var valueHere = here[i];
				var valueThere = there[i];
				if (valueHere == null || valueThere == null)
					continue;

				string name = names.TryGetValue(valueHere, out var known) ? known : $"value {i}";

				if (valueHere.IsApplicable != valueThere.IsApplicable)
				{
					result.State = EStyleDriftState.Differs;
					result.Values.Add(new UiStyleValueDrift
					{
						Name = name,
						Here = valueHere.IsApplicable ? "used" : "unused",
						There = valueThere.IsApplicable ? "used" : "unused",
					});

					continue;
				}

				// An unused value is not part of the style, so whatever it holds is not a difference.
				if (!valueHere.IsApplicable)
					continue;

				object rawHere = valueHere.RawValueObj;
				object rawThere = valueThere.RawValueObj;
				if (Equals(rawHere, rawThere))
					continue;

				string describedHere = DescribeValue(rawHere);
				string describedThere = DescribeValue(rawThere);

				result.State = EStyleDriftState.Differs;
				result.Values.Add(new UiStyleValueDrift
				{
					Name = name,
					Here = describedHere,
					There = describedThere,
					DisplayEqual = describedHere == describedThere,
				});
			}

			return result;
		}

		/// <summary>
		/// The name of every value, read off the fields that hold them. ApplicableValueBase carries no name
		/// of its own, and "value 3 differs" is not something anybody can act on.
		/// </summary>
		private static Dictionary<ApplicableValueBase, string> ValueNames( UiAbstractStyleBase _style )
		{
			var result = new Dictionary<ApplicableValueBase, string>();

			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
				| BindingFlags.DeclaredOnly;

			for (var type = _style.GetType(); type != null && type != typeof(object); type = type.BaseType)
			{
				foreach (var field in type.GetFields(flags))
				{
					if (!typeof(ApplicableValueBase).IsAssignableFrom(field.FieldType))
						continue;

					if (field.GetValue(_style) is ApplicableValueBase value && !result.ContainsKey(value))
						result[value] = ObjectNames.NicifyVariableName(field.Name);
				}
			}

			return result;
		}

		/// <summary>
		/// A value as a person reads it. Public because the two kinds of Unity null below are the sort of
		/// distinction that only stays right if something checks it.
		/// </summary>
		public static string DescribeValue( object _raw )
		{
			switch (_raw)
			{
				case null:
					return "<none>";

				// Unity's null is not null, and there are TWO kinds of it here - told apart by the instance
				// id, because they mean opposite things.
				//
				// An object field serialized as {fileID: 0} inside a [SerializeReference] block does not come
				// back as plain null but as a wrapper with instance id 0: nothing was ever assigned. A
				// reference whose asset is gone keeps its id. Calling the first one missing was wrong and
				// sent me looking for a broken asset that never existed - there are 16 unassigned values in
				// the config shipped with the package, and none of them is broken.
				case UnityEngine.Object unityObject when unityObject == null:
					return unityObject.GetInstanceID() == 0 ? "<none>" : "<missing>";

				case UnityEngine.Object unityObject:
					return $"{unityObject.name} ({unityObject.GetType().Name})";

				case Color color:
					return "#" + ColorUtility.ToHtmlStringRGBA(color);

				case float number:
					return number.ToString("R", CultureInfo.InvariantCulture);

				case IFormattable formattable:
					return formattable.ToString(null, CultureInfo.InvariantCulture);

				default:
					return _raw.ToString();
			}
		}
	}
}
