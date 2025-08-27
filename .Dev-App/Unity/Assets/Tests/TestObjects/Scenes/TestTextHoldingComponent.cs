using UnityEngine;
using UnityEngine.UI;

public class TestTextHoldingComponent : MonoBehaviour
{
	[SerializeField] private Text m_Text;
	[SerializeField] private Text m_OtherText;
	[SerializeField] private Text m_NonDestructableText;

	private void OnEnable()
	{
		m_Text.text = "Text1";
		m_OtherText.text = "Text2";
		m_NonDestructableText.text = "NonDestructable";
		m_OtherText.color = Color.white;
	}
}
