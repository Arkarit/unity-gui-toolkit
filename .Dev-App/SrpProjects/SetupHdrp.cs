using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

// Copied into the HDRP test project's Assets/Editor by srp-project.mjs; called with -executeMethod SetupHdrp.Run.
public static class SetupHdrp
{
	public static void Run()
	{
		Directory.CreateDirectory("Assets/Settings");

		// A plain HDRP asset; the alpha channel of the colour buffer needs the 16 bit format (the default 11/11/10 has none)
		var asset = ScriptableObject.CreateInstance<HDRenderPipelineAsset>();
		AssetDatabase.CreateAsset(asset, "Assets/Settings/HdrpAsset.asset");

		var settings = asset.currentPlatformRenderPipelineSettings;
		settings.colorBufferFormat = RenderPipelineSettings.ColorBufferFormat.R16G16B16A16;
		asset.currentPlatformRenderPipelineSettings = settings;
		EditorUtility.SetDirty(asset);

		GraphicsSettings.defaultRenderPipeline = asset;
		for (int i = 0; i < QualitySettings.names.Length; i++)
		{
			QualitySettings.SetQualityLevel(i);
			QualitySettings.renderPipeline = asset;
		}

		AssetDatabase.SaveAssets();
		Debug.Log("HDRP set up: " + AssetDatabase.GetAssetPath(asset));
	}
}
