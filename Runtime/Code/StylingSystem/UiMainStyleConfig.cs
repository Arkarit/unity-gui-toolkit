using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiMainStyleConfig), menuName = StringConstants.CREATE_MAIN_STYLE_CONFIG)]
	public class UiMainStyleConfig : UiStyleConfig
	{
		protected static UiMainStyleConfig s_instance;
		public static string ClassName => typeof(UiMainStyleConfig).Name;

		public static void ResetInstance() => s_instance = null;

		public static UiMainStyleConfig Instance
		{
			get
			{
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				if (s_instance == null)
				{
					s_instance = (UiMainStyleConfig)AssetReadyGate.LoadOrCreateScriptableObject(typeof(UiMainStyleConfig), out bool wasCreated);
#if UNITY_EDITOR
					if (wasCreated)
						s_instance.OnEditorCreatedAsset();
#endif
				}
				
				return s_instance;
			}
		}

#if UNITY_EDITOR

		public virtual void OnEditorCreatedAsset() { }

#endif

	}
}
