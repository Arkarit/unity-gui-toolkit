using System.IO;
using GuiToolkit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Generates the library assets of the 3D icons that ship with the package:
/// <list type="bullet">
/// <item>StandardIcon3D.prefab - the icon, tagged as standard element (clients create variants of it)</item>
/// <item>Icon3DPreset_Neutral (in Resources: the default when nothing else is configured), _Warm, _Dramatic</item>
/// <item>one reflection cubemap per preset, fixed assets</item>
/// </list>
/// Re-running updates the assets in place; GUIDs stay the same, so references in client projects survive.
/// Dev tool only - lives in the dev project, not in the package.
/// </summary>
public static class Icon3DLibraryAssetBuilder
{
	private const string PackageRoot = "Assets/External/unity-gui-toolkit";
	private const string PresetFolder = PackageRoot + "/Prefabs/Icon3D";
	private const string ResourcesFolder = PackageRoot + "/Resources/Icon3D";
	private const string IconPrefabPath = PackageRoot + "/Prefabs/StandardElements/StandardIcon3D.prefab";
	private const string MaterialPath = PackageRoot + "/Materials/UI_Icon3D.mat";

	public const string WarmPresetPath = PresetFolder + "/Icon3DPreset_Warm.prefab";
	public const string DramaticPresetPath = PresetFolder + "/Icon3DPreset_Dramatic.prefab";
	public const string NeutralPresetPath = ResourcesFolder + "/Icon3DPreset_Neutral.prefab";

	private struct LightDef
	{
		public string Name;
		public Vector3 Euler;   // camera relative
		public Color Color;
		public float Intensity;
		public LightShadows Shadows;
	}

	/// <summary>Framing and shadow catcher of a preset; a preset without one keeps the component defaults.</summary>
	private class FrameDef
	{
		public Vector3 ViewRotation;
		public float Padding;
		public float ShadowStrength;
	}

