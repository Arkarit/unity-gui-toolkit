using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiOrientationDependentStyleConfig), menuName = StringConstants.CREATE_ORIENTATION_DEPENDENT_STYLE_CONFIG)]
	[EditorAware]
	public class UiOrientationDependentStyleConfig : UiStyleConfig
	{
		public const string Landscape = "Landscape";
		public const string Portrait = "Portrait";

		protected static UiOrientationDependentStyleConfig s_instance;

		public const string EditorDir = "Assets/Resources/";
		public static string ClassName => typeof(UiOrientationDependentStyleConfig).Name;
		public static string AssetPath => EditorDir + ClassName + ".asset";

		public static void ResetInstance() => s_instance = null;

		protected override void OnEnable()
		{
			AssetReadyGate.WhenReady(() =>
			{
				var instance = Instance;
				if (instance.NumSkins == 0)
				{
					var skins = s_instance.Skins;
					skins.Add(new UiSkin(s_instance, Landscape));
					skins.Add(new UiSkin(s_instance, Portrait));
					s_instance.Skins = skins;
				}

				base.OnEnable();
				UiEventDefinitions.EvScreenOrientationChange.AddListener(OnScreenOrientationChange);
			});
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UiEventDefinitions.EvScreenOrientationChange.RemoveListener(OnScreenOrientationChange);
		}

		private void OnScreenOrientationChange( EScreenOrientation _before, EScreenOrientation _after )
		{
			Instance.CurrentSkinName = _after == EScreenOrientation.Landscape ? Landscape : Portrait;
		}

		public static UiOrientationDependentStyleConfig Instance
		{
			get
			{
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				if (s_instance == null)
				{
					s_instance = (UiOrientationDependentStyleConfig)AssetReadyGate.LoadOrCreateScriptableObject
					(
						typeof(UiOrientationDependentStyleConfig),
						out bool wasCreated
					);
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
