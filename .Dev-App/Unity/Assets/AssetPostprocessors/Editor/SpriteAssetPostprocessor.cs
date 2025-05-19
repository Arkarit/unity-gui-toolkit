using UnityEditor;
using UnityEngine;

/// <summary>
/// This class handles the sprite import settings of the whole library.
/// It is however not part of the library itself, but of the demo projects, since we don't want to introduce
/// any asset processors into an user project 
/// </summary>
public class SpriteAssetPostprocessor : AssetPostprocessor
{
	private void OnPreprocessTexture()
	{
		if (
			   !assetPath.StartsWith("Assets/External/unity-gui-toolkit/Resources/BasicSprites")
		    && !assetPath.StartsWith("Assets/External/unity-gui-toolkit/Resources/Flags")
		    && !assetPath.StartsWith("Assets/External/unity-gui-toolkit/Resources/Icons")
		)
			return;
		
		Debug.Log($"Setting texture properties for sprite '{assetPath}'");
		TextureImporter importer = (TextureImporter) assetImporter;
		
		importer.mipmapEnabled = false;
		importer.alphaSource = TextureImporterAlphaSource.FromInput;
		importer.alphaIsTransparency = true;
		importer.sRGBTexture = false;
		importer.textureCompression = TextureImporterCompression.Uncompressed; 
	}
}
