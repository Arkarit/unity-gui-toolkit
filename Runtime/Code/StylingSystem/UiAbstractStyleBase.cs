using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	public abstract class UiAbstractStyleBase : MonoBehaviour
	{
		[SerializeField] private List<ApplicableValueBase> m_Values = new();

		public int NumSkins => m_Values.Count;
		public bool Empty => m_Values.Count == 0;

	}
}