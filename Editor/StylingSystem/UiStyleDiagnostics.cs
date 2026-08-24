namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Why a style could not be resolved, in words the person looking at the inspector can act on.
	///
	/// It lives here, and not in the drawer that shows it, because a drawer cannot be tested: an inspector
	/// that is not visible never repaints, so the only way to know these messages are the right ones for the
	/// right situation is to ask for them without a GUI.
	///
	/// The point of every message is to name the ONE thing to change. "No Style assigned yet" was true and
	/// useless: the style was assigned, the skin just had nowhere to resolve it from.
	/// </summary>
	public static class UiStyleDiagnostics
	{
		/// <summary>
		/// The reason no style was resolved, or null when there is nothing to explain - which is the case
		/// when nothing is assigned in the first place. Nothing assigned is a normal state, not a failure.
		/// </summary>
		/// <param name="_config">The config the lookup went through.</param>
		/// <param name="_skin">The skin it went through, null if that skin could not be found either.</param>
		/// <param name="_requestedSkinName">The skin that was asked for. Only used when _skin is null.</param>
		/// <param name="_styleName">The style name stored on the applier.</param>
		/// <param name="_componentTypeName">
		/// The component the applier styles, if known. Worth naming: a style is identified by name AND type,
		/// so the same name can exist for a different component and the bare name would then read as a lie.
		/// </param>
		public static string ExplainMissingStyle
		(
			UiStyleConfig _config,
			UiSkin _skin,
			string _requestedSkinName,
			string _styleName,
			string _componentTypeName = null
		)
		{
			if (string.IsNullOrEmpty(_styleName))
				return null;

			string style = string.IsNullOrEmpty(_componentTypeName)
				? $"'{_styleName}'"
				: $"'{_styleName}' ({_componentTypeName})";

			if (_config == null)
				return $"No style config, so style {style} cannot be resolved.";

			if (_skin == null)
			{
				return string.IsNullOrEmpty(_requestedSkinName)
					? $"'{_config.name}' has no skin selected, so style {style} cannot be resolved."
					: $"'{_config.name}' has no skin '{_requestedSkinName}', so style {style} cannot be "
						+ "resolved. Add that skin to the config.";
			}

			string head = $"Style {style} does not exist in skin '{_skin.Name}' of '{_config.name}'";

			var parentConfig = _config.Parent;
			if (parentConfig == null)
			{
				return head + ", and the config inherits from nothing, so there is nowhere else to look. "
					+ "Styles belong to a skin: add it to this skin, or pick another style above.";
			}

			// The interesting case, and the one that looks like a bug until it is spelled out: the config
			// inherits, but THIS skin has no counterpart to inherit from, so it stands alone inside a config
			// that otherwise does not.
			var parentSkin = _skin.ParentSkin;
			if (parentSkin == null)
			{
				return head + $", and the skin inherits nothing: '{parentConfig.name}' has no skin "
					+ $"'{_skin.EffectiveInheritFromSkinName}'. Set 'Inherits skin from' on skin "
					+ $"'{_skin.Name}' to the skin it should build on.";
			}

			return head + $", and not in what that skin inherits either (skin '{parentSkin.Name}' of "
				+ $"'{parentSkin.StyleConfig.name}').";
		}

		/// <summary>
		/// Why the style shown cannot be edited although it resolved: the skin it comes from is not one this
		/// config declares, so the whole skin - and everything in it - belongs to an ancestor. There is no
		/// own skin to copy into, so an override is not offered; saying why beats a button that would write
		/// into the other asset.
		/// </summary>
		public static string ExplainForeignSkin( UiStyleConfig _config, string _skinName, UiSkin _resolvingSkin )
		{
			if (_config == null || _resolvingSkin == null)
				return null;

			return $"'{_config.name}' does not declare skin '{_skinName}' itself, so it resolves through "
				+ $"'{_resolvingSkin.StyleConfig.name}' as a whole. The style shown belongs to that config "
				+ $"and is read-only here. Add skin '{_skinName}' to '{_config.name}' to override it.";
		}
	}
}
