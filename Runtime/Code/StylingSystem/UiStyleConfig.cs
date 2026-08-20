using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiStyleConfig), menuName = StringConstants.CREATE_STYLE_CONFIG)]
	[ExecuteAlways]
	public class UiStyleConfig : ScriptableObject
	{
		public static string ClassName => typeof(UiStyleConfig).Name;
		[NonReorderable][SerializeField] private List<UiSkin> m_skins = new();

		[SerializeField] private int m_currentSkinIdx = 0;

		/// <summary>
		/// The config this one builds on. A child stores only what it overrides; everything else resolves
		/// through the parent, matched by skin name and style key. Null means "stands alone", which is
		/// exactly how every config behaved before inheritance existed.
		/// </summary>
		[SerializeField][Optional] private UiStyleConfig m_parent;

		/// <summary>
		/// Depth limit for the parent chain. One level (project onto package) covers every known case;
		/// longer chains cost nothing to resolve but widen the failure surface, so they are allowed and
		/// capped. The cap also keeps a cycle from recursing forever.
		/// </summary>
		public const int MaxInheritanceDepth = 8;

		public static UiStyleConfig Instance
		{
			get
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				Bootstrap.ThrowIfNotInitialized();
#endif				
				return UiToolkitConfiguration.Instance.UiMainStyleConfig;
			}
		}
		
		public List<UiSkin> Skins
		{
			get => m_skins;
			set
			{
				m_skins = value;
				SetDefaultSkin();
#if UNITY_EDITOR
				EditorGeneralUtility.SetDirty(this);
#endif
			}
		}

		public int CurrentSkinIdx
		{
			get => m_currentSkinIdx;
			set
			{
				if (m_currentSkinIdx == value)
					return;

				if (value > Skins.Count)
				{
					UiLog.LogError($"Skin idx {value} exceeeds skin count {Skins.Count}");
					return;
				}

				m_currentSkinIdx = value;
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			}
		}

		public int NumSkins => m_skins != null ? m_skins.Count : 0;

		public UiStyleConfig Parent
		{
			get => m_parent;
			set
			{
				if (m_parent == value)
					return;

				m_parent = value;
#if UNITY_EDITOR
				SetDirty(this);
#endif
				// Everything resolved so far may resolve differently now.
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			}
		}

		/// <summary>
		/// The style behind this key in the skin of this name, looked up in the ancestors only - the caller
		/// has already failed to find it in its own skin. Walks the chain by skin NAME, because two configs
		/// are not required to list their skins in the same order.
		/// </summary>
		internal UiAbstractStyleBase InheritedStyleByKey( string _skinName, int _key )
		{
			var config = m_parent;
			for (int depth = 1; config != null; depth++)
			{
				var skin = config.GetOwnSkinByNameOrAlias(_skinName, false);
				var style = skin?.OwnStyleByKey(_key);
				if (style != null)
					return style;

				config = config.StepToParent(this, depth);
			}

			return null;
		}

		/// <summary>
		/// One step up the chain, or null at the end of it. Reports a cycle and a runaway chain rather than
		/// recursing into either.
		/// </summary>
		private UiStyleConfig StepToParent( UiStyleConfig _start, int _depth )
		{
			if (m_parent == null)
				return null;

			if (m_parent == _start)
			{
				UiLog.LogErrorOnce($"Style config '{_start.name}' is its own ancestor - the parent chain " +
				                   "forms a cycle and is not followed. Clear one of the parent fields.");
				return null;
			}

			if (_depth + 1 >= MaxInheritanceDepth)
			{
				UiLog.LogErrorOnce($"Style config '{_start.name}' has a parent chain longer than " +
				                   $"{MaxInheritanceDepth}; anything beyond that is ignored.");
				return null;
			}

			return m_parent;
		}

		/// <summary>
		/// This config and its ancestors, nearest first. Allocates, so it belongs on the editor-side paths
		/// (vocabulary, effective sets) rather than in the resolution of a single style.
		/// </summary>
		internal List<UiStyleConfig> SelfAndAncestors()
		{
			var result = new List<UiStyleConfig> { this };
			var config = this;
			for (int depth = 0; ; depth++)
			{
				config = config.StepToParent(this, depth);
				if (config == null)
					return result;

				result.Add(config);
			}
		}

		/// <summary>
		/// Deferred through the AssetReadyGate, because a ScriptableObject's OnEnable runs exactly when
		/// the gate is closed: on asset load and after every domain reload, i.e. while the editor is
		/// still importing or compiling. Two things in here must not happen at that moment:
		///
		/// EvSkinChanged reaches every applier in every loaded scene, and an applier resolving its style
		/// asks for its style config - which the gate refuses by throwing (a NotInitializedException out
		/// of OnEnable, seen when switching back to the editor after a recompile). And UiSkin.Init ->
		/// Validate touches the AssetDatabase and can call SaveAssets() when it has to fix up a skin's
		/// back-reference; a project-wide save triggered from an import is how save avalanches start.
		///
		/// In play mode and in a build WhenReady invokes straight away, so nothing is deferred there.
		/// The derived UiAspectRatioDependentStyleConfig already wraps its whole OnEnable the same way.
		/// </summary>
		protected virtual void OnEnable()
		{
			AssetReadyGate.WhenReady(() =>
			{
				// The gate can open frames later, by which time this asset may be gone.
				if (this == null)
					return;

				foreach (var skin in m_skins)
					skin.Init(this);

				AddListeners();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			});
		}

		protected virtual void OnDisable() => RemoveListeners();

		private void AddListeners()
		{
			RemoveListeners();
			UiEventDefinitions.EvDeleteStyle.AddListener(OnDeleteStyle);
			UiEventDefinitions.EvDeleteSkin.AddListener(OnDeleteSkin);
			UiEventDefinitions.EvSetStyleAlias.AddListener(OnSetStyleAlias);
			UiEventDefinitions.EvSetSkinAlias.AddListener(OnSetSkinAlias);
			UiEventDefinitions.EvAddSkin.AddListener(OnAddSkin);
		}

		private void RemoveListeners()
		{
			UiEventDefinitions.EvDeleteStyle.RemoveListener(OnDeleteStyle);
			UiEventDefinitions.EvDeleteSkin.RemoveListener(OnDeleteSkin);
			UiEventDefinitions.EvSetStyleAlias.RemoveListener(OnSetStyleAlias);
			UiEventDefinitions.EvSetSkinAlias.RemoveListener(OnSetSkinAlias);
			UiEventDefinitions.EvAddSkin.RemoveListener(OnAddSkin);
		}

		public void ForeachSkin( Action<UiSkin> _action )
		{
			foreach (var skin in m_skins)
				_action(skin);
		}

		// First skin is always treated as default skin
		public void SetDefaultSkin()
		{
			if (m_skins == null || m_skins.Count == 0)
				return;

			CurrentSkinIdx = 0;
		}

		/// <summary>
		/// The style vocabulary: which style names exist, inherited ones included. Called "effective"
		/// because that is the distinction that starts to matter once a config has a parent - a child
		/// stores only its overrides, so its own style list is no longer the answer to "which styles are
		/// there". Whoever offers style names to choose from (the dropdown on an applier, the AI catalog)
		/// has to ask this, or inherited styles go missing in the editor while working fine at runtime.
		///
		/// Read from the FIRST skin, as before. Every skin carries the same styles by convention, and the
		/// alternative - the union across all skins - would answer a question nobody asks. A style that
		/// exists only in a later skin therefore stays invisible here; that predates inheritance and is
		/// pinned by a test of its own.
		/// </summary>
		public List<string> EffectiveStyleNames => GetEffectiveStyleNamesByMonoBehaviourType(null, false);
		public List<string> EffectiveStyleAliases => GetEffectiveStyleNamesByMonoBehaviourType(null, true);

		public List<string> GetEffectiveStyleNamesByMonoBehaviourType( Type _monoBehaviourType ) => GetEffectiveStyleNamesByMonoBehaviourType(_monoBehaviourType, false);
		public List<string> GetEffectiveStyleAliasesByMonoBehaviourType( Type _monoBehaviourType ) => GetEffectiveStyleNamesByMonoBehaviourType(_monoBehaviourType, true);

		private List<string> GetEffectiveStyleNamesByMonoBehaviourType( Type _monoBehaviourType, bool _alias )
		{
			List<string> result = new();
			if (m_skins.Count <= 0)
				return result;

			foreach (var style in m_skins[0].EffectiveStyles)
			{
				if (_monoBehaviourType != null && style.SupportedComponentType != _monoBehaviourType)
					continue;

				result.Add(_alias ? style.Alias : style.Name);
			}

			return result;
		}

		public List<string> SkinNames => GetSkinNamesOrAliases(false);

		public List<string> SkinAliases => GetSkinNamesOrAliases(true);

		public List<string> GetSkinNamesOrAliases( bool _isAlias )
		{
			List<string> result = new();
			foreach (var skin in m_skins)
			{
				result.Add(_isAlias ? skin.Alias : skin.Name);
			}

			return result;
		}

		public string CurrentSkinName
		{
			get => GetCurrentSkinNameOrAlias(false);
			set => SetCurrentSkinByNameOrAlias(value, true, false);
		}

		public string CurrentSkinAlias
		{
			get => GetCurrentSkinNameOrAlias(true);
			set => SetCurrentSkinByNameOrAlias(value, true, true);
		}

		public string GetCurrentSkinNameOrAlias( bool _isAlias )
		{
			var currentSkin = CurrentSkin;
			if (currentSkin == null)
				return null;

			return _isAlias ? currentSkin.Alias : currentSkin.Name;
		}

		public UiSkin GetSkinByName( string _name ) => GetSkinByNameOrAlias(_name, false);
		public UiSkin GetSkinByAlias( string _alias ) => GetSkinByNameOrAlias(_alias, true);

		/// <summary>
		/// The skin of this name, from this config or, failing that, from an ancestor - so an applier
		/// pinned to a fixed skin keeps working in a project that overrides only some of the skins.
		///
		/// Caution: the skin returned may belong to the PARENT asset. Reading is what this is for; writing
		/// to it writes into the parent, and for a package config that save is silently discarded. Until
		/// copy-on-write exists, use GetOwnSkinByNameOrAlias where the intent is to modify.
		/// </summary>
		public UiSkin GetSkinByNameOrAlias( string _skinNameOrAlias, bool _isAlias )
		{
			var config = this;
			for (int depth = 0; config != null; depth++)
			{
				var skin = config.GetOwnSkinByNameOrAlias(_skinNameOrAlias, _isAlias);
				if (skin != null)
					return skin;

				config = config.StepToParent(this, depth);
			}

			return null;
		}

		/// <summary>
		/// The skin of this name declared by THIS config, ignoring any parent.
		/// </summary>
		public UiSkin GetOwnSkinByNameOrAlias( string _skinNameOrAlias, bool _isAlias )
		{
			for (int i = 0; i < m_skins.Count; i++)
			{
				var skin = m_skins[i];
				var skinIdentifier = _isAlias ? skin.Alias : skin.Name;
				if (skinIdentifier == _skinNameOrAlias)
					return skin;
			}

			return null;
		}

		public UiAbstractStyleBase GetStyleByName( Type _componentType, string _skinName, string _styleName ) => GetStyleByNameOrAlias(_componentType, _skinName, _styleName, false);
		public UiAbstractStyleBase GetStyleByAlias( Type _componentType, string _skinAlias, string _styleAlias ) => GetStyleByNameOrAlias(_componentType, _skinAlias, _styleAlias, true);

		public UiAbstractStyleBase GetStyleByNameOrAlias( Type _componentType, string _skin, string _style, bool _isAlias )
		{
			var skin = GetSkinByNameOrAlias(_skin, _isAlias);
			if (skin == null)
				return null;

			var styles = skin.Styles;
			foreach (var style in styles)
			{
				if (style.SupportedComponentType != _componentType)
					continue;

				var styleIdentifier = _isAlias ? style.Alias : style.Name;
				if (styleIdentifier == _style)
					return style;
			}

			return null;
		}

		public bool SetCurrentSkinByNameOrAlias( string _skinNameOrAlias, bool _emitEvent, bool _isAlias )
		{
			for (int i = 0; i < m_skins.Count; i++)
			{
				var skin = m_skins[i];
				var skinIdentifier = _isAlias ? skin.Alias : skin.Name;
				if (skinIdentifier == _skinNameOrAlias)
				{
					if (m_currentSkinIdx == i)
						return true;

					m_currentSkinIdx = i;
					if (_emitEvent)
						UiEventDefinitions.EvSkinChanged.InvokeAlways(0);

#if UNITY_EDITOR
					if (!Application.isPlaying)
						EditorGeneralUtility.ForceRefreshEditorUi();
#endif

					return true;
				}
			}

			return false;
		}

		public UiSkin CurrentSkin
		{
			get
			{
				if (m_currentSkinIdx < 0 || m_currentSkinIdx >= m_skins.Count)
					return null;

				return m_skins[m_currentSkinIdx];
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Whether this config is the read-only copy that ships inside the package. Saving it does nothing:
		/// SkipSavingInPackageFolder drops the write without an error, which makes it the worst possible
		/// target for an override - it looks like it worked until the next reload.
		///
		/// Two tests, because the toolkit's own dev app has the package symlinked into Assets/, where asking
		/// about the Packages/ folder says nothing.
		/// </summary>
		public bool IsPackageOwned
		{
			get
			{
				var path = AssetDatabase.GetAssetPath(this);
				if (string.IsNullOrEmpty(path))
					return false;   // in memory only (a test fixture, say) - nobody's package

				if (path.StartsWith("Packages/", StringComparison.Ordinal))
					return true;

				try
				{
					var root = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir();
					return !string.IsNullOrEmpty(root) && path.StartsWith(root, StringComparison.Ordinal);
				}
				catch (Exception)
				{
					// The configuration is not reachable from here (too early, or the caller is not editor
					// aware). The Packages/ test above still holds, so answer with that rather than throw.
					return false;
				}
			}
		}
#endif

		public void Validate()
		{
			foreach (var skin in m_skins)
				skin.Validate(this);
		}

		private void OnAddSkin( UiStyleConfig _styleConfig, UiSkin _newSkin )
		{
			if
			(
				   _styleConfig != this
				|| _newSkin == null
				|| m_skins.Contains(_newSkin)
			)
				return;

			m_skins.Add(_newSkin);

#if UNITY_EDITOR
			SetDirty(this);
#endif

			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		private void OnSetSkinAlias( UiStyleConfig _styleConfig, UiSkin _skin, string _alias )
		{
			if (_styleConfig != this)
				return;

			// Matched by name, not by instance: m_name is the skin's identifier and unique within a
			// config, and a caller may legitimately hand in a detached copy of a skin rather than the
			// instance held here - a property drawer editing List<UiSkin> gets exactly that from
			// SerializedProperty.boxedValue, since UiSkin is a plain [Serializable] class.
			ForeachSkin(skin =>
			{
				if (skin.Name == _skin.Name)
					skin.Alias = _alias;
			});

#if UNITY_EDITOR
			SetDirty(this);
#endif

			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		private void OnSetStyleAlias( UiStyleConfig _styleConfig, UiAbstractStyleBase _style, string _alias )
		{
			if (_styleConfig != this)
				return;

			ForeachSkin(skin =>
			{
				skin.SetStyleAlias(_style, _alias);
			});

#if UNITY_EDITOR
			SetDirty(this);
#endif

			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		private void OnDeleteStyle( UiStyleConfig _styleConfig, UiAbstractStyleBase _style )
		{
			if (_styleConfig != this)
				return;

			ForeachSkin(skin =>
			{
				skin.DeleteStyle(_style);
			});

#if UNITY_EDITOR
			SetDirty(this);
#endif

			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		private void OnDeleteSkin( UiStyleConfig _styleConfig, string _skinName )
		{
			if (_styleConfig != this)
				return;

			for (int i = 0; i < m_skins.Count; i++)
			{
				var skin = m_skins[i];
				if (skin.Name == _skinName)
				{
					m_skins.RemoveAt(i);
					break;
				}
			}

			_styleConfig.CurrentSkinIdx = m_skins.Count > 0 ? 0 : -1;

#if UNITY_EDITOR
			SetDirty(this);
#endif

			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

#if UNITY_EDITOR
		public static void SetDirty( UiStyleConfig _instance )
		{
			if (_instance)
				EditorGeneralUtility.SetDirty(_instance);
		}
#endif
		/// <summary>
		/// Whether a style of this class and name exists, inherited ones included. Same first-skin
		/// convention as the vocabulary above.
		/// </summary>
		public bool StyleExists( Type type, string name )
		{
			if (m_skins.Count == 0)
				return false;

			foreach (var style in m_skins[0].EffectiveStyles)
			{
				if (style.Name == name && style.GetType() == type)
					return true;
			}

			return false;
		}
	}
}
