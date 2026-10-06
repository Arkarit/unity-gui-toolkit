using System.IO;
using GuiToolkit;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Builds the dev scene for the visual check of 3D icons (.Dev-App/Planning/3D-Icons.md): the same object as
/// library icons with the Warm, Neutral (default) and Dramatic presets, in front of a scene that does everything
/// it can to leak in (red directional light on all layers, magenta ambient, green fog). The object also stands in
/// the scene, lit by that scene, for comparison. Above them an animated robot (Animator + particles): once always
/// animated, once animated only while hovered (it freezes on the current frame when the pointer leaves).
/// </summary>
public static class Icon3DDemoSceneBuilder
{
	private const string Folder = "Assets/Demo/Icon3D";
	private const string ScenePath = "Assets/Scenes/Icon3DDemo.unity";
	private const string IconPrefabPath = "Assets/External/unity-gui-toolkit/Prefabs/StandardElements/StandardIcon3D.prefab";

	[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Create Demo Scene")]
	private static void Build()
	{
		if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		Directory.CreateDirectory(Folder);
		Icon3DSetup.CreateLayer("Icon3D");

		if (AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath) == null)
			Icon3DLibraryAssetBuilder.Build();

		string robotPath = CreateRobotPrefab();
		string animatedRobotPath = CreateAnimatedRobotPrefab();

		var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

		// A new single scene unloads assets nothing references - load everything after it
		var robot = AssetDatabase.LoadAssetAtPath<GameObject>(robotPath);
		var animatedRobot = AssetDatabase.LoadAssetAtPath<GameObject>(animatedRobotPath);
		var iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath);
		var warm = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DLibraryAssetBuilder.WarmPresetPath).GetComponent<UiIcon3DPreset>();
		var dramatic = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DLibraryAssetBuilder.DramaticPresetPath).GetComponent<UiIcon3DPreset>();

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

		CreateIcon(canvasGo.transform, iconPrefab, "Icon Warm", robot, warm, -400);
		CreateIcon(canvasGo.transform, iconPrefab, "Icon Neutral (default preset)", robot, null, 0);
		CreateIcon(canvasGo.transform, iconPrefab, "Icon Dramatic", robot, dramatic, 400);

		var always = CreateIcon(canvasGo.transform, iconPrefab, "Icon Animated", animatedRobot, warm, -250, true);
		var alwaysSo = new SerializedObject(always);
		alwaysSo.FindProperty("m_mode").enumValueIndex = (int)UiIcon3D.EMode.Animated;
		alwaysSo.ApplyModifiedPropertiesWithoutUndo();

		var hover = CreateIcon(canvasGo.transform, iconPrefab, "Icon Hover To Animate", animatedRobot, null, 250, true);
		hover.RawImage.raycastTarget = true;
		hover.gameObject.AddComponent<Icon3DHoverAnimate>();

		new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

		EditorSceneManager.SaveScene(scene, ScenePath);
		UiLog.Log($"3D icon demo scene created: {ScenePath}");
	}

	private static UiIcon3D CreateIcon( Transform _parent, GameObject _iconPrefab, string _name, GameObject _object, UiIcon3DPreset _preset, float _x, bool _top = false )
	{
		var go = (GameObject)PrefabUtility.InstantiatePrefab(_iconPrefab, _parent);
		go.name = _name;
		var rt = (RectTransform)go.transform;
		rt.anchorMin = rt.anchorMax = new Vector2(0.5f, _top ? 1f : 0f);
		rt.pivot = new Vector2(0.5f, _top ? 1f : 0f);
		rt.anchoredPosition = new Vector2(_x, _top ? -40 : 40);
		rt.sizeDelta = new Vector2(320, 320);

		var so = new SerializedObject(go.GetComponent<UiIcon3D>());
		so.FindProperty("m_prefab").objectReferenceValue = _object;
		so.FindProperty("m_preset").objectReferenceValue = _preset;
		so.ApplyModifiedPropertiesWithoutUndo();
		return go.GetComponent<UiIcon3D>();
	}

	/// <summary>
	/// The robot with joints and a 2 s idle loop: head turns, right arm waves, left arm sways, body bobs;
	/// sparks at the antenna tip (additive particles). Face to +Z.
	/// </summary>
	private static string CreateAnimatedRobotPrefab()
	{
		var body = CreateMaterial("Icon3DDemo_Body", new Color(0.85f, 0.85f, 0.85f), 0.5f, 0f);
		var metal = CreateMaterial("Icon3DDemo_Metal", new Color(0.7f, 0.72f, 0.75f), 0.85f, 1f);
		var eye = CreateMaterial("Icon3DDemo_Eye", new Color(0.1f, 0.1f, 0.1f), 0.9f, 0f);

		var root = new GameObject("Icon3DDemo_RobotAnimated");
		var torso = AddPart(root, PrimitiveType.Cube, "Body", new Vector3(0, 0.7f, 0), new Vector3(0.6f, 0.7f, 0.4f), body);
		AddPart(root, PrimitiveType.Cylinder, "Leg L", new Vector3(-0.15f, 0.18f, 0), new Vector3(0.15f, 0.18f, 0.15f), metal);
		AddPart(root, PrimitiveType.Cylinder, "Leg R", new Vector3(0.15f, 0.18f, 0), new Vector3(0.15f, 0.18f, 0.15f), metal);

		// Joints are empty pivots, the visible parts hang below them
		var neck = AddPivot(root, "Neck", new Vector3(0, 1.08f, 0));
		AddPart(neck, PrimitiveType.Sphere, "Head", new Vector3(0, 0.22f, 0), Vector3.one * 0.45f, body);
		AddPart(neck, PrimitiveType.Sphere, "Eye L", new Vector3(-0.09f, 0.27f, 0.19f), Vector3.one * 0.1f, eye);
		AddPart(neck, PrimitiveType.Sphere, "Eye R", new Vector3(0.09f, 0.27f, 0.19f), Vector3.one * 0.1f, eye);
		AddPart(neck, PrimitiveType.Cylinder, "Antenna", new Vector3(0, 0.54f, 0), new Vector3(0.03f, 0.1f, 0.03f), metal);
		var tip = AddPivot(neck, "Antenna Tip", new Vector3(0, 0.66f, 0));
		AddSparks(tip);

		var shoulderL = AddPivot(root, "Shoulder L", new Vector3(-0.38f, 1.0f, 0));
		AddPart(shoulderL, PrimitiveType.Capsule, "Arm L", new Vector3(-0.04f, -0.25f, 0), new Vector3(0.15f, 0.3f, 0.15f), metal);
		var shoulderR = AddPivot(root, "Shoulder R", new Vector3(0.38f, 1.0f, 0));
		AddPart(shoulderR, PrimitiveType.Capsule, "Arm R", new Vector3(0.04f, -0.25f, 0), new Vector3(0.15f, 0.3f, 0.15f), metal);

		var clip = new AnimationClip { name = "Icon3DDemo_RobotIdle" };
		SetRotationCurves(clip, "Neck", new[] { 0f, 1f, 2f }, null, new[] { -25f, 25f, -25f }, null);
		SetRotationCurves(clip, "Shoulder R", new[] { 0f, 0.5f, 1f, 1.5f, 2f }, null, null, new[] { 10f, 140f, 100f, 140f, 10f });
		SetRotationCurves(clip, "Shoulder L", new[] { 0f, 1f, 2f }, null, null, new[] { -8f, -30f, -8f });
		clip.SetCurve(torso.name, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0, 2, 0));
		clip.SetCurve(torso.name, typeof(Transform), "m_LocalPosition.y", new AnimationCurve(new Keyframe(0, 0.7f), new Keyframe(0.5f, 0.74f), new Keyframe(1, 0.7f), new Keyframe(1.5f, 0.74f), new Keyframe(2, 0.7f)));
		clip.SetCurve(torso.name, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0, 2, 0));

		var settings = AnimationUtility.GetAnimationClipSettings(clip);
		settings.loopTime = true;
		AnimationUtility.SetAnimationClipSettings(clip, settings);

		string clipPath = $"{Folder}/Icon3DDemo_RobotIdle.anim";
		string controllerPath = $"{Folder}/Icon3DDemo_RobotAnimated.controller";
		AssetDatabase.DeleteAsset(clipPath);
		AssetDatabase.DeleteAsset(controllerPath);
		AssetDatabase.CreateAsset(clip, clipPath);
		var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(controllerPath, clip);

		var animator = root.AddComponent<Animator>();
		animator.runtimeAnimatorController = controller;

		string path = $"{Folder}/Icon3DDemo_RobotAnimated.prefab";
		PrefabUtility.SaveAsPrefabAsset(root, path);
		Object.DestroyImmediate(root);
		return path;
	}

	private static GameObject AddPivot( GameObject _parent, string _name, Vector3 _position )
	{
		var pivot = new GameObject(_name);
		pivot.transform.SetParent(_parent.transform, false);
		pivot.transform.localPosition = _position;
		return pivot;
	}

	private static void SetRotationCurves( AnimationClip _clip, string _path, float[] _times, float[] _x, float[] _y, float[] _z )
	{
		_clip.SetCurve(_path, typeof(Transform), "localEulerAnglesRaw.x", Curve(_times, _x));
		_clip.SetCurve(_path, typeof(Transform), "localEulerAnglesRaw.y", Curve(_times, _y));
		_clip.SetCurve(_path, typeof(Transform), "localEulerAnglesRaw.z", Curve(_times, _z));
	}

	private static AnimationCurve Curve( float[] _times, float[] _values )
	{
		var keys = new Keyframe[_times.Length];
		for (int i = 0; i < _times.Length; i++)
			keys[i] = new Keyframe(_times[i], _values != null ? _values[i] : 0f);
		return new AnimationCurve(keys);
	}

	private static void AddSparks( GameObject _parent )
	{
		string materialPath = $"{Folder}/Icon3DDemo_Sparks.mat";
		var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
		if (material == null)
		{
			// Additive: over a transparent background it adds light, which is what premultiplied alpha expects
			material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
			AssetDatabase.CreateAsset(material, materialPath);
		}

		var go = new GameObject("Sparks");
		go.transform.SetParent(_parent.transform, false);
		go.transform.localRotation = Quaternion.Euler(-90, 0, 0);
		var particles = go.AddComponent<ParticleSystem>();
		particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = particles.main;
		main.duration = 1;
		main.loop = true;
		main.startLifetime = 0.6f;
		main.startSpeed = 0.6f;
		main.startSize = 0.06f;
		main.startColor = new Color(1f, 0.8f, 0.3f);
		main.simulationSpace = ParticleSystemSimulationSpace.Local;
		main.playOnAwake = true;

		var emission = particles.emission;
		emission.rateOverTime = 25;

		var shape = particles.shape;
		shape.shapeType = ParticleSystemShapeType.Cone;
		shape.angle = 30;
		shape.radius = 0.02f;

		go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
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

	private static GameObject AddPart( GameObject _root, PrimitiveType _type, string _name, Vector3 _position, Vector3 _scale, Material _material )
	{
		var part = GameObject.CreatePrimitive(_type);
		part.name = _name;
		Object.DestroyImmediate(part.GetComponent<Collider>());
		part.transform.SetParent(_root.transform, false);
		part.transform.localPosition = _position;
		part.transform.localScale = _scale;
		part.GetComponent<Renderer>().sharedMaterial = _material;
		return part;
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
}
