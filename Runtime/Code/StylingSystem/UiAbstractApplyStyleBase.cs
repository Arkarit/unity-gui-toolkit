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

		public UnityEvent<UiAbstractApplyStyleBase> OnBeforeApplyStyle = new();
		public UnityEvent<UiAbstractApplyStyleBase> OnAfterApplyStyle = new();
		
		public abstract Type SupportedComponentType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract Component Component { get; }
		public abstract int Key { get; }
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

		public UiStyleConfig StyleConfig
		{
			get
			{
				AssetReadyGate.ThrowIfNotReady();
				EditorCallerGate.ThrowIfNotEditorAware(Name);
#if UNITY_EDITOR
				if (!Application.isPlaying)
					m_effectiveStyleConfigInitialized = false;
#endif
				if (!m_effectiveStyleConfigInitialized)
				{
					m_effectiveStyleConfigInitialized = true;

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

		public void Record()
		{
			if (CheckCondition())
				RecordImpl();
			
#if UNITY_EDITOR
			EditorGeneralUtility.SetDirty(StyleConfig);
			AssetDatabase.SaveAssets();
			UiEventDefinitions.EvSkinValuesChanged.Invoke(1);
#endif
		}

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

				m_name = value;
				SetStyle();
			}
		}

		public UiAbstractStyleBase FindStyle()
		{
			UiSkin currentSkin = SkinIsFixed ? 
				StyleConfig.GetSkinByName(m_fixedSkinName) : 
				StyleConfig.CurrentSkin;
			
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
