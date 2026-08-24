using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[EditorAware]
	public abstract class UiAbstractApplyStyleBase : AbstractEditorAwareMonoBehaviour
	{
		[FormerlySerializedAs("m_isResolutionDependent")] 
		[SerializeField] [HideInInspector] private bool m_isAspectRatioDependent;
		[FormerlySerializedAs("m_config")] 
		[SerializeField][HideInInspector] private UiStyleConfig m_optionalStyleConfig;
		[SerializeField][HideInInspector] private string m_name;
		[SerializeField][HideInInspector] private string m_fixedSkinName;
		[SerializeField][HideInInspector] protected bool m_tweenable = true;
		[SerializeField][HideInInspector] private bool m_rebuildLayoutOnApply = false;
		[SerializeField][HideInInspector] protected int m_frameDelay = 0;
		
		protected UiAbstractStyleBase m_style;

		private bool m_skinListenersAdded = false;
		private UiStyleConfig m_effectiveStyleConfig;
		private bool m_effectiveStyleConfigInitialized;
#if UNITY_EDITOR
		private int m_effectiveStyleConfigGeneration = -1;
		private static int s_styleConfigGeneration;

		/// <summary>
		/// Drops the resolved style config of every applier. Needed when something outside an applier
		/// changes WHICH config it should use - assigning another main config in the toolkit configuration,
		/// or cloning one into the project. Editing a config's contents does not need this; that is what
		/// EvSkinChanged and EvSkinValuesChanged are for.
		/// </summary>
		public static void InvalidateEffectiveStyleConfigs() => s_styleConfigGeneration++;

		/// <summary>
		/// The config an applier resolves to is derived from two serialized fields of its own, so any
		/// inspector edit, undo or prefab apply that touches them has to drop the cached result.
		/// </summary>
		protected virtual void OnValidate() => m_effectiveStyleConfigInitialized = false;
#endif

		public UnityEvent<UiAbstractApplyStyleBase> OnBeforeApplyStyle = new();
		public UnityEvent<UiAbstractApplyStyleBase> OnAfterApplyStyle = new();
		
		public abstract Type SupportedComponentType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract Component Component { get; }
		public abstract int Key { get; }
		public abstract void ResetKey();
		
		public bool Tweenable
		{
			get => m_tweenable && !SkinIsFixed;
			set => m_tweenable = value;
		}

		public bool RebuildLayoutOnApply
		{
			get => m_rebuildLayoutOnApply;
			set => m_rebuildLayoutOnApply = value;
		}

		public bool IsAspectRatioDependent => m_isAspectRatioDependent;

		/// <summary>
		/// The config this applier resolves its style from.
		///
		/// This used to throw its cache away on every access while not playing, on the assumption that
		/// anything may have changed in the editor. It made a single style resolution cost ~290 us, nearly
		/// all of it spent walking back up the singleton chain (three editor-caller-gate stack walks and
		/// two bootstrap checks per access) - and with one resolution per applier per skin change, a scene
		/// with ~850 appliers spent a third of a second re-answering a question whose answer had not
		/// changed. It is cached now and invalidated explicitly: OnValidate covers edits to this
		/// component's own fields, and InvalidateEffectiveStyleConfigs covers a different config being
		/// assigned project-wide.
		///
		/// The EditorCallerGate check that used to sit here was removed rather than cached: this class is
		/// [EditorAware], so its own frame is on the stack for every access and the check could never
		/// fail. The AssetReadyGate check stays - that one does fire, during import and compile.
		/// </summary>
		public UiStyleConfig StyleConfig
		{
			get
			{
				AssetReadyGate.ThrowIfNotReady();
#if UNITY_EDITOR
				if (!Application.isPlaying && m_effectiveStyleConfigGeneration != s_styleConfigGeneration)
					m_effectiveStyleConfigInitialized = false;
#endif
				if (!m_effectiveStyleConfigInitialized)
				{
					m_effectiveStyleConfigInitialized = true;
#if UNITY_EDITOR
					m_effectiveStyleConfigGeneration = s_styleConfigGeneration;
#endif

					if (m_optionalStyleConfig != null)
					{
						m_effectiveStyleConfig = m_optionalStyleConfig;
						return m_effectiveStyleConfig;
					}

					if (m_isAspectRatioDependent)
					{
						m_effectiveStyleConfig = UiAspectRatioDependentStyleConfig.Instance;
						if (m_effectiveStyleConfig != null)
							return m_effectiveStyleConfig;
					}

					m_effectiveStyleConfig = UiStyleConfig.Instance;
					m_effectiveStyleConfigInitialized = true;
				}

				return m_effectiveStyleConfig;
			}
		}

		public bool SkinIsFixed => !string.IsNullOrEmpty(FixedSkinName);

		public string FixedSkinName
		{
			get => m_fixedSkinName;
			set 
			{
				if (m_fixedSkinName == value)
					return;
				
				m_fixedSkinName = value;
				SetSkinListeners(!SkinIsFixed);
				SetStyle();
				Apply();
			}
		}

		protected override void SafeAwake()
		{
			m_style = null;
			SetStyle();
			Apply();
			UiEventDefinitions.EvStyleApplierCreated.Invoke(this);
		}

		public virtual void OnDestroy()
		{
			UiEventDefinitions.EvStyleApplierDestroyed.Invoke(this);
		}

		protected virtual void OnTransformParentChanged() => UiEventDefinitions.EvStyleApplierChangedParent.Invoke(this);

		protected override void SafeOnEnable()
		{
			UiEventDefinitions.EvScreenResolutionChange.AddListener(OnScreenResolutionChanged);
			SetSkinListeners(!SkinIsFixed);

			if (Component == null)
				return;

			SetStyle();
			Apply();
		}

		protected virtual void OnDisable()
		{
			UiEventDefinitions.EvScreenResolutionChange.RemoveListener(OnScreenResolutionChanged);
			SetSkinListeners(false);
		}
		
		private void OnScreenResolutionChanged(ScreenResolution _oldScreenResolution, ScreenResolution _newScreenResolution)
		{
			Apply();
		}

		public UiAbstractStyleBase Style
		{
			get
			{
				if (m_style == null)
					SetStyle();

				return m_style;
			}
		}

		public void Reset(bool _alsoStyleConfig = false)
		{
			if (_alsoStyleConfig)
			{
				m_isAspectRatioDependent = false;
				m_optionalStyleConfig = null;
			}

			m_name = null;
			m_style = null;
			m_fixedSkinName = null;
			ResetKey();
		}
		
		public void Apply()
		{
			if (Application.isPlaying && m_frameDelay > 0)
			{
				CoRoutineRunner.Instance.StartCoroutine(ApplyDelayed());
				return;
			}

			ApplyInternal();
		}

		IEnumerator ApplyDelayed()
		{
			for (int i = 0; i < m_frameDelay; i++)
				yield return null;

			ApplyInternal();
		}

		private void ApplyInternal()
		{
			if (enabled && CheckCondition())
			{
				OnBeforeApplyStyle.Invoke(this);
				ApplyImpl();
				if (m_rebuildLayoutOnApply && transform is RectTransform targetRectTransform)
					LayoutRebuilder.ForceRebuildLayoutImmediate(targetRectTransform);
				OnAfterApplyStyle.Invoke(this);
			}
		}

		/// <summary>
		/// Writes the component's current values into its style. Materialises the style first if it is
		/// inherited - otherwise the write would land in the parent config, and for the copy that ships
		/// inside the package it would be dropped on save without a word.
		/// </summary>
		public void Record()
		{
#if UNITY_EDITOR
			MaterializeStyleForOverride();
#endif
			if (CheckCondition())
				RecordImpl();

#if UNITY_EDITOR
			EditorGeneralUtility.SetDirty(StyleConfig);
			AssetDatabase.SaveAssets();
			UiEventDefinitions.EvSkinValuesChanged.Invoke(1);
#endif
		}

#if UNITY_EDITOR
		/// <summary>
		/// The skin this applier resolves through, as DECLARED by its own config.
		///
		/// Own, because it is the skin an override would be written into: materialising into a skin that
		/// itself comes from the parent would write into the parent asset, the thing all of this prevents.
		/// Null when the config does not declare that skin at all, which can only happen for a fixed skin.
		/// </summary>
		public UiSkin OwnSkin
		{
			get
			{
				var styleConfig = StyleConfig;
				if (styleConfig == null)
					return null;

				return SkinIsFixed
					? styleConfig.GetOwnSkinByNameOrAlias(FixedSkinName, false)
					: styleConfig.CurrentSkin;
			}
		}

		/// <summary>
		/// The skin the lookup actually goes through, which may be an ancestor's: a fixed skin this config
		/// does not declare resolves through the parent AS A WHOLE. Mirrors FindStyle(), and differs from
		/// OwnSkin in exactly that case - which is the case where nothing here can be edited.
		/// </summary>
		public UiSkin ResolvingSkin
		{
			get
			{
				var styleConfig = StyleConfig;
				if (styleConfig == null)
					return null;

				return SkinIsFixed
					? styleConfig.GetSkinByName(FixedSkinName)
					: styleConfig.CurrentSkin;
			}
		}

		/// <summary>
		/// Makes sure the style this applier resolves belongs to its own config, copying it out of the
		/// parent if it does not, and re-resolves so Style points at that copy. Returns the style to write
		/// to, or null if there is none at all.
		///
		/// Public because this is exactly what an "override this inherited style" action needs - the value
		/// editing in the inspector will come through the same entry point.
		/// </summary>
		public UiAbstractStyleBase MaterializeStyleForOverride()
		{
			var styleConfig = StyleConfig;
			if (styleConfig == null)
				return null;

			var skin = OwnSkin;
			if (skin == null)
			{
				UiLog.LogError($"'{styleConfig.name}' does not declare skin '{FixedSkinName}' itself, so style " +
				               $"'{m_name}' cannot be overridden in it. Add that skin to the config first.", this);
				return null;
			}

			// Returns the own style when there already is one, so this covers both cases.
			var materialized = skin.MaterializeStyle(Key);
			if (materialized == null)
				return null;

			if (!ReferenceEquals(materialized, m_style))
				SetStyle();   // Style caches the resolved instance, and it is the old one now

			return materialized;
		}
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool CheckCondition() => Style != null;

		protected abstract void ApplyImpl();
		protected abstract void RecordImpl();

		public abstract UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null);

		public string Name
		{
			get => m_name;
			set
			{
				if (m_name == value)
					return;

				ResetKey();
				m_name = value;
				SetStyle();
			}
		}

		public UiAbstractStyleBase FindStyle()
		{
			var styleConfig = StyleConfig;

			// A missing style config is a project setup problem, not a per-component one.
			// Without this guard the two dereferences below throw a NullReferenceException
			// for every styled element the moment it is instantiated, so one authored screen
			// buries the console in stack traces that name the styling internals and never the
			// actual cause. LogErrorOnce keeps it to a single actionable message per session.
			if (styleConfig == null)
			{
				UiLog.LogErrorOnce(
					$"No {nameof(UiStyleConfig)} is assigned, so no style can be resolved. " +
					$"First affected: style '{m_name}' on GameObject '{name}'. " +
					$"To fix, open '{StringConstants.CONFIGURATION_MENU_NAME}': it picks up the default " +
					$"config shipped with the package, assigns it, and offers 'Clone' to create a " +
					$"project-owned copy under 'Assets/Resources/'. Assigning the style config field on " +
					$"the {nameof(UiToolkitConfiguration)} asset by hand works too, as does setting an " +
					$"individual config on this component.",
					this);
				return null;
			}

			UiSkin currentSkin = SkinIsFixed ?
				styleConfig.GetSkinByName(m_fixedSkinName) :
				styleConfig.CurrentSkin;

			if (currentSkin == null)
				return null;

			return currentSkin.StyleByKey(Key);
		}

		public void SetStyle()
		{
			m_style = FindStyle();
			if (m_style != null)
				m_name = m_style.Name;
		}
		
		public void OnSkinValuesChanged(float _) => Apply();

		public void OnSkinChanged(float _)
		{
#if UNITY_EDITOR
			bool isDirty = EditorUtility.IsDirty(this);
			bool isComponentDirty = EditorUtility.IsDirty(Component);
#endif
			SetStyle();
			Apply();

#if UNITY_EDITOR
			if (!isDirty)
				EditorUtility.ClearDirty(this);
			if (!isComponentDirty)
				EditorUtility.ClearDirty(Component);
#endif
		}
		
		
		public void SetSkinListeners(bool value)
		{
			if (m_skinListenersAdded == value)
				return;

			if (value)
			{
				UiEventDefinitions.EvSkinChanged.AddListener(OnSkinChanged);
				UiEventDefinitions.EvSkinValuesChanged.AddListener(OnSkinValuesChanged);
			}
			else
			{
				UiEventDefinitions.EvSkinChanged.RemoveListener(OnSkinChanged);
				UiEventDefinitions.EvSkinValuesChanged.RemoveListener(OnSkinValuesChanged);
			}

			m_skinListenersAdded = value;
		}
	}
}
