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

		public static Scope Use( UiStyleConfig _config, UiSkin _skin ) => new Scope(_config, _skin);

		/// <summary>
		/// The config that actually holds this style, seen from the current context: the edited config for
		/// a style of its own, or the ancestor it is inherited from. Null when there is no context, or when
		/// the style resolves from nowhere.
		/// </summary>
		public static UiStyleConfig OwnerOf( UiAbstractStyleBase _style )
		{
			if (_style == null || Skin == null)
				return null;

			return Skin.ConfigOwning(_style.Key);
		}

		/// <summary>
		/// Whether this style comes from somewhere else than the config being edited - the one question the
		/// three row states, the read-only display and the override action all hang off.
		/// </summary>
		public static bool IsInherited( UiAbstractStyleBase _style )
		{
			var owner = OwnerOf(_style);
			return owner != null && Config != null && owner != Config;
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

		public readonly struct Scope : IDisposable
		{
			private readonly UiStyleConfig m_previousConfig;
			private readonly UiSkin m_previousSkin;

			public Scope( UiStyleConfig _config, UiSkin _skin )
			{
				m_previousConfig = Config;
				m_previousSkin = Skin;
				Config = _config;
				Skin = _skin;
			}

			public void Dispose()
			{
				Config = m_previousConfig;
				Skin = m_previousSkin;
			}
		}
	}
}
