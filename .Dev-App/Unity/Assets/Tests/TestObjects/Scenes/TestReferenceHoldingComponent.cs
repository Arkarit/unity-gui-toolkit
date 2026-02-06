using UnityEngine;
using UnityEngine.UI;

public class TestReferenceHoldingComponent : MonoBehaviour
{
	[SerializeField] private Text m_Text;
	[SerializeField] private Text m_OtherText;
	[SerializeField] private Text m_NonDestructableText;
	[SerializeField] private CanvasScaler m_CanvasScaler;

	private void OnEnable()
	{
		m_Text.text = "Text1";
		m_OtherText.text = "Text2";
		m_NonDestructableText.text = "NonDestructable";
		m_OtherText.color = Color.white;
	}
}
