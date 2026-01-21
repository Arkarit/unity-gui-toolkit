using UnityEngine;
using System;


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

		internal static void Initialize()
		{
			var effectiveStyleConfig = UiToolkitConfiguration.Instance.UiMainStyleConfig;
			if (effectiveStyleConfig != null)
			{
				s_instance = effectiveStyleConfig;
#if UNITY_EDITOR
				UiLog.Log($"Using user UiMainStyleConfig at {AssetDatabase.GetAssetPath(s_instance)}");
#endif
				return;
			}

			s_instance = (UiMainStyleConfig)AssetReadyGate.LoadOrCreateScriptableObject(typeof(UiMainStyleConfig), out bool wasCreated);
			string s = $"Loaded {ClassName}";
#if UNITY_EDITOR
			s += $", asset path:{AssetDatabase.GetAssetPath(s_instance)}, wasCreated:{wasCreated}";
#endif
			UiLog.Log(s);
#if UNITY_EDITOR
			if (wasCreated)
				s_instance.OnEditorCreatedAsset();
#endif
		}

		public static UiMainStyleConfig Instance
		{
			get
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				Bootstrap.ThrowIfNotInitialized();
				if (s_instance == null)
					throw new InvalidOperationException($"UiMainStyleConfig should be initialized by Bootstrap.Initialize() or another place, but isn't.");
#endif				
				return s_instance;
			}
		}

#if UNITY_EDITOR

		public virtual void OnEditorCreatedAsset() { }

#endif

	}
}
