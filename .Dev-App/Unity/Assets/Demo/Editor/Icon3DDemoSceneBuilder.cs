using System.IO;
using GuiToolkit;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Builds the dev scene for the visual check of 3D icons (phase 1 of .Dev-App/Planning/3D-Icons.md):
/// three icons of the same object - warm preset, built-in neutral preset, cold preset - in front of a scene that
/// does everything it can to leak in (red directional light on all layers, magenta ambient, green fog).
/// The same object also stands in the scene, lit by that scene, for comparison.
/// </summary>
public static class Icon3DDemoSceneBuilder
{
	private const string Folder = "Assets/Demo/Icon3D";
	private const string ScenePath = "Assets/Scenes/Icon3DDemo.unity";

	[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Create Demo Scene")]
	private static void Build()
	{
		if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		Directory.CreateDirectory(Folder);
		Icon3DSetup.CreateLayer("Icon3D");

		string robotPath = CreateRobotPrefab();
		string warmPath = CreatePresetPrefab("Icon3DPreset_Warm", new Color(1f, 0.65f, 0.35f), new Color(0.25f, 0.15f, 0.08f), new Color(0.4f, 0.25f, 0.6f),
			new Color(1f, 0.85f, 0.6f), new Color(0.55f, 0.35f, 0.2f), new Color(0.15f, 0.08f, 0.04f));
		string coldPath = CreatePresetPrefab("Icon3DPreset_Cold", new Color(0.45f, 0.75f, 1f), new Color(0.06f, 0.1f, 0.2f), new Color(0.9f, 0.9f, 1f),
			new Color(0.75f, 0.9f, 1f), new Color(0.25f, 0.4f, 0.6f), new Color(0.04f, 0.07f, 0.12f));

		var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

		// A new single scene unloads assets nothing references - C# references taken before are dead now
		var robot = AssetDatabase.LoadAssetAtPath<GameObject>(robotPath);
		var warm = AssetDatabase.LoadAssetAtPath<GameObject>(warmPath).GetComponent<UiIcon3DPreset>();
		var cold = AssetDatabase.LoadAssetAtPath<GameObject>(coldPath).GetComponent<UiIcon3DPreset>();

		// Hostile environment: everything here would show up in the icons if isolation failed
		RenderSettings.ambientMode = AmbientMode.Flat;
		RenderSettings.ambientLight = new Color(0.6f, 0f, 0.6f);
		RenderSettings.fog = true;
		RenderSettings.fogMode = FogMode.Exponential;
		RenderSettings.fogColor = new Color(0f, 0.8f, 0f);
		RenderSettings.fogDensity = 0.08f;

		var sunGo = new GameObject("Red Sun (all layers)");
		sunGo.transform.rotation = Quaternion.Euler(40, 160, 0);
		var sun = sunGo.AddComponent<Light>();
		sun.type = LightType.Directional;
		sun.color = Color.red;
		sun.intensity = 2;
		sun.cullingMask = ~0;

		var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
		cameraGo.transform.position = new Vector3(0, 1.2f, 4f);
		cameraGo.transform.LookAt(new Vector3(0, 0.8f, 0));
		var cam = cameraGo.AddComponent<Camera>();
		cam.clearFlags = CameraClearFlags.SolidColor;
		cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);

		var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
		ground.name = "Ground";
		ground.transform.localScale = Vector3.one * 3;

		var sceneRobot = (GameObject)PrefabUtility.InstantiatePrefab(robot);
		sceneRobot.name = "Robot in the scene (lit by the scene)";   // faces +Z, i.e. the camera

		var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGo.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		var material = FindAsset<Material>("UI_Icon3D t:Material");
		CreateIcon(canvasGo.transform, "Icon Warm", robot, warm, material, -400);
		CreateIcon(canvasGo.transform, "Icon Neutral (built-in preset)", robot, null, material, 0);
		CreateIcon(canvasGo.transform, "Icon Cold", robot, cold, material, 400);

		EditorSceneManager.SaveScene(scene, ScenePath);
		UiLog.Log($"3D icon demo scene created: {ScenePath}");
	}

	private static void CreateIcon( Transform _parent, string _name, GameObject _prefab, UiIcon3DPreset _preset, Material _material, float _x )
	{
		var go = new GameObject(_name, typeof(RectTransform), typeof(RawImage));
		go.transform.SetParent(_parent, false);
		var rt = (RectTransform)go.transform;
		rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
		rt.pivot = new Vector2(0.5f, 0f);
		rt.anchoredPosition = new Vector2(_x, 40);
		rt.sizeDelta = new Vector2(320, 320);

		go.GetComponent<RawImage>().material = _material;

		var demo = go.AddComponent<UiIcon3DStaticDemo>();
		var so = new SerializedObject(demo);
		so.FindProperty("m_prefab").objectReferenceValue = _prefab;
		so.FindProperty("m_preset").objectReferenceValue = _preset;
		so.FindProperty("m_size").vector2IntValue = new Vector2Int(320, 320);
		so.ApplyModifiedPropertiesWithoutUndo();
	}

