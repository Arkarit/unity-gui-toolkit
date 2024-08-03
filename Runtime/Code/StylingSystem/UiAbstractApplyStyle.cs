using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

		protected override void OnEnable()
		{
			base.OnEnable();
			UiEvents.EvSkinChanged.AddListener(OnSkinChanged);

			if (SpecificMonoBehaviour == null)
				return;

			Apply();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UiEvents.EvSkinChanged.RemoveListener(OnSkinChanged);
		}

		private void OnSkinChanged()
		{
#if UNITY_EDITOR
			string oldName = Name;
#endif

			SetStyle();
			Apply();

#if UNITY_EDITOR
			if (oldName != Name)
			{
				EditorUtility.SetDirty(this);
			}
#endif
		}
	}
}