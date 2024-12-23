using System;
using System.Collections;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistortGroup : MonoBehaviour
	{
		[FormerlySerializedAs("m_direction")]
		[SerializeField] protected EAxis2DFlags m_axisFlags;
		[SerializeField] private Transform m_container;
		[SerializeField] protected bool m_inverse;

		protected Transform Container
		{
			get
			{
				if (m_container == null)
					m_container = transform;

				return m_container;
			}
		}


		private UiDistortBase[] m_elements = Array.Empty<UiDistortBase>();

		public void Refresh()
		{
			ReInit();
		}

		private void OnTransformChildrenChanged()
		{
			ReInit();
		}

		private void OnEnable()
		{
			ReInit();
			UiEventDefinitions.EvSkinChanged.AddListener(OnSkinChanged);
		}

		private void OnDisable()
		{
			UiEventDefinitions.EvSkinChanged.RemoveListener(OnSkinChanged);
		}

		private void OnSkinChanged(float _)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += ReInit;
				return;
			}
#endif

			StartCoroutine(ReInitCoroutine());
		}

		private IEnumerator ReInitCoroutine()
		{
			yield return 0;
			ReInit();
		}


		private void ReInit()
		{
			CollectElements();
			PrepareElements();
			UiStyleUtility.ReApplyAppliers(m_elements);
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += FinishElements;
				return;
			}
#endif
			StartCoroutine(FinishElementsCoroutine());
		}

		private void CollectElements()
		{
			m_elements = Container.GetComponentsInChildren<UiDistortBase>();
		}

		private void PrepareElements()
		{
			var length = m_elements.Length;
			for (int i = 0; i < length; i++)
			{
				var element = m_elements[i];
				element.enabled = true;
				element.Mirror = EAxis2DFlags.None;
			}
		}

		private IEnumerator FinishElementsCoroutine()
		{
			yield return 0;
			FinishElements();
		}

		private void FinishElements()
		{
			if (!this)
				return;

			CollectElements();
			var length = m_elements.Length;
			for (int i = 0; i < length; i++)
			{
				var element = m_elements[i];
				bool isFirst = i == 0;
				bool isLast = i == length - 1;

				bool isMiddle = !isFirst && !isLast;
				if (isMiddle)
				{
					element.SetDirty();
					element.enabled = false;
					continue;
				}

				if (isFirst)
				{
					element.Mirror = m_inverse ? m_axisFlags : EAxis2DFlags.None;
					continue;
				}

				element.Mirror =  m_inverse ? EAxis2DFlags.None : m_axisFlags;
			}
		}

		private void OnValidate()
		{
			ReInit();
		}
	}
}