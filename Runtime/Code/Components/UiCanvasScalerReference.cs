using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// This class is intended to have a central prefab to determine canvas scaling.
	/// This makes global changes much easier, since you only have to change settings at one place.
	/// </summary>
	public class UiCanvasScalerReference : CanvasScaler
	{
		[SerializeField][Optional] private CanvasScaler m_reference;

		private Coroutine m_coroutine = null;
		private bool m_inForceRefresh;

		public CanvasScaler Reference => m_reference;

		protected override void OnEnable()
		{
			if (!m_inForceRefresh)
			{
				if (m_reference)
				{
					m_reference.CopyTo((CanvasScaler)this);
				}

				if (m_coroutine == null)
					m_coroutine = StartCoroutine(ForceRefresh());
			}

			base.OnEnable();
		}

		private IEnumerator ForceRefresh()
		{
			yield return null;
			m_inForceRefresh = true;
			enabled = false;
			// ReSharper disable once Unity.InefficientPropertyAccess
			enabled = true;
			m_inForceRefresh = false;
			m_coroutine = null;
		}
	}
}
