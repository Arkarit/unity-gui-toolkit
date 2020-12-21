using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiSettingsEntryLanguage : UiSettingsEntryToggle
	{
		[SerializeField]
		private Image m_image;

		[SerializeField]
		private UiLocaManager.ELanguage m_language;

		protected override void OnEnable()
		{
			base.OnEnable();

			bool isActive = UiMain.LocaManager.Language == m_language;

			StoredValue = isActive;

			if (isActive)
				StartCoroutine(SetToggleDelayed(true));
		}

		protected override void OnValueChanged( bool _active )
		{
			if (_active)
			{
				UiMain.LocaManager.ChangeLanguage(m_language);
				StoredValue = true;
				return;
			}

			StoredValue = false;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_image.sprite == null)
			{
				Debug.LogError("Image path not found!");
				return;
			}

			string currentAssetPath = AssetDatabase.GetAssetPath(m_image.sprite);
			string assetPath = UiEditorUtility.GetAssetProjectDir(currentAssetPath) + UiLocaManager.StringByLanguage(m_language) + ".png";
			Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

			if (m_image.sprite == null)
			{
				Debug.LogError("Sprite not found!");
				return;
			}

			m_image.sprite = sprite;
		}
#endif
	}
}