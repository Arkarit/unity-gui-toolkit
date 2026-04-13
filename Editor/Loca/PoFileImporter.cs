using UnityEditor.AssetImporters;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Imports .po files as TextAssets so Unity does not log "File couldn't be read" errors.
	/// The .po files are source files for translators; the actual runtime assets are .po.txt files.
	/// </summary>
	[ScriptedImporter(1, "po")]
	public class PoFileImporter : ScriptedImporter
	{
		public override void OnImportAsset( AssetImportContext ctx )
		{
			var text = System.IO.File.ReadAllText(ctx.assetPath, System.Text.Encoding.UTF8);
			var asset = new TextAsset(text);
			ctx.AddObjectToAsset("main", asset);
			ctx.SetMainObject(asset);
		}
	}
}
