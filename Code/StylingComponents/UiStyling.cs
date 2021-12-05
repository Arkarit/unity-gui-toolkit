using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[Flags]
	public enum UiStylingFlags
	{
		Sprite = 01,
	}

	public class UiStyling : MonoBehaviour, ISerializationCallbackReceiver
	{
		[SerializeField] protected string m_identifier;
		[Space(10)]
		[SerializeField] protected UiStylingFlags m_flags;

		private UiToolkitConfiguration m_toolkitConfiguration;

		private UiToolkitConfiguration Config
		{
			get
			{
				if (m_toolkitConfiguration == null)
					m_toolkitConfiguration = UiToolkitConfiguration.Instance;
				return m_toolkitConfiguration;
			}
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
//			SaveStyle(_original:true, _persistent:false);
			RestoreStyle(_original:false, _persistent:true);
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
//			SaveStyle(_original:false, _persistent:true);
			RestoreStyle(_original:true, _persistent:false);
		}

		private void SaveStyle(bool _original, bool _persistent)
		{
			UiEditorUtility.EnsureFolderExists(GetDir());
			if ((m_flags & UiStylingFlags.Sprite) != 0)
			{
				string path = GetPath(UiStylingFlags.Sprite);
				Image image = GetComponent<Image>();
				if (image)
				{
					var data = AssetDatabase.LoadAssetAtPath<UiStylingDataSprite>(path);
					if (data == null)
					{
						data = ScriptableObject.CreateInstance<UiStylingDataSprite>();
						UiEditorUtility.CreateAsset(data, path);
					}
					data.Sprite = image.sprite;
					EditorUtility.SetDirty(data);
					AssetDatabase.SaveAssets();
				}
			}
		}

		private void RestoreStyle(bool _original, bool _persistent)
		{
		}

		private string GetDir()
		{
			return Config.StylingPath + m_identifier + "/";
		}

		private string GetPath(UiStylingFlags _flag)
		{
			string s;
			switch (_flag)
			{
				case UiStylingFlags.Sprite:
					s = "sprite.asset";
					break;
				default:
					throw new ArgumentException("Only single flags allowed");
			}

			return GetDir() + s;
		}
	}
}