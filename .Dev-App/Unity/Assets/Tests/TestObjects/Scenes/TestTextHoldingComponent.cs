using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestTextHoldingComponent : MonoBehaviour
{
	[SerializeField] private TMP_Text m_Text;
	[SerializeField] private TMP_Text m_OtherText;

	private void OnEnable()
	{
		m_Text.text = "Text1";
		m_OtherText.text = "Text2";
		m_OtherText.color = Color.white;
	}
}
