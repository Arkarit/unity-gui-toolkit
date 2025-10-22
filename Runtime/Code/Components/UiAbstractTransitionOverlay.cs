using System;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiAbstractTransitionOverlay : MonoBehaviour
	{
		public abstract void FadeInOverlay( Action _onFadedIn = null, bool _instant = false );
		public abstract void FadeOutOverlay( Action _onFadedOut = null, bool _instant = false );
	}
}