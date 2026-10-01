using System.Collections.Generic;
using System.IO;
using GuiToolkit;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the scroll stress scene for 3D icons (phase 3): 100 library icons in a scroll view (RectMask2D),
/// showing 30 generated objects that are loaded by canonical id through the Resources provider.
/// The Icon3DScrollStressDemo component on the canvas can reassign icons while their loads are pending.
/// </summary>
public static class Icon3DScrollStressSceneBuilder
{
	private const string ItemFolder = "Assets/Demo/Icon3D/Resources/Icon3DDemoItems";
	private const string MaterialFolder = "Assets/Demo/Icon3D/Materials";
	private const string ScenePath = "Assets/Scenes/Icon3DScrollStress.unity";
	private const string IconPrefabPath = "Assets/External/unity-gui-toolkit/Prefabs/StandardElements/StandardIcon3D.prefab";
	private const int ItemCount = 30;
	private const int IconCount = 100;

	[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Create Scroll Stress Demo")]
	private static void Build()
	{
		if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		Directory.CreateDirectory(ItemFolder);
		Directory.CreateDirectory(MaterialFolder);
		Icon3DSetup.CreateLayer("Icon3D");

		if (AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath) == null)
			Icon3DLibraryAssetBuilder.Build();

		var ids = CreateItems();

		var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
		var iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath);
		var warm = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DLibraryAssetBuilder.WarmPresetPath).GetComponent<UiIcon3DPreset>();
		var dramatic = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DLibraryAssetBuilder.DramaticPresetPath).GetComponent<UiIcon3DPreset>();

		var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGo.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		// Scroll view: ScrollRect > Viewport (RectMask2D) > Content (grid)
		var scrollGo = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
		scrollGo.transform.SetParent(canvasGo.transform, false);
		Stretch((RectTransform)scrollGo.transform, new Vector2(80, 60), new Vector2(-80, -140));
		scrollGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f, 1);

		var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
		viewportGo.transform.SetParent(scrollGo.transform, false);
		Stretch((RectTransform)viewportGo.transform, Vector2.zero, Vector2.zero);

		var contentGo = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
		contentGo.transform.SetParent(viewportGo.transform, false);
		var content = (RectTransform)contentGo.transform;
		content.anchorMin = new Vector2(0, 1);
		content.anchorMax = new Vector2(1, 1);
		content.pivot = new Vector2(0.5f, 1);
		content.sizeDelta = Vector2.zero;
		var grid = contentGo.GetComponent<GridLayoutGroup>();
		grid.cellSize = new Vector2(150, 150);
		grid.spacing = new Vector2(12, 12);
		grid.padding = new RectOffset(12, 12, 12, 12);
		contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

		var scrollRect = scrollGo.GetComponent<ScrollRect>();
		scrollRect.viewport = (RectTransform)viewportGo.transform;
		scrollRect.content = content;
		scrollRect.horizontal = false;
		scrollRect.scrollSensitivity = 40;

		var icons = new UiIcon3D[IconCount];
		for (int i = 0; i < IconCount; i++)
		{
			var go = (GameObject)PrefabUtility.InstantiatePrefab(iconPrefab, content);
			go.name = $"Icon {i:00}";
			var icon = go.GetComponent<UiIcon3D>();
			var so = new SerializedObject(icon);
			so.FindProperty("m_preset").objectReferenceValue = i % 11 == 0 ? dramatic : i % 7 == 0 ? warm : null;
			so.FindProperty("m_prefabRef").FindPropertyRelative("Id").stringValue = ids[i % ids.Count];
			so.ApplyModifiedPropertiesWithoutUndo();
			icons[i] = icon;
		}

		var demo = canvasGo.AddComponent<Icon3DScrollStressDemo>();
		demo.Setup(icons, ids.ToArray());

		EditorSceneManager.SaveScene(scene, ScenePath);
		UiLog.Log($"3D icon scroll stress scene created: {ScenePath}");
	}

	private static void Stretch( RectTransform _rt, Vector2 _offsetMin, Vector2 _offsetMax )
	{
		_rt.anchorMin = Vector2.zero;
		_rt.anchorMax = Vector2.one;
		_rt.offsetMin = _offsetMin;
		_rt.offsetMax = _offsetMax;
	}

	/// 30 different little objects from primitives, saved under Resources; returns their canonical ids.
	private static List<string> CreateItems()
	{
		var materials = new[]
		{
			CreateMaterial("Red", new Color(0.85f, 0.2f, 0.2f), 0.5f, 0f),
			CreateMaterial("Green", new Color(0.25f, 0.75f, 0.3f), 0.4f, 0f),
			CreateMaterial("Blue", new Color(0.25f, 0.45f, 0.9f), 0.6f, 0f),
			CreateMaterial("Yellow", new Color(0.95f, 0.8f, 0.2f), 0.5f, 0f),
			CreateMaterial("Gold", new Color(1f, 0.78f, 0.35f), 0.8f, 1f),
			CreateMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.85f, 1f),
			CreateMaterial("White", new Color(0.9f, 0.9f, 0.9f), 0.3f, 0f),
			CreateMaterial("Dark", new Color(0.15f, 0.15f, 0.17f), 0.6f, 0f),
		};
		var shapes = new[] { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Capsule, PrimitiveType.Cylinder };

		var ids = new List<string>();
		for (int i = 0; i < ItemCount; i++)
		{
			var random = new System.Random(1000 + i);
			var root = new GameObject($"Item_{i:00}");

			// A stack of 2 to 4 parts, each a bit smaller, sometimes tilted
			float y = 0;
			int parts = 2 + random.Next(3);
			for (int p = 0; p < parts; p++)
			{
				var part = GameObject.CreatePrimitive(shapes[random.Next(shapes.Length)]);
				Object.DestroyImmediate(part.GetComponent<Collider>());
				part.transform.SetParent(root.transform, false);
				float size = Mathf.Lerp(0.9f, 0.4f, (float)p / parts) * (0.8f + 0.4f * (float)random.NextDouble());
				part.transform.localScale = new Vector3(size * (0.7f + 0.6f * (float)random.NextDouble()), size, size);
				part.transform.localPosition = new Vector3(0, y + size * 0.5f, 0);
				part.transform.localRotation = Quaternion.Euler(0, random.Next(4) * 22.5f, random.Next(3) == 0 ? 20 : 0);
				part.GetComponent<Renderer>().sharedMaterial = materials[random.Next(materials.Length)];
				y += size;
			}

			string path = $"{ItemFolder}/Item_{i:00}.prefab";
			PrefabUtility.SaveAsPrefabAsset(root, path);
			Object.DestroyImmediate(root);
			ids.Add($"res:Icon3DDemoItems/Item_{i:00}");
		}

		return ids;
	}

	private static Material CreateMaterial( string _name, Color _color, float _smoothness, float _metallic )
	{
		string path = $"{MaterialFolder}/Icon3DDemoItem_{_name}.mat";
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
