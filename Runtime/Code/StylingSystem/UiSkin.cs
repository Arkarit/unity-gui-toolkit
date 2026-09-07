using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiSkin
	{
		// Config this skin belongs to
		[SerializeField] private UiStyleConfig m_config;
		// The m_name member should never change. It's the identifier of the skin and is only ever set in ctor.
		[SerializeField] private string m_name;
		// m_alias can be changed and used for display purposes.
		[SerializeField] private string m_alias;
		[NonReorderable][SerializeReference] private List<UiAbstractStyleBase> m_styles = new();
		[FormerlySerializedAs("m_aspectRatioGE")] [SerializeField] private float m_aspectRatioGreaterEqual = 0;

		/// <summary>
		/// Which skin of the parent config this one builds on. Empty means "the one with the same name",
		/// which is what matches most of the time and needs saying nowhere.
		///
		/// It needs saying when the names differ, and that is the normal case for a project's own skin: a
		/// client config with skins Default and BOTW inherits from a package config with Default and Light,
		/// so BOTW would find no counterpart and inherit nothing at all. Naming Default here lets it build on
		/// the package's Default like any other skin.
		/// </summary>
		[SerializeField] private string m_inheritFromSkinName;
		[SerializeField] private bool m_inheritFromSameConfig;

		/// <summary>
		/// The styles this skin deliberately does NOT have, although it would inherit them - the pendant to
		/// a prefab instance's removed component, and the one thing a child could not say before: "the
		/// config we build on has this, we do not".
		///
		/// Not the same as turning every property off, which is what one reaches for otherwise. A style
		/// resolves as a whole from the nearest skin that OWNS it, so an override with nothing applicable
		/// keeps the parent's values from ever being asked for, and it stays in the vocabulary that fills
		/// every style popup. Worse, IsApplicable is a property of the style DEFINITION rather than of one
		/// skin: EvStyleApplicableChanged synchronises it across every same-named style of the config, so
		/// switching a property off in one skin switches it off in its siblings too. A removal is stored
		/// per skin and costs one line.
		/// </summary>
		[SerializeField] private List<UiSuppressedStyle> m_suppressedStyles = new();


		private Dictionary<int, UiAbstractStyleBase> m_styleByKey;
		// Not initialised here, and not readonly, for the same reason m_styleByKey is not: Unity brings a
		// [Serializable] class back without running a constructor, so a field initialiser would leave this
		// null after every reload. It is built in BuildDictionary, which every reader goes through.
		private HashSet<int> m_suppressedKeys;
		// Shape of the style list the lookup was built from - see BuildDictionaryIfNecessary.
		private int m_builtStyleCount = -1;
		private int m_builtSuppressedCount = -1;
		private UiAbstractStyleBase m_builtFirstStyle;
		private UiAbstractStyleBase m_builtLastStyle;
		private static readonly List<int> m_stylesToRemove = new();

		public UiSkin(UiStyleConfig _config, string _name, float _aspectRatioGreaterEqual = -1 ) 
		{
			m_config = _config;
			m_name = _name;
			m_aspectRatioGreaterEqual = _aspectRatioGreaterEqual;
			if (!IsAspectRatioDependent && !Mathf.Approximately(-1, _aspectRatioGreaterEqual))
				throw new ArgumentException("Non-Aspect Ratio dependent UiSkins can't have an 'aspect ratio greater than' setting");
		}
		
		public string Name => m_name;
		public string Alias
		{
			get
			{
				if (string.IsNullOrEmpty(m_alias))
					return m_name;
				
				return m_alias;
			}
			
			set => m_alias = value;
		}

		public List<UiAbstractStyleBase> Styles => m_styles;
		public UiStyleConfig StyleConfig => m_config;

		/// <summary>
		/// What this skin has removed from what it inherits. Read-only: the two operations below are the
		/// way in, because both of them have cases they have to refuse.
		/// </summary>
		public IReadOnlyList<UiSuppressedStyle> SuppressedStyles
			=> m_suppressedStyles ?? (IReadOnlyList<UiSuppressedStyle>) Array.Empty<UiSuppressedStyle>();

		/// <summary>
		/// How many removals this skin holds. Part of what the editor keys its row cache on: removing a
		/// style changes which rows are shown without changing the style list at all.
		/// </summary>
		public int SuppressedCount => m_suppressedStyles != null ? m_suppressedStyles.Count : 0;

		/// <summary>
		/// Whether anything was ever decided in this skin: a style of its own, a style removed from what it
		/// inherits, a display name, or a skin it was told to build on.
		///
		/// Asked before throwing a skin away - a child config's skins may exist for no other reason than to
		/// have something to override into, and those are worth exactly nothing once there is nothing to
		/// override. Everything above is a decision somebody made and would not expect to lose.
		///
		/// The aspect ratio threshold is deliberately not in here: in an aspect-ratio-dependent config every
		/// skin carries one, it is how the skin is SELECTED rather than something put into it, and counting
		/// it would make such a skin impossible to clean up again.
		/// </summary>
		public bool HasOwnContent =>
			   m_styles.Count > 0
			|| SuppressedCount > 0
			|| !string.IsNullOrEmpty(m_alias)
			|| !string.IsNullOrEmpty(m_inheritFromSkinName)
			|| m_inheritFromSameConfig;

		/// <summary>
		/// Which skin of the parent this one inherits from. Empty (the default) means the same name.
		/// </summary>
		public string InheritFromSkinName
		{
			get => m_inheritFromSkinName;
			set
			{
				var wanted = string.IsNullOrWhiteSpace(value) ? null : value;
				if (m_inheritFromSkinName == wanted)
					return;

				m_inheritFromSkinName = wanted;
				InvalidateStyleLookup();
#if UNITY_EDITOR
				if (m_config != null)
					EditorGeneralUtility.SetDirty(m_config);
#endif
			}
		}

		/// <summary>
		/// Whether the skin named above is one of THIS config's, rather than one of the parent's.
		///
		/// A skin is often a variant of another skin next to it, not of anything in the parent: measured on
		/// the client, its BOTW skin shares 50 of 80 styles with its own Default and only 44 with the
		/// package's - and the two own skins hold exactly the same set of styles, which the package's do not.
		/// Overrides against the sibling then also mean what they say: "differs from our own look".
		///
		/// Stored as its own flag rather than guessed from the name, because a name can exist on both sides
		/// and guessing would take the choice away.
		/// </summary>
		public bool InheritFromSameConfig
		{
			get => m_inheritFromSameConfig;
			set
			{
				if (m_inheritFromSameConfig == value)
					return;

				m_inheritFromSameConfig = value;
				InvalidateStyleLookup();
#if UNITY_EDITOR
				if (m_config != null)
					EditorGeneralUtility.SetDirty(m_config);
#endif
			}
		}

		/// <summary>
		/// The name this skin looks for in the parent - its own unless told otherwise.
		/// </summary>
		public string EffectiveInheritFromSkinName =>
			string.IsNullOrEmpty(m_inheritFromSkinName) ? m_name : m_inheritFromSkinName;

		/// <summary>
		/// The skin this one builds on: one of this config's own when told so, one of the parent's otherwise.
		/// Null when there is nothing of that name.
		///
		/// Resolving one hop at a time is what lets every level of a chain map to a different name - and now
		/// also to a different config, since a hop may stay inside this one.
		/// </summary>
		public UiSkin ParentSkin
		{
			get
			{
				if (m_config == null)
					return null;

				if (m_inheritFromSameConfig)
				{
					// No implicit same-name fallback here: within one config that would mean the skin builds
					// on itself, which is the one thing it cannot do.
					if (string.IsNullOrEmpty(m_inheritFromSkinName) || m_inheritFromSkinName == m_name)
						return null;

					return m_config.GetOwnSkinByNameOrAlias(m_inheritFromSkinName, false);
				}

				return m_config.Parent?.GetOwnSkinByNameOrAlias(EffectiveInheritFromSkinName, false);
			}
		}

		/// <summary>
		/// Whether building on that skin would close a circle - which the resolution survives (every walk is
		/// depth-capped) but which makes a style resolve from somewhere nobody intended. Asked by the editor
		/// so the choice is not offered in the first place.
		/// </summary>
		public bool WouldInheritingFromCreateACycle( UiSkin _candidate )
		{
			if (_candidate == null)
				return false;

			if (_candidate == this)
				return true;

			foreach (var skin in _candidate.SelfAndInheritedSkins())
			{
				if (skin == this)
					return true;
			}

			return false;
		}
		public bool IsAspectRatioDependent => m_config is UiAspectRatioDependentStyleConfig;
		public float AspectRatioGreaterEqual => m_aspectRatioGreaterEqual;

		public void Init(UiStyleConfig _config)
		{
			Validate(_config);
			foreach (var style in m_styles)
				style.Init();

			BuildDictionary();
			PruneStaleSuppressions();
		}

		/// <summary>
		/// Drops removals of styles that are no longer offered from anywhere - the parent deleted the style,
		/// or the skin now builds on something else. Such an entry hides nothing and only accumulates.
		///
		/// In memory only, deliberately: this runs from UiStyleConfig.OnEnable, and setting the asset dirty
		/// from there is how a project-wide save avalanche starts (see the note on that OnEnable). The next
		/// save the asset gets for any other reason writes the pruned list.
		///
		/// Guarded on there BEING a parent skin, and that guard is the whole safety of it: while assets are
		/// still loading the parent may not be reachable yet, and "nothing is inherited" would then look
		/// like every removal is stale.
		/// </summary>
		private void PruneStaleSuppressions()
		{
			if (m_suppressedStyles == null || m_suppressedStyles.Count == 0)
				return;

			var parentSkin = ParentSkin;
			if (parentSkin == null)
				return;

			bool removedAny = false;
			for (int i = m_suppressedStyles.Count - 1; i >= 0; i--)
			{
				if (parentSkin.StyleByKey(m_suppressedStyles[i].Key) != null)
					continue;

				m_suppressedStyles.RemoveAt(i);
				removedAny = true;
			}

			if (removedAny)
				InvalidateStyleLookup();
		}

		public UiAbstractStyleBase StyleByName<T>(string _name) where T:Component
		{
			BuildDictionaryIfNecessary();

			var key = UiStyleUtility.GetKey(typeof(T), _name);
			return StyleByKey(key);
		}

		public CT StyleByName<T,CT>(string _name) 
			where CT:UiAbstractStyleBase 
			where T:Component
		{
			return (CT) StyleByName<T>(_name);
		}

		/// <summary>
		/// The style behind this key: this skin's own, or, failing that, the one inherited from the
		/// same-named skin of an ancestor config. This is the single place where a style is resolved at
		/// runtime, which is what makes inheritance cheap - appliers store a name and a key, never a
		/// reference, so nothing serialized has to change for a lookup to reach further.
		/// </summary>
		public UiAbstractStyleBase StyleByKey(int _key) => StyleByKey(_key, 0);

		private UiAbstractStyleBase StyleByKey(int _key, int _depth)
		{
			var own = OwnStyleByKey(_key);
			if (own != null)
				return own;

			// A style removed here stops the walk - that is the entire point of removing it. Asked after
			// the own lookup, so a skin that somehow holds both keeps resolving its own copy rather than
			// nothing; SuppressStyle drops an own copy when it takes one on, so that cannot arise from here.
			if (SuppressesStyle(_key))
				return null;

			if (_depth + 1 >= UiStyleConfig.MaxInheritanceDepth)
			{
				UiLog.LogErrorOnce($"Skin '{m_name}' inherits more than {UiStyleConfig.MaxInheritanceDepth} " +
				                   "levels deep, or the chain forms a cycle; anything beyond that is ignored.");
				return null;
			}

			return ParentSkin?.StyleByKey(_key, _depth + 1);
		}

		/// <summary>
		/// Whether this skin holds the style behind this key itself, as opposed to inheriting it. The
		/// question to ask before writing: an inherited style belongs to another asset.
		/// </summary>
		public bool OwnsStyle(int _key) => OwnStyleByKey(_key) != null;

		/// <summary>
		/// Whether this skin has removed the style behind this key from what it inherits. The editor still
		/// draws such a row - a removal that cannot be seen cannot be taken back - so anything that draws
		/// or resolves has to ask this rather than conclude from a row's presence that there is a style.
		/// </summary>
		public bool SuppressesStyle(int _key)
		{
			BuildDictionaryIfNecessary();
			return m_suppressedKeys.Contains(_key);
		}

		/// <summary>
		/// Removes an inherited style from THIS skin: it stops resolving here, while the config it comes
		/// from keeps it untouched. <see cref="RestoreSuppressedStyle"/> takes it back.
		///
		/// Refuses what it cannot mean. Nothing inherited behind the key is either a style nobody has, or
		/// the only copy of one - and dropping the only copy is a deletion, which is EvDeleteStyle's job
		/// and says so. A config inside the package is refused for the usual reason: its saves are
		/// discarded without a word, so the removal would be gone after the next reload.
		///
		/// An own copy is dropped along the way. A skin that both overrode a style and removed it would be
		/// saying two opposite things, and the values in that copy are exactly what the caller just asked
		/// to be rid of - the dialog in the editor says so before it gets here.
		/// </summary>
		public bool SuppressStyle(int _key)
		{
			if (SuppressesStyle(_key))
				return true;

			var inherited = InheritedStyleByKey(_key);
			if (inherited == null)
			{
				var own = OwnStyleByKey(_key);
				UiLog.LogError(own != null
					? $"Style '{own.Name}' is not inherited from anywhere, so it cannot be removed in skin " +
					  $"'{m_name}' - it only exists here. Delete it if that is what you mean."
					: $"Skin '{m_name}' inherits no style behind key {_key}, so there is nothing to remove.");
				return false;
			}

#if UNITY_EDITOR
			if (m_config != null && m_config.IsPackageOwned)
			{
				UiLog.LogError($"Cannot remove '{inherited.Name}' in '{m_config.name}': that config ships " +
				               "inside the package and is read-only, so the removal would be lost on save.");
				return false;
			}
#endif

			var ownCopy = OwnStyleByKey(_key);
			if (ownCopy != null)
				m_styles.Remove(ownCopy);

			// A skin can arrive without the list: the config's JSON import builds its skins through
			// JsonUtility, which leaves a list that was empty when it was written as null.
			m_suppressedStyles ??= new List<UiSuppressedStyle>();
			m_suppressedStyles.Add(new UiSuppressedStyle(ownCopy ?? inherited));
			InvalidateStyleLookup();

#if UNITY_EDITOR
			if (m_config != null)
				EditorGeneralUtility.SetDirty(m_config);
#endif
			return true;
		}

		/// <summary>
		/// Takes a removal back, so the style is inherited again. The pendant to Revert on a prefab
		/// instance's removed component, and the reason a removed row stays visible at all.
		/// </summary>
		public bool RestoreSuppressedStyle(int _key)
		{
			if (!SuppressesStyle(_key))
				return false;

#if UNITY_EDITOR
			if (m_config != null && m_config.IsPackageOwned)
			{
				UiLog.LogError($"Cannot change '{m_config.name}': that config ships inside the package and " +
				               "is read-only, so the change would be lost on save.");
				return false;
			}
#endif

			DropSuppression(_key);

#if UNITY_EDITOR
			if (m_config != null)
				EditorGeneralUtility.SetDirty(m_config);
#endif
			return true;
		}

		private bool DropSuppression(int _key)
		{
			if (m_suppressedStyles == null)
				return false;

			for (int i = 0; i < m_suppressedStyles.Count; i++)
			{
				if (m_suppressedStyles[i].Key != _key)
					continue;

				m_suppressedStyles.RemoveAt(i);
				InvalidateStyleLookup();
				return true;
			}

			return false;
		}

		/// <summary>
		/// Copy-on-write: makes an inherited style this skin's own, so it can be written to.
		///
		/// A resolved inherited style IS the parent's instance - styles are [SerializeReference] objects
		/// living inside their config asset, and resolution hands out the real one. Writing to it therefore
		/// edits the parent, and if the parent is the config that ships with the package, the save is
		/// silently dropped (see SkipSavingInPackageFolder): the change appears to work and is gone after
		/// the next reload. Every write path has to come through here first.
		///
		/// Returns the style to write to: the existing own one, the fresh copy, or - if there is nothing to
		/// materialise or nowhere to put it - the inherited one unchanged, so a caller never gets null
		/// where it previously had a style.
		/// </summary>
		public UiAbstractStyleBase MaterializeStyle(int _key)
		{
			var own = OwnStyleByKey(_key);
			if (own != null)
				return own;

			if (m_config == null)
				return null;

			var inherited = InheritedStyleByKey(_key);
			if (inherited == null)
				return null;

#if UNITY_EDITOR
			if (m_config.IsPackageOwned)
			{
				UiLog.LogError($"Cannot override style '{inherited.Name}' in '{m_config.name}': that config " +
				               "ships inside the package and is read-only, so the override would be lost on " +
				               "save. Clone it into the project first and inherit from the package copy.");
				return inherited;
			}
#endif

			var clone = UiStyleUtility.CloneStyle(inherited, m_config);
			if (clone == null)
				return inherited;

			// Wanting an own value here supersedes having removed the style: the two cannot both hold, and
			// this is the more recent of the two statements. Matters for the callers that materialise
			// without having drawn a row first - the AI style writer does exactly that.
			DropSuppression(_key);

			m_styles.Add(clone);
			InvalidateStyleLookup();

#if UNITY_EDITOR
			EditorGeneralUtility.SetDirty(m_config);
#endif
			return clone;
		}

		/// <summary>
		/// What this skin would inherit for this key, ignoring whatever it owns itself. Null when no
		/// ancestor offers it - which is what tells an override apart from a style of one's own.
		/// </summary>
		public UiAbstractStyleBase InheritedStyleByKey(int _key) => ParentSkin?.StyleByKey(_key);

		/// <summary>
		/// The opposite of MaterializeStyle: drops this skin's own copy so the style is inherited again.
		///
		/// Refuses when there is nothing to fall back to. Removing the only copy of a style is a deletion,
		/// not a revert, and it would silently take the style out of the config - DeleteStyle is the way to
		/// say that on purpose.
		///
		/// Returns the style the skin resolves afterwards: the inherited one on success, the own one when
		/// the revert was refused.
		/// </summary>
		public UiAbstractStyleBase RevertStyleToInherited(int _key)
		{
			var inherited = InheritedStyleByKey(_key);
			var own = OwnStyleByKey(_key);

			if (own == null)
				return inherited;   // already inherited (or unknown), nothing to drop

			if (inherited == null)
			{
				UiLog.LogError($"Style '{own.Name}' is not inherited from anywhere, so it cannot be reverted - " +
				               "dropping it would remove it from the config altogether. Delete it if that is " +
				               "what you mean.");
				return own;
			}

#if UNITY_EDITOR
			if (m_config.IsPackageOwned)
			{
				UiLog.LogError($"Cannot change '{m_config.name}': that config ships inside the package and is " +
				               "read-only, so the change would be lost on save.");
				return own;
			}
#endif

			m_styles.Remove(own);
			InvalidateStyleLookup();

#if UNITY_EDITOR
			EditorGeneralUtility.SetDirty(m_config);
#endif
			return inherited;
		}

		/// <summary>
		/// The config that actually holds the style behind this key - this skin's own config, or the
		/// ancestor it is inherited from. Null if nothing resolves. What the editor needs in order to say
		/// where a style comes from.
		/// </summary>
		public UiStyleConfig ConfigOwning(int _key) => SkinOwning(_key)?.StyleConfig;

		/// <summary>
		/// The skin that actually holds the style behind this key - this one or the nearest it inherits from.
		///
		/// The skin, not just its config: since a skin may build on a sibling, naming the config no longer
		/// says where a style comes from, and "inherited" can no longer be decided by comparing configs.
		/// </summary>
		public UiSkin SkinOwning(int _key)
		{
			var skin = this;
			for (int depth = 0; skin != null && depth < UiStyleConfig.MaxInheritanceDepth; depth++)
			{
				if (skin.OwnStyleByKey(_key) != null)
					return skin;

				skin = skin.ParentSkin;
			}

			return null;
		}

		/// <summary>
		/// This skin and the ones it inherits from, nearest first. Walking skins rather than configs is what
		/// makes a per-level name mapping work at all.
		/// </summary>
		public List<UiSkin> SelfAndInheritedSkins()
		{
			var result = new List<UiSkin>();
			var skin = this;
			for (int depth = 0; skin != null && depth < UiStyleConfig.MaxInheritanceDepth; depth++)
			{
				result.Add(skin);
				skin = skin.ParentSkin;
			}

			return result;
		}

		/// <summary>
		/// The style behind this key in THIS skin, ignoring any parent config.
		/// </summary>
		internal UiAbstractStyleBase OwnStyleByKey(int _key)
		{
			BuildDictionaryIfNecessary();

			if (m_styleByKey.TryGetValue(_key, out UiAbstractStyleBase result))
			{
				return result;
			}

			return null;
		}

		/// <summary>
		/// Everything this skin resolves to: its own styles plus those inherited from same-named skins up
		/// the chain, with the nearest one winning. Built on demand rather than cached, because it has to
		/// follow changes in every config it draws from, and it is asked for by the editor and by a skin
		/// change - not per frame. Do not put it on a hot path without measuring first.
		/// </summary>
		public List<UiAbstractStyleBase> EffectiveStyles
		{
			get
			{
				var result = new List<UiAbstractStyleBase>(m_styles);
				if (m_config == null)
					return result;

				var seen = new HashSet<int>();
				foreach (var style in result)
					seen.Add(style.Key);

				var chain = SelfAndInheritedSkins();
				for (int i = 0; i < chain.Count; i++)
				{
					// A skin's removals hide the key from everything ABOVE it in the chain, this level
					// included - counted as seen rather than filtered at the end, because a removal halfway
					// up has to stop the walk there and not merely drop the nearest hit.
					foreach (var suppressed in chain[i].SuppressedStyles)
						seen.Add(suppressed.Key);

					if (i == 0)
						continue;

					foreach (var style in chain[i].Styles)
					{
						if (seen.Add(style.Key))
							result.Add(style);
					}
				}

				return result;
			}
		}

		public void DeleteStyle(UiAbstractStyleBase _style)
		{
			for (int i = 0; i < m_styles.Count; i++)
			{
				if (m_styles[i].Key == _style.Key)
				{
					m_styles.RemoveAt(i);
					break;
				}
			}

			BuildDictionary();
		}

		public void SetStyleAlias(UiAbstractStyleBase _style, string _newDisplayName)
		{
			for (int i = 0; i < m_styles.Count; i++)
			{
				if (m_styles[i].Key == _style.Key)
				{
					m_styles[i].Alias = _newDisplayName;
					break;
				}
			}

			BuildDictionary();
		}

		/// <summary>
		/// Rebuilds the key lookup only when the style list actually changed shape.
		///
		/// This used to rebuild unconditionally while not playing, because Styles is a public list that
		/// anything in the editor may add to or remove from, and a stale lookup would hide a style that is
		/// plainly there. The price was steep: every single lookup recomputed one key per style, measured
		/// at ~61 us for a skin with 70 styles - and with one lookup per applier per skin change, that was
		/// the bulk of what made the editor feel slow.
		///
		/// Instead of trusting nobody, the cheap observable facts about the list are remembered: how many
		/// styles it had, and which instances sat at its ends. Adding, removing or replacing a style
		/// changes at least one of them, and a reload replaces the instances wholesale, so all of those
		/// rebuild. The one case this does not see is an in-place replacement in the MIDDLE of the list
		/// that keeps the count - no code path does that today (deletions go through DeleteStyle,
		/// additions append), and code that wants to be explicit can call InvalidateStyleLookup().
		/// </summary>
		private void BuildDictionaryIfNecessary()
		{
			if (m_styleByKey != null && !StyleListChangedShape())
				return;

			BuildDictionary();
		}

		private bool StyleListChangedShape()
		{
			// The removals ride along in this lookup, so a change to them counts as a change of shape.
			// Their count is enough: nothing swaps one removal for another, they are only ever added or
			// dropped, and both move the count.
			if (SuppressedCount != m_builtSuppressedCount)
				return true;

			int count = m_styles.Count;
			if (count != m_builtStyleCount)
				return true;

			if (count == 0)
				return false;

			return !ReferenceEquals(m_styles[0], m_builtFirstStyle)
			    || !ReferenceEquals(m_styles[count - 1], m_builtLastStyle);
		}

		/// <summary>
		/// Forces the key lookup to be rebuilt on next access. Only needed for a change the shape check
		/// cannot see, i.e. swapping a style in the middle of the list for another one.
		/// </summary>
		public void InvalidateStyleLookup() => m_builtStyleCount = -1;

		private void BuildDictionary()
		{
			if (m_styleByKey == null)
				m_styleByKey = new Dictionary<int, UiAbstractStyleBase>(m_styles.Count);
			
			m_styleByKey.Clear();

			foreach (var style in m_styles)
			{
				m_styleByKey.Add(style.Key, style);
			}

			if (m_suppressedKeys == null)
				m_suppressedKeys = new HashSet<int>();

			m_suppressedKeys.Clear();
			if (m_suppressedStyles != null)
			{
				foreach (var suppressed in m_suppressedStyles)
					m_suppressedKeys.Add(suppressed.Key);
			}

			m_builtSuppressedCount = SuppressedCount;
			m_builtStyleCount = m_styles.Count;
			m_builtFirstStyle = m_builtStyleCount > 0 ? m_styles[0] : null;
			m_builtLastStyle = m_builtStyleCount > 0 ? m_styles[m_builtStyleCount - 1] : null;
		}

		public void Validate(UiStyleConfig _config)
		{
			bool doSetDirty = false;

			if (m_config != _config)
			{
				m_config = _config;
				doSetDirty = true;
			}

			for (int i=0; i < m_styles.Count; i++)
			{
				var style = m_styles[i];
				if (style == null)
					m_stylesToRemove.Add(i);
			}

			if (m_stylesToRemove.Count > 0)
			{
				string styleIndicesToRemoveStr = string.Empty;
				for (int i = 0; i < m_stylesToRemove.Count; i++)
				{
					var styleIdx = m_stylesToRemove[i];
					styleIndicesToRemoveStr += styleIdx.ToString();
					var isLast = i == m_stylesToRemove.Count - 1;
					styleIndicesToRemoveStr += isLast ? " " : ", ";
				}

				UiLog.LogError($"Styling system: The styles {styleIndicesToRemoveStr} are null and will be removed. This is most likely caused by one or more missing Style/StyleApplier classes pair(s)." + 
				               " Sorry, the exact types of these classes pairs can not be determined here - well, because the styles are null. Please be sure to revert your git changes, if you accidentally deleted it.");

				for (int i = m_stylesToRemove.Count - 1; i >= 0; i--)
				{
					var styleIdx = m_stylesToRemove[i];
					m_styles.RemoveAt(styleIdx);
				}

				m_stylesToRemove.Clear();
				doSetDirty = true;
			}

#if UNITY_EDITOR
			if (!doSetDirty)
				return;

			EditorGeneralUtility.SetDirty(m_config);
			AssetDatabase.SaveAssets();
#endif

		}
	}
}
