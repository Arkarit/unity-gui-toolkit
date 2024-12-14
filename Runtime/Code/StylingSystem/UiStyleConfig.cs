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
		[NonReorderable][SerializeField] private List<UiSkin> m_skins = new();

		[SerializeField] private int m_currentSkinIdx = 0;

		public List<UiSkin> Skins
		{
			get => m_skins;
			set
			{
				m_skins = value;
				SetDefaultSkin();
#if UNITY_EDITOR
				EditorUtility.SetDirty(this);
#endif
			}
		}
		
		public int CurrentSkinIdx
		{
			get =>  m_currentSkinIdx;
			set
			{
				if (m_currentSkinIdx == value)
					return;
				
				if (value > Skins.Count)
				{
					Debug.LogError($"Skin idx {value} exceeeds skin count {Skins.Count}");
					return;
				}
				
				m_currentSkinIdx = value;
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			}
		}

		public int NumSkins => m_skins != null ? m_skins.Count : 0;

		protected virtual void OnEnable()
		{
			foreach (var skin in m_skins)
				skin.Init(this);

			AddListeners();
			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		protected void OnDisable() => RemoveListeners();

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

		public void ForeachSkin(Action<UiSkin> _action)
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
		
		public List<string> StyleNames => GetStyleNamesByMonoBehaviourType(null, false);
		public List<string> StyleAliases => GetStyleNamesByMonoBehaviourType(null, true);

		public List<string> GetStyleNamesByMonoBehaviourType(Type _monoBehaviourType) => GetStyleNamesByMonoBehaviourType(_monoBehaviourType, false);
		public List<string> GetStyleAliasesByMonoBehaviourType(Type _monoBehaviourType) => GetStyleNamesByMonoBehaviourType(_monoBehaviourType, true);

		private List<string> GetStyleNamesByMonoBehaviourType(Type _monoBehaviourType, bool _alias)
		{
			List<string> result = new();
			if (m_skins.Count <= 0)
				return result;

			var skin = m_skins[0];
			foreach (var style in skin.Styles)
			{
				if (_monoBehaviourType != null && style.SupportedComponentType != _monoBehaviourType)
					continue;

				result.Add(_alias ? style.Alias : style.Name);
			}
			
			return result;
		}

		public List<string> SkinNames => GetSkinNamesOrAliases(false);

		public List<string> SkinAliases => GetSkinNamesOrAliases(true);
		
		public List<string> GetSkinNamesOrAliases(bool _isAlias)
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

		public string GetCurrentSkinNameOrAlias(bool _isAlias)
		{
			var currentSkin = CurrentSkin;
			if (currentSkin == null)
				return null;

			return _isAlias ? currentSkin.Alias : currentSkin.Name;
		}

		public UiSkin GetSkinByName(string _name) => GetSkinByNameOrAlias(_name, false);
		public UiSkin GetSkinByAlias(string _alias) => GetSkinByNameOrAlias(_alias, true);
		public UiSkin GetSkinByNameOrAlias(string _skinNameOrAlias, bool _isAlias)
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
		
		public UiAbstractStyleBase GetStyleByName(Type _componentType, string _skinName, string _styleName) => GetStyleByNameOrAlias(_componentType, _skinName, _styleName, false);
		public UiAbstractStyleBase GetStyleByAlias(Type _componentType, string _skinAlias, string _styleAlias) => GetStyleByNameOrAlias(_componentType, _skinAlias, _styleAlias, true);

		public UiAbstractStyleBase GetStyleByNameOrAlias(Type _componentType, string _skin, string _style, bool _isAlias)
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

		public bool SetCurrentSkinByNameOrAlias(string _skinNameOrAlias, bool _emitEvent, bool _isAlias)
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

		public void Validate()
		{
			foreach (var skin in m_skins)
				skin.Validate(this);
		}

		private void OnAddSkin(UiStyleConfig _styleConfig, UiSkin _newSkin)
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

		private void OnSetSkinAlias(UiStyleConfig _styleConfig, UiSkin _skin, string _alias)
		{
			if (_styleConfig != this)
				return;
			
			//FIXME: The _skin instance is different than the skins in style config - why??!
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

		private void OnSetStyleAlias(UiStyleConfig _styleConfig, UiAbstractStyleBase _style, string _alias)
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
		
		private void OnDeleteStyle(UiStyleConfig _styleConfig, UiAbstractStyleBase _style)
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

		private void OnDeleteSkin(UiStyleConfig _styleConfig, string _skinName)
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
		public static void SetDirty(UiStyleConfig instance) => EditorUtility.SetDirty(instance);
#endif
		public bool StyleExists(Type type, string name)
		{
			if (m_skins.Count == 0)
				return false;

			var skin = m_skins[0];
			foreach (var style in skin.Styles)
			{
				if (style.Name == name && style.GetType() == type)
					return true;
			}

			return false;
		}
	}
}
