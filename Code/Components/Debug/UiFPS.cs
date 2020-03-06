using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiFPS : MonoBehaviour
	{
		public int NumSamples = 20;
		public Text m_text;
		private float[] m_samples;
		private int m_idx;

		private void Start()
		{
			m_samples = new float[NumSamples];
		}

		private void Update()
		{
			float dt = Time.deltaTime;
			m_samples[m_idx++] = dt;
			if (m_idx >= NumSamples)
				m_idx = 0;

			float sum = 0;
			for (int i=0; i<NumSamples; i++)
				sum += m_samples[i];
			sum /= NumSamples;
			sum = 1.0f / sum;
			m_text.text = ((int)sum).ToString();
		}
	}
}