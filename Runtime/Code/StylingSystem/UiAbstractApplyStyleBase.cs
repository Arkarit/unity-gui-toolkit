using System;
using PlasticGui.Gluon.Help.Conditions;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public abstract class UiAbstractApplyStyleBase : MonoBehaviour
	{
		[SerializeField][HideInInspector] private string m_name;
		[SerializeReference] protected UiAbstractStyleBase m_style;

		public abstract Type SupportedMonoBehaviourType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract MonoBehaviour MonoBehaviour { get; }
		public abstract int Key { get; }

		protected virtual void Awake()
		{
			m_style = null;
			SetStyle();
			Apply();
		}

		protected virtual void OnEnable()
		{
			UiEvents.EvScreenOrientationChange.AddListener(OnScreenOrientationChanged);
		}

		protected virtual void OnDisable()
		{
			UiEvents.EvScreenOrientationChange.RemoveListener(OnScreenOrientationChanged);
		}

		private void OnScreenOrientationChanged(EScreenOrientation _oldScreenOrientation, EScreenOrientation _newScreenOrientation)
		{
Debug.Log("OnScreenOrientationChanged");
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

		public void Apply()
		{
			if (CheckCondition())
				ApplyImpl();
		}

		private bool CheckCondition()
		{
			if (Style == null)
				return false;
			return    Style.ScreeenOrientationCondition == UiAbstractStyleBase.EScreenOrientationCondition.Always 
			       || Style.ScreeenOrientationCondition == (UiAbstractStyleBase.EScreenOrientationCondition) UiMain.ScreenOrientation;
		}

		protected abstract void ApplyImpl();

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
			UiSkin currentSkin = UiStyleConfig.Instance.CurrentSkin;
			if (currentSkin == null)
				return null;

			return currentSkin.StyleByKey(Key);
		}

		public void SetStyle()
		{
			m_style = FindStyle();
			if (m_style != null)
				m_name = m_style.Name;
		}
	}
}