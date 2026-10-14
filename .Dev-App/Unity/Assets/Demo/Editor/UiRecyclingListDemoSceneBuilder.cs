using GuiToolkit;
using GuiToolkit.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Builds the UiRecyclingList demo: a horizontal scroll list (authored with a HorizontalLayoutGroup, as it would be
/// in a real screen) of a thousand cards, each with an animated 3D icon of the demo robots.
/// </summary>
public static class UiRecyclingListDemoSceneBuilder
{
	private const string Folder = "Assets/Demo/RecyclingList";
	private const string ItemPrefabPath = Folder + "/RecyclingListDemoItem.prefab";
	private const string ScenePath = "Assets/Scenes/RecyclingListDemo.unity";
	private const string IconPrefabPath = "Assets/External/unity-gui-toolkit/Prefabs/StandardElements/StandardIcon3D.prefab";
	private static readonly string[] ObjectPaths =
	{
		"Assets/Demo/Icon3D/Icon3DDemo_RobotAnimated.prefab",
		"Assets/Demo/Icon3D/Icon3DDemo_Robot.prefab",
		"Assets/Demo/Icon3D/Icon3DDemo_Robot_WithLargeHead.prefab",
	};

	[MenuItem(StringConstants.MENU_HEADER + "3D Icons/Create Recycling List Demo")]
	private static void Build()
	{
		if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			return;

		Icon3DSetup.CreateLayer("Icon3D");
		if (AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath) == null)
			Icon3DLibraryAssetBuilder.Build();

		BuildItemPrefab();

		// Loaded after the new scene: opening it unloads what nothing references, the fresh prefab among it
		var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
		var itemPrefab = AssetDatabase.LoadAssetAtPath<UiRecyclingListDemoItem>(ItemPrefabPath);
		new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

		var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
		var scaler = canvasGo.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920, 1080);

		// Scroll view: ScrollRect > Viewport (RectMask2D) > Content (HorizontalLayoutGroup + ContentSizeFitter)
		var scrollGo = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
		scrollGo.transform.SetParent(canvasGo.transform, false);
		var scrollRt = (RectTransform)scrollGo.transform;
		scrollRt.anchorMin = new Vector2(0, 0.5f);
		scrollRt.anchorMax = new Vector2(1, 0.5f);
		scrollRt.sizeDelta = new Vector2(-160, 420);
		scrollGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 1);

		var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
		viewportGo.transform.SetParent(scrollGo.transform, false);
		var viewport = (RectTransform)viewportGo.transform;
		viewport.anchorMin = Vector2.zero;
		viewport.anchorMax = Vector2.one;
		viewport.sizeDelta = Vector2.zero;

		var contentGo = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
		contentGo.transform.SetParent(viewport, false);
		var content = (RectTransform)contentGo.transform;
		content.anchorMin = new Vector2(0, 0);
		content.anchorMax = new Vector2(0, 1);
		content.pivot = new Vector2(0, 0.5f);
		content.sizeDelta = Vector2.zero;
		var layout = contentGo.GetComponent<HorizontalLayoutGroup>();
		layout.spacing = 24;
		layout.padding = new RectOffset(24, 24, 0, 0);
		layout.childAlignment = TextAnchor.MiddleLeft;
		layout.childControlWidth = false;
		layout.childControlHeight = false;
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;
		contentGo.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

		var scrollRect = scrollGo.GetComponent<ScrollRect>();
		scrollRect.viewport = viewport;
		scrollRect.content = content;
		scrollRect.vertical = false;
		scrollRect.scrollSensitivity = 60;

		// Three items in the editor, so the layout can be seen and tuned; the list replaces them at runtime
		for (int i = 0; i < 3; i++)
		{
			var preview = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab.gameObject, content);
			preview.AddComponent<HideOnPlay>();
		}

		var list = scrollGo.AddComponent<UiRecyclingList>();

		var objects = new GameObject[ObjectPaths.Length];
		for (int i = 0; i < objects.Length; i++)
			objects[i] = AssetDatabase.LoadAssetAtPath<GameObject>(ObjectPaths[i]);

		var demo = canvasGo.AddComponent<UiRecyclingListDemo>();
		demo.Setup(list, itemPrefab, objects);

		EditorSceneManager.SaveScene(scene, ScenePath);
		UiLog.Log($"Recycling list demo scene created: {ScenePath}");
	}

	private static void BuildItemPrefab()
	{
		System.IO.Directory.CreateDirectory(Folder);

		var root = new GameObject("RecyclingListDemoItem", typeof(RectTransform), typeof(Image), typeof(Button));
		((RectTransform)root.transform).sizeDelta = new Vector2(260, 340);
		var background = root.GetComponent<Image>();

		var iconGo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(IconPrefabPath), root.transform);
		var iconRt = (RectTransform)iconGo.transform;
		iconRt.anchorMin = new Vector2(0, 0);
		iconRt.anchorMax = new Vector2(1, 1);
		iconRt.offsetMin = new Vector2(10, 50);
		iconRt.offsetMax = new Vector2(-10, -10);
		var icon = iconGo.GetComponent<UiIcon3D>();
		icon.Mode = UiIcon3D.EMode.Animated;
		icon.RawImage.raycastTarget = false;

		var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
		labelGo.transform.SetParent(root.transform, false);
		var labelRt = (RectTransform)labelGo.transform;
		labelRt.anchorMin = new Vector2(0, 0);
		labelRt.anchorMax = new Vector2(1, 0);
		labelRt.pivot = new Vector2(0.5f, 0);
		labelRt.sizeDelta = new Vector2(0, 50);
		var label = labelGo.GetComponent<TextMeshProUGUI>();
		label.text = "#0";
		label.fontSize = 30;
		label.alignment = TextAlignmentOptions.Center;
		label.raycastTarget = false;

		var item = root.AddComponent<UiRecyclingListDemoItem>();
		item.Setup(background, icon, label, root.GetComponent<Button>());

		PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
		Object.DestroyImmediate(root);
	}
}
