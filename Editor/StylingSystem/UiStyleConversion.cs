using System.Collections.Generic;
using System.Text;
using UnityEditor;

namespace GuiToolkit.Style.Editor
{
	/// <summary>One copy a conversion could drop, and whether it is going to.</summary>
	public class UiStyleConversionEntry
	{
		public UiSkin Skin;
		public int Key;
		public string Alias;
		public string TypeName;

		/// <summary>
		/// Default yes - that is the point of converting. Turned off to PIN a style: keeping a copy that is
		/// identical today so it stops following the other config tomorrow. A deliberate decision, and the
		/// only reason not to drop a copy that carries nothing.
		/// </summary>
		public bool Drop = true;
	}

	/// <summary>
	/// What turning a clone into a child would actually do, as a list one can look at and change.
	///
	/// It is built from the drift analysis and does exactly one thing: drop the copies that carry no
	/// difference. It never invents a skin mapping - the skin drawer is where that is chosen, and a mapping
	/// invented here would silently decide what a skin builds on.
	/// </summary>
	public class UiStyleConversionPlan
	{
		public UiStyleConfig Config;

		/// <summary>The parent to set, or null when it is already set.</summary>
		public UiStyleConfig ParentToSet;

		public readonly List<UiStyleConversionEntry> Entries = new();

		public int DropCount
		{
			get
			{
				int result = 0;
				foreach (var entry in Entries)
				{
					if (entry.Drop)
						result++;
				}

				return result;
			}
		}

		public int PinnedCount => Entries.Count - DropCount;

		public string Describe()
		{
			var sb = new StringBuilder();

			if (ParentToSet != null)
				sb.AppendLine($"Set 'Inherits from' of '{Config.name}' to '{ParentToSet.name}'.");

			sb.AppendLine($"Drop {DropCount} of {Entries.Count} copies that carry no difference"
				+ (PinnedCount > 0 ? $", keep {PinnedCount} as pinned overrides." : "."));

			return sb.ToString();
		}
	}

	public static class UiStyleConversion
	{
		/// <summary>
		/// Which of the findings a conversion can act on: the styles that are identical to what they would be
		/// inherited from. Nothing else - a difference is somebody's decision, and a style that exists on one
		/// side only has nothing to fall back to.
		/// </summary>
		public static UiStyleConversionPlan Plan( UiStyleConfig _config, UiStyleConfig _other )
		{
			var plan = new UiStyleConversionPlan { Config = _config };
			if (_config == null || _other == null || _config == _other)
				return plan;

			plan.ParentToSet = _config.Parent == _other ? null : _other;

			var drift = UiStyleDriftAnalyzer.Analyze(_config, _other);
			foreach (var skinDrift in drift.Skins)
			{
				if (skinDrift.Skin == null || skinDrift.OtherSkinName == null)
					continue;

				foreach (var style in skinDrift.Styles)
				{
					if (style.State != EStyleDriftState.Identical)
						continue;

					plan.Entries.Add(new UiStyleConversionEntry
					{
						Skin = skinDrift.Skin,
						Key = style.Key,
						Alias = style.Alias,
						TypeName = style.TypeName,
					});
				}
			}

			return plan;
		}

		/// <summary>
		/// Carries the plan out. Returns what happened, in the same words the report uses.
		///
		/// The parent is set FIRST and on purpose: dropping a copy before there is anything to inherit it
		/// from would not be a conversion, it would be a deletion. Every drop then goes through
		/// UiSkin.RevertStyleToInherited, which refuses exactly that case - and also refuses to touch the
		/// config inside the package, whose saves are discarded without a word. So a plan built against a
		/// config that has changed underneath cannot quietly lose a style: it is counted as refused.
		/// </summary>
		public static string Apply( UiStyleConversionPlan _plan )
		{
			if (_plan?.Config == null)
				return "Nothing to do.";

			var config = _plan.Config;

			// One snapshot of the whole asset, so the whole conversion is a single step in the undo history
			// rather than a few hundred - and so it is undoable at all.
			Undo.RegisterCompleteObjectUndo(config, "Convert style config to inheritance");

			if (_plan.ParentToSet != null)
				config.Parent = _plan.ParentToSet;

			int dropped = 0;
			int refused = 0;

			foreach (var entry in _plan.Entries)
			{
				if (!entry.Drop || entry.Skin == null)
					continue;

				entry.Skin.RevertStyleToInherited(entry.Key);

				if (entry.Skin.OwnsStyle(entry.Key))
					refused++;
				else
					dropped++;
			}

			UiStyleConfig.SetDirty(config);
			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);

			var sb = new StringBuilder();
			sb.AppendLine($"'{config.name}' now inherits from '{config.Parent?.name}'.");
			sb.AppendLine($"{dropped} copies dropped, {_plan.PinnedCount} kept as pinned overrides.");

			if (refused > 0)
			{
				sb.AppendLine($"{refused} could not be dropped - see the console. Nothing was lost: a style is "
					+ "only ever removed when there is something to inherit it from.");
			}

			return sb.ToString();
		}
	}
}
