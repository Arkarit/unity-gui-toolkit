using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiLanguageToggle : UiToggle
	{
		[SerializeField]
		private Image m_image;

		[SerializeField]
		private LocaManager.Language m_language;

		protected override void OnEnable()
		{
			base.OnEnable();
			OnValueChanged.AddListener(OnValueChangedListener);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			OnValueChanged.RemoveListener(OnValueChangedListener);
		}

		private void OnValueChangedListener( bool _active )
		{
			if (_active)
			{
				UiMain.LocaManager.ChangeLanguage(m_language);
			}
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
			string assetPath = UiEditorUtility.GetAssetProjectDir(currentAssetPath) + LocaManager.StringByLanguage(m_language) + ".png";
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