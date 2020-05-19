using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[Serializable]
	public class UiLayoutElementTransformPolicy
	{
		public const int Unlimited = -1;

		public enum SizePolicy
		{
			Fixed,
			Flexible,
			Master,
		}

		public enum AlignmentPolicy
		{
			Minimum,
			Center,
			Maximum,
		}

		[SerializeField]
		private float m_minimumSize;
		[SerializeField]
		[FormerlySerializedAs("m_sizeA")]
		private float m_preferredSize;
		[SerializeField]
		private float m_maximumSize;
		[SerializeField]
		private SizePolicy m_sizePolicy;
		[SerializeField]
		private AlignmentPolicy m_alignmentPolicy;

		public float GetPreferredSize()
		{
			return m_preferredSize;
		}
	}
}