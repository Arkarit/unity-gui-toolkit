using UnityEngine;

namespace GuiToolkit
{
	public class QualityBasedVisibility : MonoBehaviour
	{
		[SerializeField] private int m_qualitiesVisible;
		
		private void Awake()
		{
			QualitySettings.activeQualityLevelChanged += ActiveQualityLevelChanged;
			AdjustVisibility(QualitySettings.GetQualityLevel());
		}

		private void ActiveQualityLevelChanged(int _, int _newQualityLevel) => AdjustVisibility(_newQualityLevel);
		
		private void AdjustVisibility(int _quality)
		{
			gameObject.SetActive((m_qualitiesVisible & (1 << _quality)) != 0);
		}
	}
}
