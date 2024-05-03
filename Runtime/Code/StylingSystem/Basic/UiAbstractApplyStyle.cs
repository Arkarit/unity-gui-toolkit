using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public abstract class UiAbstractApplyStyle<MB,ST> : UiAbstractApplyStyleBase 
		where MB : MonoBehaviour 
		where ST : UiAbstractStyle<MB>
	{
		private MB m_monoBehaviour;
		private int m_key = 0;

		public override Type SupportedMonoBehaviourType => typeof(MB);
		public override Type SupportedStyleType => typeof(ST);

		public override MonoBehaviour MonoBehaviour
		{
			get
			{
				if (m_monoBehaviour == null)
				{
					m_monoBehaviour = GetComponent<MB>();
					if (m_monoBehaviour == null)
					{
						Debug.LogWarning($"{GetType().Name}: Required MonoBehaviour type '{SupportedMonoBehaviourType.Name}'" + 
						                 $" not found on GameObject '{gameObject.name}', styling won't work here");
					}
				}

				return m_monoBehaviour;
			}
		}

		public override int Key
		{
			get
			{
				if (m_key == 0 || !Application.isPlaying)
					m_key = UiStyleUtility.GetKey(SupportedMonoBehaviourType, Name);

				return m_key;
			}
		}

		public MB SpecificMonoBehaviour => MonoBehaviour as MB;
		public ST SpecificStyle => Style as ST;

		public virtual void OnEnable()
		{
			UiEvents.EvSkinChanged.AddListener(OnSkinChanged);

Debug.Log($"OnEnable {gameObject.name}");
			if (m_monoBehaviour == null)
				return;

			Apply();

		}

		public virtual void OnDisable()
		{
			UiEvents.EvSkinChanged.RemoveListener(OnSkinChanged);

Debug.Log($"OnDisable {gameObject.name}");
			if (m_monoBehaviour == null)
				return;


		}

		private void OnSkinChanged()
		{
Debug.Log($"OnSkinChanged {gameObject.name}");
			SetStyle();
			Apply();
		}
	}
}