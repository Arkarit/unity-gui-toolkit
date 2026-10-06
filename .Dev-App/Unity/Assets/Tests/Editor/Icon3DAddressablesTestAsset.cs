using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Makes sure the Addressable 3D objects used by TestIcon3DAddressables exist in the dev project: a few spheres
	/// of different colour, addressed as GT_TestAddrIcon3D_0 ... _N. Runs on editor load, does nothing when they exist.
	/// </summary>
	[InitializeOnLoad]
	public static class Icon3DAddressablesTestAsset
	{
		public const string Folder = "Assets/Tests/TestObjects/Prefabs/Icon3DAddressables";
		public const string KeyFormat = "GT_TestAddrIcon3D_{0}";
		public const int Count = 3;

		static Icon3DAddressablesTestAsset()
		{
			EditorApplication.update += OnFirstUpdate;
		}

		private static void OnFirstUpdate()
		{
			EditorApplication.update -= OnFirstUpdate;
			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
				return;

			try
			{
				Ensure();
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"Icon3DAddressablesTestAsset: could not create test objects: {e.Message}");
			}
		}

		public static void Ensure()
		{
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			if (settings == null)
				return;

			Directory.CreateDirectory(Folder);
			bool changed = false;

			for (int i = 0; i < Count; i++)
			{
				string key = string.Format(KeyFormat, i);
				string path = $"{Folder}/{key}.prefab";

				if (!File.Exists(path))
				{
					var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					sphere.name = key;
					var material = new Material(Shader.Find("Standard"))
					{
						name = key + "_Mat",
						color = Color.HSVToRGB(i / (float) Count, 0.8f, 0.9f)
					};
					AssetDatabase.CreateAsset(material, $"{Folder}/{key}_Mat.mat");
					sphere.GetComponent<Renderer>().sharedMaterial = material;
					PrefabUtility.SaveAsPrefabAsset(sphere, path);
					Object.DestroyImmediate(sphere);
					changed = true;
				}

				string guid = AssetDatabase.AssetPathToGUID(path);
				var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
				if (entry.address != key)
				{
					entry.address = key;
					changed = true;
				}
			}

			if (changed)
				AssetDatabase.SaveAssets();
		}
	}
}
