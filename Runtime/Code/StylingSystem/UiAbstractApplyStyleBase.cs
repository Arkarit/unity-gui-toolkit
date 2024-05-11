using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public abstract class UiAbstractApplyStyleBase : MonoBehaviour
	{
		[SerializeField][HideInInspector] private string m_name;
		protected UiAbstractStyleBase m_style;

		public abstract Type SupportedMonoBehaviourType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract MonoBehaviour MonoBehaviour { get; }
		public abstract int Key { get; }

		protected virtual void Awake()
		{
			SetStyle();
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

		public abstract void Apply();

		public abstract UiAbstractStyleBase CreateStyle(string _name, UiAbstractStyleBase _template = null);

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
			UiSkin currentSkin = UiMainStyleConfig.Instance.CurrentSkin;
			if (currentSkin == null)
				return null;

			var result = currentSkin.StyleByKey(Key);
			if (result != null)
				return result;

			return currentSkin.FindFirstStyleForMonoBehaviour(SupportedMonoBehaviourType);
		}

		public void SetStyle()
		{
			m_style = FindStyle();
			if (m_style != null)
				m_name = m_style.Name;
		}
	}
}