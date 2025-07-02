using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class CreatePackageVariantInProject
	{
		const string Prefix = "Assets/";
		
		const string CreatePackageVariantName = "Create Variant in Project";
		const string CreatePackageVariant = Prefix + CreatePackageVariantName;
		const int CreatePackageVariantOrder = -800;

		[MenuItem(CreatePackageVariant, false, CreatePackageVariantOrder)]
		public static void CreatePackageVariantExec( MenuCommand command )
		{
			//TODO: Create Prefab variant(s) in Asset folder (Simple variants, no replacement/hierarchy magic)
		}

		[MenuItem(CreatePackageVariant, true, CreatePackageVariantOrder)]
		public static bool CreatePackageVariantValidate()
		{
			//TODO: validate if prefab asset(s) are selected in packages folder
			return true;
		}

	}
}

