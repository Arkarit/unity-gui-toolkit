using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Test
{
	/// <summary>
	/// 3D icons with skinned and multi material objects, on self made models (<see cref="Icon3DTestModels"/>): skinning
	/// and the Animator reach the icon, the frame fits, nothing leaks between icons, transparent and multi material
	/// objects render. Static meshes are covered by TestIcon3DRenderer.
	/// </summary>
	public class TestIcon3DSkinned
	{
		private const int TestLayer = 31;
		private const int Size = 64;

		private static readonly string[] AnimatedModels = { Icon3DTestModels.TentaclePath, Icon3DTestModels.PuppetPath };

		private readonly List<Object> m_cleanup = new();
		private Canvas m_canvas;
		private UiIcon3DPreset m_preset;
		private Texture2D m_readback;

		[OneTimeSetUp]
		public void GenerateModels()
		{
#if UNITY_EDITOR
			Icon3DTestModels.EnsureAssets();
#endif
		}

		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;

			var canvasGo = Track(new GameObject("Canvas", typeof(Canvas)));
			m_canvas = canvasGo.GetComponent<Canvas>();
			m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));

			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			m_preset = presetGo.AddComponent<UiIcon3DPreset>();
			m_preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1f;
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_cleanup.Clear();

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
		}

		[Test]
		public void Models_Are_Well_Formed()
		{
			var tentacle = Load(Icon3DTestModels.TentaclePath).GetComponentInChildren<SkinnedMeshRenderer>();
			Assert.AreEqual(Icon3DTestModels.TentacleBones, tentacle.bones.Length);
			AssertSkinning(tentacle.sharedMesh, Icon3DTestModels.TentacleBones);

			var puppet = Load(Icon3DTestModels.PuppetPath).GetComponentInChildren<SkinnedMeshRenderer>();
			Assert.AreEqual(Icon3DTestModels.PuppetBones, puppet.bones.Length);
			AssertSkinning(puppet.sharedMesh, Icon3DTestModels.PuppetBones);

			Assert.IsNotNull(Load(Icon3DTestModels.TentaclePath).GetComponent<Animator>().runtimeAnimatorController);
			Assert.IsNotNull(Load(Icon3DTestModels.PuppetPath).GetComponent<Animator>().runtimeAnimatorController);

			var goblet = Load(Icon3DTestModels.GobletPath).GetComponent<MeshRenderer>();
			Assert.AreEqual(2, goblet.sharedMaterials.Length);
			Assert.AreEqual(2, goblet.GetComponent<MeshFilter>().sharedMesh.subMeshCount);

			Assert.AreEqual(3, Load(Icon3DTestModels.TrophyPath).GetComponentsInChildren<MeshRenderer>().Length);
		}

		[TestCase(Icon3DTestModels.TentaclePath)]
		[TestCase(Icon3DTestModels.PuppetPath)]
		[TestCase(Icon3DTestModels.GobletPath)]
		[TestCase(Icon3DTestModels.TrophyPath)]
		public void Static_Icon_Shows_The_Object_And_Fits_The_Frame(string _path)
		{
			var icon = CreateIcon(Load(_path));
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(icon.IsRendered);

			var pixels = Read(icon);
			int covered = 0, border = 0;
			for (int y = 0; y < Size; y++)
			{
				for (int x = 0; x < Size; x++)
				{
					bool filled = pixels[y * Size + x].a > 0.5f;
					if (filled)
						covered++;
					if (filled && (x == 0 || y == 0 || x == Size - 1 || y == Size - 1))
						border++;
				}
			}

			Debug.Log($"ICON3D {System.IO.Path.GetFileNameWithoutExtension(_path)}: covered {covered * 100f / (Size * Size):F1}% of the frame, {border} border pixels");
			Assert.Greater(covered, Size * Size * 0.04f, "object (nearly) invisible");
			Assert.AreEqual(0, border, "object touches the frame edge - fitting too tight or bounds wrong (skinned bounds)");
		}

		[Test]
		public void Skinned_Icon_Is_Deterministic_And_Unaffected_By_An_Animated_Neighbour()
		{
			var puppet = Load(Icon3DTestModels.PuppetPath);
			var first = CreateIcon(puppet);
			UiIcon3DRenderer.Flush();
			var before = Hash(Read(first));

			var animated = CreateIcon(Load(Icon3DTestModels.TentaclePath));
			animated.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();
			UiIcon3DRenderer.Flush();

			var second = CreateIcon(puppet);
			UiIcon3DRenderer.Flush();
			Assert.AreEqual(before, Hash(Read(second)), "a skinned icon renders differently next to a running animated one");
		}

		[UnityTest]
		public IEnumerator The_Animators_Clip_Moves_The_Skinned_Mesh([ValueSource(nameof(AnimatedModels))] string _path)
		{
			var icon = CreateIcon(Load(_path));
			icon.Mode = UiIcon3D.EMode.Animated;
			UiIcon3DRenderer.Flush();

			var images = new HashSet<string>();
			for (int i = 0; i < 8; i++)
			{
				// Real time: the Animator advances with Time.deltaTime, test runner frames alone are milliseconds apart
				yield return new WaitForSeconds(0.13f);
				UiIcon3DRenderer.Flush();
				images.Add(Hash(Read(icon)));
			}

			Debug.Log($"ICON3D {System.IO.Path.GetFileNameWithoutExtension(_path)} animated: {images.Count} distinct frames of 8");
			Assert.Greater(images.Count, 2, "image does not change - the clip does not reach the skinned mesh in the icon");
		}

		[Test]
		public void Transparent_Bowl_And_Opaque_Stem_Render()
		{
			var icon = CreateIcon(Load(Icon3DTestModels.GobletPath));
			UiIcon3DRenderer.Flush();

			int opaque = 0, translucent = 0;
			foreach (var pixel in Read(icon))
			{
				if (pixel.a > 0.95f)
					opaque++;
				else if (pixel.a > 0.05f)
					translucent++;
			}

			// What alpha a transparent material leaves in the result is the open phase 6 item ("transparent-material
			// alpha fix"): this test only pins that both submeshes draw, and prints the numbers for that work.
			Debug.Log($"ICON3D goblet: {opaque} opaque, {translucent} translucent pixels of {Size * Size}");
			Assert.Greater(opaque, 20, "opaque stem/base missing");
			Assert.Greater(opaque + translucent, Size * Size * 0.08f, "bowl not drawn");
		}

		[Test]
		public void Multi_Material_Trophy_Shows_Metal_Cloth_And_Wood()
		{
			var icon = CreateIcon(Load(Icon3DTestModels.TrophyPath));
			UiIcon3DRenderer.Flush();

			int cloth = 0, gold = 0, wood = 0;
			foreach (var p in Read(icon))
			{
				if (p.a < 0.9f)
					continue;
				if (p.r > p.g * 2.5f && p.r > p.b * 2.5f)
					cloth++;                          // deep red ribbon
				else if (p.r > p.g && p.g > p.b * 1.6f && p.r > 0.35f)
					gold++;                           // metal
				else if (p.r > p.g && p.g > p.b && p.r < 0.35f)
					wood++;                           // dark brown base
			}

			Debug.Log($"ICON3D trophy pixels: cloth {cloth}, metal {gold}, wood {wood}");
			Assert.Greater(cloth, 3, "ribbon material missing");
			Assert.Greater(gold, 20, "metal material missing");
			Assert.Greater(wood, 3, "wood material missing");
		}

		#region Helpers

		private static GameObject Load( string _path )
		{
#if UNITY_EDITOR
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_path);
			Assert.IsNotNull(prefab, $"test model missing: {_path} (menu: Gui Toolkit > 3D Icons > Dev: Rebuild Test Models, in edit mode)");
			return prefab;
