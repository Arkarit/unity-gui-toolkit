using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class TestUiObject : MonoBehaviour
	{
		public TMP_Text m_text;

		public void Init(int _index, int _indexCount)
		{
			m_text.text = _index.ToString();
			float hue = (float) _index / (float) _indexCount;
			m_text.color = Color.HSVToRGB(hue,1,1);
		}
	}
}