using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Copied into the URP test project's Assets/Editor by srp-project.mjs; called with -executeMethod SetupUrp.Run.
public static class SetupUrp
{
	public static void Run()
	{
		Directory.CreateDirectory("Assets/Settings");

		var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
		AssetDatabase.CreateAsset(rendererData, "Assets/Settings/UrpRenderer.asset");

		var asset = UniversalRenderPipelineAsset.Create(rendererData);
		AssetDatabase.CreateAsset(asset, "Assets/Settings/UrpAsset.asset");

		GraphicsSettings.defaultRenderPipeline = asset;
		for (int i = 0; i < QualitySettings.names.Length; i++)
		{
			QualitySettings.SetQualityLevel(i);
			QualitySettings.renderPipeline = asset;
		}

		AssetDatabase.SaveAssets();
		Debug.Log("URP set up: " + AssetDatabase.GetAssetPath(asset));
	}
}
