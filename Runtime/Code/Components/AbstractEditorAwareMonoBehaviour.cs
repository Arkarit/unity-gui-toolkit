using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class AbstractEditorAwareMonoBehaviour : MonoBehaviour, IEditorAware
	{
		public abstract string[] RequiredScriptableObjects {get;}
		public abstract void SafeAwake();
		public abstract void SafeOnEnable();
		
		public virtual bool Condition() => true;
		

		public void Awake()
		{
			AssetReadyGate.WhenReady
			(
				SafeAwake,
				Condition,
				RequiredScriptableObjects
			);
		}
		
		public void OnEnable()
		{
			AssetReadyGate.WhenReady
			(
				SafeOnEnable,
				Condition,
				RequiredScriptableObjects
			);
		}
		
	}
}