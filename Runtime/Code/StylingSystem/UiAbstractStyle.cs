using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public abstract class UiAbstractStyle<MB> : UiAbstractStyleBase where MB : MonoBehaviour
	{
	}
}
