using System;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Which config and which skin the style rows drawn right now are being edited THROUGH.
	///
	/// A style row cannot answer that from its own SerializedProperty, and for two different reasons. In a
	/// config inspector an inherited row belongs to the parent asset, so its property names that asset and
	/// the parent's skin - which may be called something else than the skin it is shown under. And in an
	/// applier inspector the row is not backed by a config asset at all: the style is drawn through a
	/// throwaway helper ScriptableObject, so the property names that.
	///
	/// So whoever draws style rows says what they are being edited through, and everything else follows
	/// from those two facts. Set it with Use() in a using statement - the scope restores what was there
	/// before, which is what keeps one inspector's context from leaking into the next one's rows.
	/// </summary>
	public static class UiStyleRowContext
	{
		public static UiStyleConfig Config { get; private set; }
		public static UiSkin Skin { get; private set; }

		/// <summary>
		/// Whether whoever draws these rows already says above them where the style comes from.
		///
		/// An applier shows exactly one style and heads it with a line of its own, so a row repeating the
		/// origin says the same thing twice in the same breath. A config inspector has no such line - there
		/// the row IS the only place it can be said, and dropping it would lose it.
		/// </summary>
		public static bool OriginShownAbove { get; private set; }

		public static Scope Use( UiStyleConfig _config, UiSkin _skin, bool _originShownAbove = false )
			=> new Scope(_config, _skin, _originShownAbove);

		/// <summary>
		/// The skin that actually holds this style, seen from the current context: the edited skin for a
		/// style of its own, or the nearest one it is inherited from. Null when there is no context, or when
		/// the style resolves from nowhere.
		/// </summary>
		public static UiSkin SkinOwnerOf( UiAbstractStyleBase _style )
		{
			if (_style == null || Skin == null)
				return null;

			return Skin.SkinOwning(_style.Key);
		}

		/// <summary>
		/// The config that holds this style - the edited one for a style of its own or of a sibling skin, the
		/// ancestor otherwise.
		/// </summary>
		public static UiStyleConfig OwnerOf( UiAbstractStyleBase _style ) => SkinOwnerOf(_style)?.StyleConfig;

		/// <summary>
		/// Whether this style comes from somewhere else than the skin being edited - the one question the
		/// three row states, the read-only display and the override action all hang off.
		///
		/// The SKIN, not the config: a skin may build on a sibling, and then an inherited style belongs to
		/// the very config being edited. Comparing configs would call it own, and the row would write into
		/// the sibling skin - changing the look everybody sees instead of the one being edited.
		/// </summary>
		public static bool IsInherited( UiAbstractStyleBase _style )
		{
			var owner = SkinOwnerOf(_style);
			return owner != null && owner != Skin;
		}

		/// <summary>
		/// Whether this style is the edited config's own AND inherited from somewhere - i.e. an override,
		/// the only state that can drift from what it came from.
		/// </summary>
		public static bool IsOverride( UiAbstractStyleBase _style )
		{
			if (_style == null || Skin == null || Config == null)
				return false;

			return Skin.OwnsStyle(_style.Key) && Skin.InheritedStyleByKey(_style.Key) != null;
		}

		/// <summary>
		/// How to refer to the skin a style comes from. A different config is named by the asset, because
		/// that is what has to be opened to change it; a sibling skin of the same config by its own name,
		/// because naming the config would say nothing there.
		/// </summary>
		public static string SourceName( UiSkin _skin )
		{
			if (_skin == null)
				return "?";

			return _skin.StyleConfig != Config
				? $"'{_skin.StyleConfig.name}'"
				: $"skin '{_skin.Name}'";
		}

		/// <summary>Where an OVERRIDE came from - the skin that still provides it further up the chain.</summary>
		public static string OverriddenSourceName( UiAbstractStyleBase _style )
			=> SourceName(Skin?.ParentSkin?.SkinOwning(_style.Key));

		public readonly struct Scope : IDisposable
		{
			private readonly UiStyleConfig m_previousConfig;
			private readonly UiSkin m_previousSkin;
			private readonly bool m_previousOriginShownAbove;

			public Scope( UiStyleConfig _config, UiSkin _skin, bool _originShownAbove )
			{
				m_previousConfig = Config;
				m_previousSkin = Skin;
				m_previousOriginShownAbove = OriginShownAbove;
				Config = _config;
				Skin = _skin;
				OriginShownAbove = _originShownAbove;
			}

			public void Dispose()
			{
				Config = m_previousConfig;
				Skin = m_previousSkin;
				OriginShownAbove = m_previousOriginShownAbove;
			}
		}
	}
}