	/// A small robot from primitives. Its face (eyes) points to +Z, Unity's forward.
	private static string CreateRobotPrefab()
	{
		var body = CreateMaterial("Icon3DDemo_Body", new Color(0.85f, 0.85f, 0.85f), 0.5f, 0f);
		var metal = CreateMaterial("Icon3DDemo_Metal", new Color(0.7f, 0.72f, 0.75f), 0.85f, 1f);
		var eye = CreateMaterial("Icon3DDemo_Eye", new Color(0.1f, 0.1f, 0.1f), 0.9f, 0f);

		var root = new GameObject("Icon3DDemo_Robot");
		AddPart(root, PrimitiveType.Cube, "Body", new Vector3(0, 0.7f, 0), new Vector3(0.6f, 0.7f, 0.4f), body);
		AddPart(root, PrimitiveType.Sphere, "Head", new Vector3(0, 1.3f, 0), new Vector3(0.45f, 0.45f, 0.45f), body);
		AddPart(root, PrimitiveType.Sphere, "Eye L", new Vector3(-0.09f, 1.35f, 0.19f), Vector3.one * 0.1f, eye);
		AddPart(root, PrimitiveType.Sphere, "Eye R", new Vector3(0.09f, 1.35f, 0.19f), Vector3.one * 0.1f, eye);
		AddPart(root, PrimitiveType.Capsule, "Arm L", new Vector3(-0.42f, 0.75f, 0), new Vector3(0.15f, 0.3f, 0.15f), metal);
		AddPart(root, PrimitiveType.Capsule, "Arm R", new Vector3(0.42f, 0.75f, 0), new Vector3(0.15f, 0.3f, 0.15f), metal);
		AddPart(root, PrimitiveType.Cylinder, "Leg L", new Vector3(-0.15f, 0.18f, 0), new Vector3(0.15f, 0.18f, 0.15f), metal);
		AddPart(root, PrimitiveType.Cylinder, "Leg R", new Vector3(0.15f, 0.18f, 0), new Vector3(0.15f, 0.18f, 0.15f), metal);
		AddPart(root, PrimitiveType.Cylinder, "Antenna", new Vector3(0, 1.62f, 0), new Vector3(0.03f, 0.1f, 0.03f), metal);

		string path = $"{Folder}/Icon3DDemo_Robot.prefab";
		PrefabUtility.SaveAsPrefabAsset(root, path);
		Object.DestroyImmediate(root);
		return path;
	}

	private static void AddPart( GameObject _root, PrimitiveType _type, string _name, Vector3 _position, Vector3 _scale, Material _material )
	{
		var part = GameObject.CreatePrimitive(_type);
		part.name = _name;
		Object.DestroyImmediate(part.GetComponent<Collider>());
		part.transform.SetParent(_root.transform, false);
		part.transform.localPosition = _position;
		part.transform.localScale = _scale;
		part.GetComponent<Renderer>().sharedMaterial = _material;
	}

	/// Key light from top left, rim light from behind, own ambient and reflection - all camera relative.
	/// The reflection matters for metal: without one it reflects the black baseline and renders black.
	private static string CreatePresetPrefab( string _name, Color _key, Color _ambient, Color _rim, Color _sky, Color _horizon, Color _ground )
	{
		string reflectionPath = $"{Folder}/{_name}_Reflection.asset";
		AssetDatabase.DeleteAsset(reflectionPath);
		var reflection = Icon3DEnvironmentUtility.CreateGradientCubemap(_sky, _horizon, _ground);
		reflection.name = $"{_name}_Reflection";
		AssetDatabase.CreateAsset(reflection, reflectionPath);

		var root = new GameObject(_name);
		root.AddComponent<UiIcon3DPreset>();

		var settings = root.AddComponent<UiCameraRenderSettings>();
		settings.AmbientMode.Value = AmbientMode.Flat;
		settings.AmbientMode.Enabled = true;
		settings.AmbientLight.Value = _ambient;
		settings.AmbientLight.Enabled = true;
		settings.DefaultReflectionMode.Value = DefaultReflectionMode.Custom;
		settings.DefaultReflectionMode.Enabled = true;
		settings.CustomReflection.Value = reflection;
		settings.CustomReflection.Enabled = true;

		AddLight(root, "Key", Quaternion.Euler(35, 35, 0), _key, 1.3f, true);
		AddLight(root, "Rim", Quaternion.Euler(20, 200, 0), _rim, 1.0f, false);

		string path = $"{Folder}/{_name}.prefab";
		PrefabUtility.SaveAsPrefabAsset(root, path);
		Object.DestroyImmediate(root);
		return path;
	}

	private static void AddLight( GameObject _root, string _name, Quaternion _rotation, Color _color, float _intensity, bool _shadows )
	{
		var go = new GameObject(_name);
		go.transform.SetParent(_root.transform, false);
		go.transform.localRotation = _rotation;
		var light = go.AddComponent<Light>();
		light.type = LightType.Directional;
		light.color = _color;
		light.intensity = _intensity;
		light.shadows = _shadows ? LightShadows.Soft : LightShadows.None;
	}

	private static Material CreateMaterial( string _name, Color _color, float _smoothness, float _metallic )
	{
		string path = $"{Folder}/{_name}.mat";
		var material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material == null)
		{
			material = new Material(Shader.Find("Standard"));
			AssetDatabase.CreateAsset(material, path);
		}

		material.color = _color;
		material.SetFloat("_Glossiness", _smoothness);
		material.SetFloat("_Metallic", _metallic);
		EditorUtility.SetDirty(material);
		return material;
	}

	private static T FindAsset<T>( string _filter ) where T : Object
	{
		foreach (var guid in AssetDatabase.FindAssets(_filter))
		{
			var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
			if (asset != null)
				return asset;
		}

		return null;
	}
}
