using UnityEngine;
using UnityEngine.UI;

using TMPro;
public class TestTextHoldingComponent : MonoBehaviour
{
	[SerializeField] private TMP_Text m_Text;
	[SerializeField] private TMP_Text m_OtherText;
	[SerializeField] private Text m_NonDestructableText;

	private void OnEnable()
	{
		m_Text.text = "Text1";
		m_OtherText.text = "Text2";
		m_NonDestructableText.text = "NonDestructable";
		m_OtherText.color = Color.white;
	}
}
