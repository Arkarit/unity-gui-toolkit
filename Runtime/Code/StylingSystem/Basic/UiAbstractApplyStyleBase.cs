using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public abstract class UiAbstractApplyStyleBase : MonoBehaviour
	{
		[SerializeField][HideInInspector] private string m_name;
		[SerializeReference] private UiAbstractStyleBase m_style;

		public abstract Type SupportedMonoBehaviourType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract MonoBehaviour MonoBehaviour { get; }
		public abstract int Key { get; }

		protected virtual void Awake()
		{
			SetStyle();
		}

		public UiAbstractStyleBase Style
		{
			get => m_style;
		}

		public abstract void Apply();

		public abstract UiAbstractStyleBase CreateStyle(string _name);

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

			return currentSkin.StyleByKey(Key);
		}

		public void SetStyle()
		{
			m_style = FindStyle();
		}
	}
}