	[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Dev: Rebuild Library Assets")]
	public static void Build()
	{
		Directory.CreateDirectory(PresetFolder);
		Directory.CreateDirectory(ResourcesFolder);

		// Neutral: soft studio light, the default for everything
		BuildPreset(NeutralPresetPath, null, new Color(0.36f, 0.36f, 0.38f),
			new Color(0.9f, 0.9f, 0.92f), new Color(0.5f, 0.5f, 0.52f), new Color(0.14f, 0.14f, 0.15f),
			new LightDef { Name = "Key", Euler = new Vector3(35, 30, 0), Color = Color.white, Intensity = 1.0f, Shadows = LightShadows.Soft },
			new LightDef { Name = "Rim", Euler = new Vector3(15, 200, 0), Color = Color.white, Intensity = 0.5f, Shadows = LightShadows.None });

		// Warm: golden key, violet rim, warm ambient; a soft shadow under the object. The camera looks down a little
		// more than in Neutral, otherwise the ground (and with it the shadow) is seen edge on
		BuildPreset(WarmPresetPath, new FrameDef { ViewRotation = new Vector3(25, 150, 0), Padding = 0.12f, ShadowStrength = 0.5f },
			new Color(0.28f, 0.18f, 0.1f),
			new Color(1f, 0.9f, 0.75f), new Color(0.6f, 0.42f, 0.28f), new Color(0.15f, 0.09f, 0.05f),
			new LightDef { Name = "Key", Euler = new Vector3(50, 55, 0), Color = new Color(1f, 0.78f, 0.55f), Intensity = 1.2f, Shadows = LightShadows.Soft },
			new LightDef { Name = "Rim", Euler = new Vector3(15, 200, 0), Color = new Color(0.55f, 0.45f, 0.8f), Intensity = 0.8f, Shadows = LightShadows.None });

		// Dramatic: hard top light, cold strong rim, almost no ambient; a dark shadow
		BuildPreset(DramaticPresetPath, new FrameDef { ViewRotation = new Vector3(25, 150, 0), Padding = 0.12f, ShadowStrength = 0.75f },
			new Color(0.04f, 0.04f, 0.06f),
			new Color(0.6f, 0.7f, 0.9f), new Color(0.12f, 0.12f, 0.16f), new Color(0.02f, 0.02f, 0.03f),
			new LightDef { Name = "Key", Euler = new Vector3(60, 50, 0), Color = Color.white, Intensity = 1.6f, Shadows = LightShadows.Hard },
			new LightDef { Name = "Rim", Euler = new Vector3(10, 215, 0), Color = new Color(0.5f, 0.7f, 1f), Intensity = 1.4f, Shadows = LightShadows.None });

		BuildIconPrefab();

		AssetDatabase.SaveAssets();
		UiIcon3DRenderer.Invalidate();
		UiLog.Log("3D icon library assets rebuilt");
	}

	private static void BuildPreset( string _path, FrameDef _frame, Color _ambient, Color _sky, Color _horizon, Color _ground, params LightDef[] _lights )
	{
		string name = Path.GetFileNameWithoutExtension(_path);
		var reflection = CreateOrUpdateCubemap(Path.ChangeExtension(_path, null) + "_Reflection.asset", _sky, _horizon, _ground);

		var root = new GameObject(name);
		var preset = root.AddComponent<UiIcon3DPreset>();
		if (_frame != null)
		{
			preset.ViewRotation = Quaternion.Euler(_frame.ViewRotation);
			preset.Padding = _frame.Padding;
			preset.ShadowCatcher.Enabled = true;
			preset.ShadowCatcher.Strength = _frame.ShadowStrength;
		}

		var settings = root.AddComponent<UiCameraRenderSettings>();
		Enable(settings.AmbientMode, AmbientMode.Flat);
		Enable(settings.AmbientLight, _ambient);
		Enable(settings.AmbientIntensity, 1f);
		Enable(settings.DefaultReflectionMode, DefaultReflectionMode.Custom);
		Enable(settings.CustomReflection, (Texture)reflection);
		Enable(settings.ReflectionIntensity, 1f);

		foreach (var def in _lights)
		{
			var go = new GameObject(def.Name);
			go.transform.SetParent(root.transform, false);
			go.transform.localRotation = Quaternion.Euler(def.Euler);
			var light = go.AddComponent<Light>();
			light.type = LightType.Directional;
			light.color = def.Color;
			light.intensity = def.Intensity;
			light.shadows = def.Shadows;
		}

		PrefabUtility.SaveAsPrefabAsset(root, _path);
		Object.DestroyImmediate(root);
	}

	private static void BuildIconPrefab()
	{
		var root = new GameObject("StandardIcon3D", typeof(RectTransform), typeof(RawImage));
		((RectTransform)root.transform).sizeDelta = new Vector2(256, 256);

		var rawImage = root.GetComponent<RawImage>();
		rawImage.material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
		rawImage.raycastTarget = false;

		root.AddComponent<UiIcon3D>();

		var element = root.AddComponent<UiStandardElement>();
		var so = new SerializedObject(element);
		so.FindProperty("m_element").intValue = (int)EStandardElement.StandardIcon3D;
		so.ApplyModifiedPropertiesWithoutUndo();

		var comment = root.AddComponent<UiComment>();
		var commentSo = new SerializedObject(comment);
		commentSo.FindProperty("m_comment").stringValue =
			"A 3D object shown as icon, rendered with its own lighting preset and isolated from the scene. " +
			"Set the object (prefab) and optionally a preset (UiIcon3DPreset prefab); the texture follows the rect size. " +
			"Identical icons share one render. Uses the premultiplied UI_Icon3D material.";
		commentSo.ApplyModifiedPropertiesWithoutUndo();

		PrefabUtility.SaveAsPrefabAsset(root, IconPrefabPath);
		Object.DestroyImmediate(root);
	}

	/// Keeps the asset (and its GUID) if it exists, only its content changes.
	private static Cubemap CreateOrUpdateCubemap( string _path, Color _sky, Color _horizon, Color _ground )
	{
		var generated = Icon3DEnvironmentUtility.CreateGradientCubemap(_sky, _horizon, _ground);
		generated.name = Path.GetFileNameWithoutExtension(_path);

		var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(_path);
		if (existing == null)
		{
			AssetDatabase.CreateAsset(generated, _path);
			return generated;
		}

		EditorUtility.CopySerialized(generated, existing);
		existing.name = generated.name;
		Object.DestroyImmediate(generated);
		EditorUtility.SetDirty(existing);
		return existing;
	}

	private static void Enable<T>( PerCameraSetting<T> _setting, T _value )
	{
		_setting.Value = _value;
		_setting.Enabled = true;
	}
}
