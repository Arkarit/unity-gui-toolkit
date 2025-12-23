using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTextContainer : UiThing, IPoolable
	{
		protected UiAutoLocalize m_translator;
		protected TextMeshProUGUI m_tmpText;
		protected Text m_text;
		protected bool m_initialized = false;

		public virtual string Text
		{
			get
			{
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					return m_translator.Text;
				if (m_tmpText)
					return m_tmpText.text;
				if (m_text)
					return m_text.text;
				return "";
			}

			set
			{
				if (value == null)
					return;
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					m_translator.LocaKey = value;
				else if (m_tmpText)
					m_tmpText.text = value;
				else if (m_text)
					m_text.text = value;
				else
					UiLog.LogError($"No text found for '{gameObject.name}', can not set string '{value}'");
			}
		}

		public virtual Color TextColor
		{
			get
			{
				InitIfNecessary();
				if (m_tmpText)
					return m_tmpText.color;
				if (m_text)
					return m_text.color;
				return Color.white;
			}

			set
			{
				if (value == null)
					return;
				InitIfNecessary();
				if (m_tmpText)
					m_tmpText.color = value;
				else if (m_text)
					m_text.color = value;
				else
					UiLog.LogError($"No text found for '{gameObject.name}', can not set color '{value}'");
			}
		}

		public UnityEngine.Object TextComponent
		{
			get
			{
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					return m_translator;
				if (m_tmpText)
					return m_tmpText;
				if (m_text)
					return m_text;
				return null;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			InitIfNecessary();
		}

		protected virtual void Init() { }

		protected void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_translator = GetComponentInChildren<UiAutoLocalize>();
			m_tmpText = GetComponentInChildren<TextMeshProUGUI>();
			m_text = GetComponentInChildren<Text>();

			Init();

			m_initialized = true;
		}

		public void OnPoolCreated() {}

		public void OnPoolReleased() => EnabledInHierarchy = true;
	}
}