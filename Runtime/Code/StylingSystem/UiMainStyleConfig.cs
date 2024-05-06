using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public class UiMainStyleConfig : AbstractSingletonScriptableObject<UiMainStyleConfig>
	{
		[SerializeField] private List<UiSkin> m_skins;

		[SerializeField] private int m_currentSkinIdx;

		public List<UiSkin> Skins => m_skins;

		protected override void OnEnable()
		{
			base.OnEnable();
			foreach (var skin in m_skins)
				skin.Init();
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
			if (m_skins == null || m_skins.Count <= 0)
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
				if (m_skins == null)
					return result;

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
				if (m_skins != null)
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
	}
}