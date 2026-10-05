using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Pixel tests for <see cref="UiIcon3DRenderer"/> (Built-in): isolation from the scene and between icons,
	/// transparency, framing, sharing. The environment test compares against the oracle found in
	/// <see cref="TestIcon3DEnvironmentFindings"/>: the same render with a clean scene active.
	/// </summary>
	public class TestIcon3DRenderer
	{
		private const int Size = 32;
		private const int TestLayer = 31;
		private const float Threshold = 0.05f;

		private Scene m_mainScene;
		private Material m_whiteLit;
		private Texture2D m_readback;
		private Cubemap m_blackCube;
		private readonly List<Object> m_cleanup = new();
		private readonly List<Icon3DHandle> m_handles = new();

		private AmbientMode m_savedAmbientMode;
		private Color m_savedAmbientLight;
		private float m_savedAmbientIntensity;
		private bool m_savedFog;
		private Color m_savedFogColor;
		private FogMode m_savedFogMode;
		private float m_savedFogDensity;

		[UnitySetUp]
		public IEnumerator SetUp()
		{
			m_mainScene = SceneManager.GetActiveScene();
			m_savedAmbientMode = RenderSettings.ambientMode;
			m_savedAmbientLight = RenderSettings.ambientLight;
			m_savedAmbientIntensity = RenderSettings.ambientIntensity;
			m_savedFog = RenderSettings.fog;
			m_savedFogColor = RenderSettings.fogColor;
			m_savedFogMode = RenderSettings.fogMode;
			m_savedFogDensity = RenderSettings.fogDensity;

			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 4;

			m_whiteLit = Track(Icon3DTestModels.CreateOpaque("White", Color.white, 0, 0.5f));
			m_whiteLit.SetFloat("_Glossiness", 0);
			m_whiteLit.SetFloat("_Metallic", 0);
			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));
			m_blackCube = Track(CreateCube(Color.black));
			yield return null;
		}

		[UnityTearDown]
		public IEnumerator TearDown()
		{
			foreach (var handle in m_handles)
				handle.Release();
			m_handles.Clear();

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Backend = null;
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;

			if (SceneManager.GetActiveScene() != m_mainScene)
				SceneManager.SetActiveScene(m_mainScene);

			RenderSettings.ambientMode = m_savedAmbientMode;
			RenderSettings.ambientLight = m_savedAmbientLight;
			RenderSettings.ambientIntensity = m_savedAmbientIntensity;
			RenderSettings.fog = m_savedFog;
			RenderSettings.fogColor = m_savedFogColor;
			RenderSettings.fogMode = m_savedFogMode;
			RenderSettings.fogDensity = m_savedFogDensity;

			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.Destroy(obj);
			m_cleanup.Clear();
			yield return null;
		}

		[Test]
		public void Preset_Light_Lights_The_Object()
		{
			var sphere = CreateSphereTemplate();
			var preset = CreatePreset("Red", Color.red);

			var pixels = RenderAndRead(sphere, preset);
			var center = Center(pixels);
			Log("preset light only", center);

			Assert.Greater(center.r, 0.5f);
			Assert.Less(center.g, Threshold);
			Assert.Less(center.b, Threshold);
			Assert.AreEqual(1f, center.a, 0.01f, "object is opaque");
			Assert.AreEqual(0f, pixels[0].a, 0.01f, "background is transparent");
		}

		[Test]
		public void Two_Presets_Do_Not_Influence_Each_Other()
		{
			var sphere = CreateSphereTemplate();
			var red = CreatePreset("Red", Color.red);
			var blue = CreatePreset("Blue", Color.blue);

			var redHandle = Request(sphere, red);
			var blueHandle = Request(sphere, blue);
			UiIcon3DRenderer.Flush();

			var redCenter = Center(Read(redHandle.Texture));
			var blueCenter = Center(Read(blueHandle.Texture));
			Log("red preset", redCenter);
			Log("blue preset", blueCenter);

			Assert.Greater(redCenter.r, 0.5f);
			Assert.Less(redCenter.b, Threshold, "blue light reached the red icon");
			Assert.Greater(blueCenter.b, 0.5f);
			Assert.Less(blueCenter.r, Threshold, "red light reached the blue icon");
		}

		[Test]
		public void Scene_Light_Does_Not_Reach_The_Icon_And_Is_Restored()
		{
			var sphere = CreateSphereTemplate();
			var preset = CreatePreset("Red", Color.red);
			var sceneLight = CreateSceneLight(Color.green);
			int maskBefore = sceneLight.cullingMask;

			var center = Center(RenderAndRead(sphere, preset));
			Log("scene light (green)", center);

			Assert.Less(center.g, Threshold, "green scene light reached the icon");
			Assert.AreEqual(maskBefore, sceneLight.cullingMask, "scene light culling mask not restored");
		}

		[Test]
		public void Scene_Object_At_Stage_Position_Is_Not_Rendered()
		{
			var sphere = CreateSphereTemplate();
			var preset = CreatePreset("White", Color.white);

			// Camera looks along +Z (identity view) at the sphere on the stage; a red unlit scene cube on the
			// default layer sits between them, covering the centre
			var blocker = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
			blocker.transform.position = UiIcon3DRenderer.StagePosition + new Vector3(0, 0, -0.6f);
			blocker.transform.localScale = Vector3.one * 0.2f;
			blocker.GetComponent<Renderer>().sharedMaterial = Track(Icon3DTestModels.CreateUnlit(Color.red));

			var pixels = RenderAndRead(sphere, preset, Quaternion.identity);
			Log("scene object at stage", Center(pixels));
			Assert.Less(Mathf.Abs(Center(pixels).r - Center(pixels).g), Threshold, "red scene cube visible in the icon");
			Assert.AreEqual(0f, pixels[0].a, 0.01f, "scene cube fills the background");
		}

		/// The central isolation test: a hostile scene environment must not change the icon at all.
		/// Expected image = oracle: the same icon rendered with a clean scene active and environment overrides off.
		[UnityTest]
		public IEnumerator Scene_Environment_Does_Not_Reach_The_Icon_Oracle()
		{
			var sphere = CreateSphereTemplate();
			var ambient = new Color(0.2f, 0.3f, 0.4f);
			var preset = CreatePreset("WhiteWithAmbient", Color.white, ambient);
			var cleanScene = SceneManager.CreateScene("Icon3DOracleScene");

			// Hostile main scene: magenta ambient, dense red fog, default skybox reflection
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = Color.magenta;
			RenderSettings.ambientIntensity = 1;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Exponential;
			RenderSettings.fogColor = Color.red;
			RenderSettings.fogDensity = 1;

			var handle = Request(sphere, preset);
			UiIcon3DRenderer.Flush();
			var withOverrides = Read(handle.Texture);
			Assert.AreEqual(Color.magenta, RenderSettings.ambientLight, "scene ambient not restored");
			Assert.IsTrue(RenderSettings.fog, "scene fog not restored");

			// Oracle: clean scene active with exactly baseline + preset values, renderer does not touch the environment
			SceneManager.SetActiveScene(cleanScene);
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = ambient;
			RenderSettings.ambientIntensity = 1;
			RenderSettings.fog = false;
			RenderSettings.skybox = null;
			RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
			RenderSettings.customReflectionTexture = m_blackCube;
			RenderSettings.reflectionIntensity = 1;

			UiIcon3DRenderer.Backend = new NoEnvironmentBackend();
			handle.SetDirty();
			UiIcon3DRenderer.Flush();
			var oracle = Read(handle.Texture);
			SceneManager.SetActiveScene(m_mainScene);

			float maxDiff = MaxDifference(withOverrides, oracle, out int worst);
			Log($"oracle at worst pixel {worst % Size},{worst / Size}", oracle[worst]);
			Log($"icon at worst pixel {worst % Size},{worst / Size}", withOverrides[worst]);
			Debug.Log($"ICON3D oracle max channel difference: {maxDiff:F4}");

			yield return SceneManager.UnloadSceneAsync(cleanScene);
			Assert.Less(maxDiff, 0.02f, "scene environment changes the icon - an environment input is not covered");
		}

		[Test]
		public void Edges_Are_Premultiplied()
		{
			var sphere = CreateSphereTemplate();
			var preset = CreatePreset("White", Color.white);

			var pixels = RenderAndRead(sphere, preset);
			bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
			int edgePixels = 0;
			foreach (var p in pixels)
			{
				// Premultiplied holds for linear values; an sRGB texture stores them encoded
				var c = linear ? p.linear : p;
				Assert.LessOrEqual(Mathf.Max(c.r, c.g, c.b), p.a + 0.02f, $"colour exceeds alpha at {p} - not premultiplied");
				if (p.a > 0.05f && p.a < 0.95f)
					edgePixels++;
			}

			Debug.Log($"ICON3D antialiased edge pixels: {edgePixels}");
			Assert.Greater(edgePixels, 0, "no soft edges - MSAA resolve did not happen");
		}

		/// Coverage of a sphere object (padding 5%, view face-on):
		/// - Sphere fit uses the sphere around the bounding box (radius = extents.magnitude), which a rotating cube needs
		///   exactly; for a sphere object it is sqrt(3) too large: pi * (0.45 / sqrt(3))^2 = ~0.21 (measured 0.205).
		/// - Box fit frames the box corners, the sphere's front face cube corners come closest: ~0.44.
		[TestCase(UiIcon3DPreset.EFitMode.Box, 0.35f, 0.6f)]
		[TestCase(UiIcon3DPreset.EFitMode.Sphere, 0.17f, 0.25f)]
		public void Framing_Does_Not_Depend_On_Object_Size( UiIcon3DPreset.EFitMode _fitMode, float _minCoverage, float _maxCoverage )
		{
			var preset = CreatePreset("White", Color.white);
			preset.FitMode = _fitMode;
			var small = CreateSphereTemplate(0.01f);
			var large = CreateSphereTemplate(100f);

			var smallPixels = RenderAndRead(small, preset, Quaternion.identity);
			float smallCoverage = Coverage(smallPixels);
			float largeCoverage = Coverage(RenderAndRead(large, preset, Quaternion.identity));
			Debug.Log($"ICON3D {_fitMode} fit coverage small {smallCoverage:F3} large {largeCoverage:F3}");

			Assert.Greater(smallCoverage, _minCoverage, "object framed too loosely");
			Assert.Less(smallCoverage, _maxCoverage, "object framed too tightly");
			Assert.AreEqual(smallCoverage, largeCoverage, 0.02f, "framing depends on object size");

			// Padding: nothing touches the border
			for (int i = 0; i < Size; i++)
			{
				Assert.Less(smallPixels[i].a, 0.5f, "object touches the bottom border");
				Assert.Less(smallPixels[(Size - 1) * Size + i].a, 0.5f, "object touches the top border");
				Assert.Less(smallPixels[i * Size].a, 0.5f, "object touches the left border");
				Assert.Less(smallPixels[i * Size + Size - 1].a, 0.5f, "object touches the right border");
			}
		}

		[Test]
		public void Identical_Requests_Share_One_Render()
		{
			var sphere = CreateSphereTemplate();
			var preset = CreatePreset("White", Color.white);
			int rendersBefore = UiIcon3DRenderer.RenderCount;

			var a = Request(sphere, preset);
			var b = Request(sphere, preset);
			UiIcon3DRenderer.Flush();

			Assert.AreEqual(a.Key, b.Key);
			Assert.AreSame(a.Texture, b.Texture);
			Assert.AreEqual(1, UiIcon3DRenderer.RenderCount - rendersBefore);
			Assert.AreEqual(1, UiIcon3DRenderer.RequestCount);

			a.Release();
			Assert.IsNotNull(b.Texture, "texture released while another handle still uses it");
			b.Release();
			Assert.AreEqual(0, UiIcon3DRenderer.RequestCount);
		}

		[Test]
		public void Object_Light_Stays_In_Its_Own_Icon()
		{
			var preset = CreatePreset("Dim", Color.white * 0.2f);
			var glowing = CreateSphereTemplate();
			var lightGo = new GameObject("Blue Point");
			lightGo.transform.SetParent(glowing.transform, false);
			lightGo.transform.localPosition = new Vector3(0, 0, -1);   // in front of the sphere's -Z side
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = Color.blue;
			light.range = 5;
			light.intensity = 3;

			var plain = CreateSphereTemplate();

			var glowingHandle = Request(glowing, preset, Quaternion.identity);
			var plainHandle = Request(plain, preset, Quaternion.identity);
			UiIcon3DRenderer.Flush();

			var glowingCenter = Center(Read(glowingHandle.Texture));
			var plainCenter = Center(Read(plainHandle.Texture));
			Log("object with own blue light", glowingCenter);
			Log("object rendered after it", plainCenter);

			Assert.Greater(glowingCenter.b - glowingCenter.r, 0.2f, "own light missing");
			Assert.Less(Mathf.Abs(plainCenter.b - plainCenter.r), Threshold, "other object's light leaked into this icon");
		}

		/// Icon objects are arbitrary game prefabs; their scripts run while they are on the stage.
		/// An exception there (Unity logs it, the render goes on) must not affect other icons or the scene.
		[Test]
		public void Throwing_Object_Script_Does_Not_Break_Other_Icons()
		{
			var preset = CreatePreset("Red", Color.red);
			var broken = CreateSphereTemplate();
			broken.AddComponent<ThrowOnEnable>();
			var fine = CreateSphereTemplate();
			var ambientBefore = RenderSettings.ambientLight;

			LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("test exception from an icon object"));
			var brokenHandle = Request(broken, preset);
			var fineHandle = Request(fine, preset);
			UiIcon3DRenderer.Flush();

			Assert.IsTrue(brokenHandle.IsRendered, "an exception in OnEnable is logged by Unity, the icon still renders");
			Assert.IsTrue(fineHandle.IsRendered);
			Assert.Greater(Center(Read(fineHandle.Texture)).r, 0.5f);
			Assert.AreEqual(ambientBefore, RenderSettings.ambientLight, "environment not restored");
		}

		#region Helpers

		private class ThrowOnEnable : MonoBehaviour
		{
			// The template is inactive, so only the stage instance gets here
			private void OnEnable() => throw new System.InvalidOperationException("test exception from an icon object");
		}

		/// Backend for the oracle: renders like Built-in but leaves the environment to the active scene.
		private class NoEnvironmentBackend : IIcon3DRenderBackend
		{
			private readonly BuiltinIcon3DBackend m_builtin = new();
			public bool LightsHonourCullingMask => m_builtin.LightsHonourCullingMask;
			public void SetupCamera( Camera _camera ) => m_builtin.SetupCamera(_camera);
			public void BeginEnvironment( UiIcon3DPreset _preset ) { }
			public void Render( Camera _camera ) => m_builtin.Render(_camera);
			public void EndEnvironment() { }
			public void Dispose() => m_builtin.Dispose();
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}

		private Icon3DHandle Request( GameObject _template, UiIcon3DPreset _preset, Quaternion? _rotation = null )
		{
			var handle = UiIcon3DRenderer.RenderStatic(_template, _preset, new Vector2Int(Size, Size), _rotation);
			m_handles.Add(handle);
			return handle;
		}

		private Color[] RenderAndRead( GameObject _template, UiIcon3DPreset _preset, Quaternion? _rotation = null )
		{
			var handle = Request(_template, _preset, _rotation);
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(handle.IsRendered, "icon was not rendered");
			return Read(handle.Texture);
		}

		private Color[] Read( RenderTexture _texture )
		{
			var previous = RenderTexture.active;
			RenderTexture.active = _texture;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		/// Inactive template, so it is neither rendered in the scene nor awake; the renderer copies it.
		private GameObject CreateSphereTemplate( float _scale = 1 )
		{
			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.transform.localScale = Vector3.one * _scale;
			sphere.GetComponent<Renderer>().sharedMaterial = m_whiteLit;
			return sphere;
		}

		/// Preset with one directional light shining along the view direction, i.e. from the camera.
		private UiIcon3DPreset CreatePreset( string _name, Color _lightColor, Color? _ambient = null )
		{
			var go = Track(new GameObject(_name));
			go.SetActive(false);
			var preset = go.AddComponent<UiIcon3DPreset>();
			preset.FitMode = UiIcon3DPreset.EFitMode.Sphere;
			preset.Padding = 0.05f;

			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(go.transform, false);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.color = _lightColor;
			light.intensity = 1.5f;
			light.shadows = LightShadows.None;

			if (_ambient.HasValue)
			{
				var settings = go.AddComponent<UiCameraRenderSettings>();
				settings.AmbientMode.Value = AmbientMode.Flat;
				settings.AmbientMode.Enabled = true;
				settings.AmbientLight.Value = _ambient.Value;
				settings.AmbientLight.Enabled = true;
				settings.AmbientIntensity.Value = 1;
				settings.AmbientIntensity.Enabled = true;
			}

			return preset;
		}

		private Light CreateSceneLight( Color _color )
		{
			var go = Track(new GameObject("Scene Light"));
			var light = go.AddComponent<Light>();
			light.type = LightType.Directional;
			light.color = _color;
			light.intensity = 2;
			light.cullingMask = ~0;
			go.transform.rotation = Quaternion.Euler(0, 180, 0);   // shines at the front the icon camera sees
			return light;
		}

		private static Cubemap CreateCube( Color _color )
		{
			const int size = 4;
			var result = new Cubemap(size, TextureFormat.RGBA32, false);
			var pixels = new Color[size * size];
			for (int i = 0; i < pixels.Length; i++)
				pixels[i] = _color;
			for (int face = 0; face < 6; face++)
				result.SetPixels(pixels, (CubemapFace)face);
			result.Apply();
			return result;
		}

		private static Color Center( Color[] _pixels ) => _pixels[Size / 2 * Size + Size / 2];

		private static float Coverage( Color[] _pixels )
		{
			int covered = 0;
			foreach (var p in _pixels)
				if (p.a > 0.5f)
					covered++;
			return (float)covered / _pixels.Length;
		}

		private static float MaxDifference( Color[] _a, Color[] _b, out int _worstIndex )
		{
			float maxDiff = 0;
			_worstIndex = 0;
			for (int i = 0; i < _a.Length; i++)
			{
				var d = _a[i] - _b[i];
				float diff = Mathf.Max(Mathf.Abs(d.r), Mathf.Abs(d.g), Mathf.Abs(d.b), Mathf.Abs(d.a));
				if (diff > maxDiff)
				{
					maxDiff = diff;
					_worstIndex = i;
				}
			}

			return maxDiff;
		}

		private static void Log( string _what, Color _color ) =>
			Debug.Log($"ICON3D {_what}: r={_color.r:F3} g={_color.g:F3} b={_color.b:F3} a={_color.a:F3}");

		#endregion
	}
}
