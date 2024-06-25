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
			set
			{
				for (int i = 0; i < m_skins.Count; i++)
				{
					var skin = m_skins[i];
					if (skin.Name == value)
					{
						if (m_currentSkinIdx == i)
							return;

						m_currentSkinIdx = i;
						UiEvents.EvSkinChanged.InvokeAlways();
						return;
					}

					//TODO new skin
				}
			}
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