#else
			Assert.Ignore("test models are editor generated assets");
			return null;
#endif
		}

		private static void AssertSkinning( Mesh _mesh, int _boneCount )
		{
			Assert.AreEqual(_boneCount, _mesh.bindposes.Length);
			var weights = _mesh.boneWeights;
			Assert.AreEqual(_mesh.vertexCount, weights.Length);
			foreach (var w in weights)
			{
				Assert.AreEqual(1f, w.weight0 + w.weight1 + w.weight2 + w.weight3, 0.01f);
				Assert.Less(w.boneIndex0, _boneCount);
			}
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private UiIcon3D CreateIcon( GameObject _prefab )
		{
			var go = Track(new GameObject("Icon", typeof(RectTransform)));
			go.transform.SetParent(m_canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);
			var icon = go.AddComponent<UiIcon3D>();
			icon.Preset = m_preset;
			icon.Prefab = _prefab;
			return icon;
		}

		private Color[] Read( UiIcon3D _icon )
		{
			var previous = RenderTexture.active;
			RenderTexture.active = (RenderTexture)_icon.RawImage.texture;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		private static string Hash( Color[] _pixels )
		{
			var sb = new System.Text.StringBuilder();
			for (int i = 0; i < _pixels.Length; i += 5)
				sb.Append((int)(_pixels[i].a * 10)).Append((int)(_pixels[i].r * 10));
			return sb.ToString();
		}

		#endregion
	}
}
