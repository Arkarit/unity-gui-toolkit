using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public class UiStyleConfig : ScriptableObject
	{
		[NonReorderable][SerializeField] private List<UiSkin> m_skins = new();

		[SerializeField] private int m_currentSkinIdx = 0;

		public List<UiSkin> Skins => m_skins;

		protected void OnEnable()
		{
			foreach (var skin in m_skins)
				skin.Init();
			UiEvents.EvDeleteStyle.AddListener(OnDeleteStyle);
			UiEvents.EvDeleteSkin.AddListener(OnDeleteSkin);
		}

		protected void OnDisable()
		{
			UiEvents.EvDeleteStyle.RemoveListener(OnDeleteStyle);
			UiEvents.EvDeleteSkin.RemoveListener(OnDeleteSkin);
		}

		public void ForeachSkin(Action<UiSkin> action)
		{
			foreach (var skin in m_skins)
				action(skin);
		}

		public List<string> StyleNames => GetStyleNamesByMonoBehaviourType(null);

		public List<string> GetStyleNamesByMonoBehaviourType(Type monoBehaviourType)
		{
			List<string> result = new();
			if (m_skins.Count <= 0)
				return result;

			var skin = m_skins[0];
			foreach (var style in skin.Styles)
			{
				if (monoBehaviourType != null && style.SupportedMonoBehaviourType != monoBehaviourType)
					continue;

				result.Add(style.Name);
			}
			return result;
		}

		public List<string> SkinNames
		{
			get
			{
				List<string> result = new();
				foreach (var skin in m_skins)
				{
					result.Add(skin.Name);
				}

				return result;
			}
		}

		public string CurrentSkinName
		{
			get
			{
				var currentSkin = CurrentSkin;
				if (currentSkin == null)
					return null;

				return currentSkin.Name;
			}

			set => SetCurrentSkin(value, true);
		}

		public bool SetCurrentSkin(string _skinName, bool _emitEvent)
		{
			for (int i = 0; i < m_skins.Count; i++)
			{
				var skin = m_skins[i];
				if (skin.Name == _skinName)
				{
					if (m_currentSkinIdx == i)
						return true;

					m_currentSkinIdx = i;
					if (_emitEvent)
						UiEvents.EvSkinChanged.InvokeAlways();

					return true;
				}
			}

			return false;
		}

		public bool SetCurrentSkin(string _skinName, bool _emitEvent, float _tweenDuration)
		{
			var styleConfig = UiStyleConfig.Instance;
			var previousSkin = styleConfig.CurrentSkin;

			if (!styleConfig.SetCurrentSkin(_skinName, false))
				return false;

			if (_tweenDuration > 0)
			{
				var currentSkin = styleConfig.CurrentSkin;
				if (currentSkin == null)
					return false;


			}
			return true;
		}


		public UiSkin CurrentSkin 
		{
			get
			{
				if (m_currentSkinIdx < 0 || m_currentSkinIdx >= m_skins.Count)
				{
					return null;
				}

				return m_skins[m_currentSkinIdx];
			}
		}

		public static UiStyleConfig Instance => UiToolkitConfiguration.Instance.StyleConfig;

		public bool HasSkin(string skinName)
		{
			foreach (var skin in m_skins)
			{
				if (skin.Name == skinName)
					return true;
			}

			return false;
		}

		private void OnDeleteStyle(UiAbstractStyleBase _style)
		{
			ForeachSkin(skin =>
			{
				skin.DeleteStyle(_style);
			});

#if UNITY_EDITOR
			EditorSave(this);
#endif

			UiEvents.EvSkinChanged.InvokeAlways();
		}

		private void OnDeleteSkin(string _skinName)
		{
			for (int i = 0; i < m_skins.Count; i++)
			{
				var skin = m_skins[i];
				if (skin.Name == _skinName)
				{
					m_skins.RemoveAt(i);
					break;
				}
			}

#if UNITY_EDITOR
			EditorSave(this);
#endif

			UiEvents.EvSkinChanged.InvokeAlways();
		}
#if UNITY_EDITOR

		public static void EditorSave(UiStyleConfig instance)
		{
			EditorUtility.SetDirty(instance);
			AssetDatabase.SaveAssetIfDirty(instance);
		}

#endif

	}
}