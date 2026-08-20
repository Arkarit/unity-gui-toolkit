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


		private Dictionary<int, UiAbstractStyleBase> m_styleByKey;
		// Shape of the style list the lookup was built from - see BuildDictionaryIfNecessary.
		private int m_builtStyleCount = -1;
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
		public bool IsAspectRatioDependent => m_config is UiAspectRatioDependentStyleConfig;
		public float AspectRatioGreaterEqual => m_aspectRatioGreaterEqual;

		public void Init(UiStyleConfig _config)
		{
			Validate(_config);
			foreach (var style in m_styles)
				style.Init();

			BuildDictionary();
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
		public UiAbstractStyleBase StyleByKey(int _key)
		{
			var own = OwnStyleByKey(_key);
			if (own != null)
				return own;

			if (m_config == null)
				return null;

			return m_config.InheritedStyleByKey(m_name, _key);
		}

		/// <summary>
		/// Whether this skin holds the style behind this key itself, as opposed to inheriting it. The
		/// question to ask before writing: an inherited style belongs to another asset.
		/// </summary>
		public bool OwnsStyle(int _key) => OwnStyleByKey(_key) != null;

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

			var inherited = m_config.InheritedStyleByKey(m_name, _key);
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
		public UiAbstractStyleBase InheritedStyleByKey(int _key) => m_config?.InheritedStyleByKey(m_name, _key);

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
			var inherited = m_config?.InheritedStyleByKey(m_name, _key);
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
		public UiStyleConfig ConfigOwning(int _key)
		{
			if (OwnStyleByKey(_key) != null)
				return m_config;

			if (m_config == null)
				return null;

			var chain = m_config.SelfAndAncestors();
			for (int i = 1; i < chain.Count; i++)
			{
				var skin = chain[i].GetOwnSkinByNameOrAlias(m_name, false);
				if (skin?.OwnStyleByKey(_key) != null)
					return chain[i];
			}

			return null;
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

				var chain = m_config.SelfAndAncestors();
				for (int i = 1; i < chain.Count; i++)
				{
					var inheritedSkin = chain[i].GetOwnSkinByNameOrAlias(m_name, false);
					if (inheritedSkin == null)
						continue;

					foreach (var style in inheritedSkin.Styles)
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
