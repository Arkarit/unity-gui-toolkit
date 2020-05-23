using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class TestUiObject : MonoBehaviour
	{
		public TMP_Text m_text;
		public Image m_image;

		public void Init(int _index, int _indexCount)
		{
			if (m_text != null)
				m_text.text = _index.ToString();
			float hue = (float) _index / (float) _indexCount;
			if (m_text != null)
				m_text.color = Color.HSVToRGB(hue,1,1);
			if (m_image != null)
				m_image.color = Color.HSVToRGB(hue,0.5f,1);
		}
	}
}