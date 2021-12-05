using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiStyling : MonoBehaviour, ISerializationCallbackReceiver
	{
		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			SaveStyle(true);
			RestoreStyle(false);
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			SaveStyle(false);
			RestoreStyle(true);
		}

		private void SaveStyle(bool _original)
		{
		}

		private void RestoreStyle(bool _original)
		{
		}
	}
}