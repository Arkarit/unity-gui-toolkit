using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Findings of the scene isolation spike for the 3D icon plan (.Dev-App/Planning/3D-Icons.md), kept as tests.
	///
	/// Q1-Q6 pin down how Unity (Built-in) behaves today. They assert the behaviour as found, so a Unity update
	/// that changes it shows up as a failure here - that would be a reason to revisit the plan, not a bug.
	/// Q7-Q8 check the approach the plan relies on: per-camera overrides of RenderSettings, compared against the
	/// "oracle" of switching the active scene, which swaps the complete environment.
	///
	/// All measured colours are logged with the prefix "SPIKE".
	///
	/// Setup: a white Standard-shader sphere in a runtime-created "icon scene", rendered by a camera with
	/// <c>Camera.scene</c> set to that scene, against lights, objects and ambient in the test runner's (active) scene.
	/// </summary>
	public class SpikeIcon3DSceneIsolation
	{
		private const int Size = 32;
		private const float Threshold = 0.05f;
		private const string ChangedHint = " - Unity behaviour changed, revisit the 3D icon plan";

		private Scene m_mainScene;
		private Scene m_iconScene;
		private RenderTexture m_target;
		private Texture2D m_readback;
		private Material m_whiteLit;
		private Cubemap m_blackCube;
		private Cubemap m_yellowCube;

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

			// Neutral main scene environment: black flat ambient, no fog
			SetAmbient(Color.black);
			RenderSettings.fog = false;

			m_iconScene = SceneManager.CreateScene("Icon3DSpikeScene");
			m_target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
			m_readback = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
			m_whiteLit = new Material(Shader.Find("Standard")) { color = Color.white };
			m_whiteLit.SetFloat("_Glossiness", 0);
			m_whiteLit.SetFloat("_Metallic", 0);
			m_blackCube = CreateCube(Color.black);
			m_yellowCube = CreateCube(Color.yellow);
			yield return null;
		}

		[UnityTearDown]
		public IEnumerator TearDown()
		{
			if (SceneManager.GetActiveScene() != m_mainScene)
				SceneManager.SetActiveScene(m_mainScene);

			RenderSettings.ambientMode = m_savedAmbientMode;
			RenderSettings.ambientLight = m_savedAmbientLight;
			RenderSettings.ambientIntensity = m_savedAmbientIntensity;
			RenderSettings.fog = m_savedFog;
			RenderSettings.fogColor = m_savedFogColor;
			RenderSettings.fogMode = m_savedFogMode;
			RenderSettings.fogDensity = m_savedFogDensity;

			foreach (var root in m_mainScene.GetRootGameObjects())
				if (root.name.StartsWith("Spike"))
					Object.Destroy(root);

			Object.Destroy(m_target);
			Object.Destroy(m_readback);
			Object.Destroy(m_whiteLit);
			Object.Destroy(m_blackCube);
			Object.Destroy(m_yellowCube);
			yield return SceneManager.UnloadSceneAsync(m_iconScene);
		}

		/// Found: Camera.scene does not filter objects of a runtime-created scene.
		[UnityTest]
		public IEnumerator Q1_Camera_Scene_Does_Not_Filter_Objects()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);
			CreateLight(m_iconScene, Color.white);

			// Big red unlit blocker in the main scene, right in front of the icon camera
			var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
			blocker.name = "SpikeBlocker";
			blocker.transform.position = new Vector3(0, 0, -1.5f);
			blocker.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = Color.red };
			yield return null;

			var c = RenderCenter(camera);
			Log("Q1 objects", c);
			Assert.Greater(c.r - c.g, 0.5f, "main-scene blocker no longer visible" + ChangedHint);
		}

		/// Found: lights of other scenes light the icon.
		[UnityTest]
		public IEnumerator Q2_Foreign_Scene_Lights_Light_Icon()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);
			CreateLight(m_mainScene, Color.green);
			yield return null;

			var c = RenderCenter(camera);
			Log("Q2 foreign light", c);
			Assert.Greater(c.g, 0.5f, "main-scene light no longer reaches the icon" + ChangedHint);
		}

		/// Found: icon scene lights light other scenes.
		[UnityTest]
		public IEnumerator Q3_Icon_Scene_Lights_Light_Main_Scene()
		{
			CreateLight(m_iconScene, Color.red);
			CreateSphere(m_mainScene, new Vector3(100, 0, 0));
			var camera = CreateCamera(m_mainScene, m_mainScene, new Vector3(100, 0, 0));
			yield return null;

			var c = RenderCenter(camera);
			Log("Q3 icon light in main scene", c);
			Assert.Greater(c.r, 0.5f, "icon-scene light no longer reaches the main scene" + ChangedHint);
		}

		/// Found: the active scene's environment is used, not the icon scene's.
		[UnityTest]
		public IEnumerator Q4_Q5_Active_Scene_Environment_Wins()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);

			// RenderSettings write to the active scene: give the icon scene cyan, keep the main scene magenta
			SceneManager.SetActiveScene(m_iconScene);
			SetAmbient(Color.cyan);
			SceneManager.SetActiveScene(m_mainScene);
			SetAmbient(Color.magenta);
			yield return null;

			var c = RenderCenter(camera);
			Log("Q4/Q5 own scene ambient (cyan) vs active (magenta)", c);
			Assert.Greater(c.r, 0.5f, "icon no longer gets the active scene's magenta" + ChangedHint);
			// ~0.08 in every channel is the main scene's environment reflection (see Q8a), cyan would be ~0.8
			Assert.Less(c.g, 0.3f, "icon now gets its own scene's cyan" + ChangedHint);
		}

		/// Found: switching the active scene swaps the complete environment immediately, within the frame.
		/// Not used in production (fires SceneManager.activeSceneChanged), but as the oracle in Q8.
		[UnityTest]
		public IEnumerator Q6_Switching_Active_Scene_Around_Render_Switches_Environment()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);

			SceneManager.SetActiveScene(m_iconScene);
			SetAmbient(Color.cyan);
			SceneManager.SetActiveScene(m_mainScene);
			SetAmbient(Color.magenta);
			yield return null;

			int sceneChangedEvents = 0;
			UnityEngine.Events.UnityAction<Scene, Scene> counter = ( _, _ ) => sceneChangedEvents++;
			SceneManager.activeSceneChanged += counter;

			SceneManager.SetActiveScene(m_iconScene);
			var c = RenderCenter(camera);
			SceneManager.SetActiveScene(m_mainScene);

			SceneManager.activeSceneChanged -= counter;

			Log("Q6 active scene switched around render", c);
			Debug.Log($"SPIKE Q6 activeSceneChanged events per render: {sceneChangedEvents}");
			Assert.Greater(c.g, 0.5f, "switching the active scene no longer switches the environment" + ChangedHint);
			Assert.Less(c.r, Threshold);
			Assert.AreEqual(Color.magenta, RenderSettings.ambientLight, "main scene environment was not restored");
		}

		/// The plan's approach: UiCameraRenderSettings on the icon camera overrides the active scene's
		/// ambient, fog and environment reflection.
		[UnityTest]
		public IEnumerator Q7_Per_Camera_Override_Changes_The_Image()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);
			SetAmbient(Color.magenta);
			SetDenseRedFog();
			AddOverrides(camera, m_blackCube);
			yield return null;

			var c = RenderCenter(camera);
			Log("Q7 override cyan, no fog, black reflection (scene magenta, red fog, skybox reflection)", c);
			Assert.Greater(c.g, 0.5f, "override ambient not visible in the image");
			Assert.Less(c.r, Threshold, "scene ambient, fog or reflection still visible");
			Assert.AreEqual(Color.magenta, RenderSettings.ambientLight, "scene ambient not restored");
			Assert.IsTrue(RenderSettings.fog, "scene fog not restored");
		}

		/// Found: overriding ambient and fog is not enough; the scene's environment reflection (skybox) leaks in.
		/// This is the oracle doing its job. If this starts to fail, reflection no longer leaks - fine, but unexpected.
		[UnityTest]
		public IEnumerator Q8a_Without_Reflection_Override_Reflection_Leaks()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);
			PrepareOracleScenes(m_blackCube);
			yield return null;

			float maxDiff = CompareWithOracle(camera, null, "Q8a");
			Assert.Greater(maxDiff, 0.05f, "reflection no longer leaks without override" + ChangedHint);
		}

		/// Oracle: with ambient, fog and reflection overridden, the image matches a real environment switch.
		[UnityTest]
		public IEnumerator Q8b_Override_Matches_Scene_Switch_Oracle()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);
			PrepareOracleScenes(m_blackCube);
			yield return null;

			float maxDiff = CompareWithOracle(camera, m_blackCube, "Q8b");
			Assert.Less(maxDiff, 0.02f, "override image differs from the oracle - an environment input is not covered");
		}

		/// Like Q8b, but with a visible (yellow) reflection, so the override is shown to set a reflection,
		/// not merely to switch reflection off.
		[UnityTest]
		public IEnumerator Q9_Override_Sets_Preset_Reflection()
		{
			CreateSphere(m_iconScene, Vector3.zero);
			var camera = CreateCamera(m_iconScene, m_iconScene);
			PrepareOracleScenes(m_yellowCube);
			yield return null;

			float maxDiff = CompareWithOracle(camera, m_yellowCube, "Q9");
			// With black reflection the centre has r = 0 (Q7); cyan ambient dominates blue, so only red tells yellow apart
			var c = m_readback.GetPixel(Size / 2, Size / 2);
			Log("Q9 centre with yellow reflection", c);
			Assert.Greater(c.r, 0.03f, "yellow reflection not visible at all - test does not prove anything");
			Assert.Less(maxDiff, 0.02f, "preset reflection differs from the oracle");
		}

		/// Icon scene: the clean environment we want (cyan ambient, no fog, given reflection).
		/// Main scene: as hostile as possible (magenta ambient, dense red fog, skybox reflection).
		private void PrepareOracleScenes( Cubemap _reflection )
		{
			SceneManager.SetActiveScene(m_iconScene);
			SetAmbient(Color.cyan);
			RenderSettings.fog = false;
			RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
			RenderSettings.customReflectionTexture = _reflection;
			RenderSettings.reflectionIntensity = 1;
			SceneManager.SetActiveScene(m_mainScene);

			SetAmbient(Color.magenta);
			SetDenseRedFog();
		}

		/// Renders once with the icon scene active (oracle) and once with overrides on the camera;
		/// returns the largest channel difference. _reflection null = reflection not overridden.
		/// Leaves the override image in m_readback.
		private float CompareWithOracle( Camera _camera, Cubemap _reflection, string _label )
		{
			SceneManager.SetActiveScene(m_iconScene);
			var oracle = RenderAll(_camera);
			SceneManager.SetActiveScene(m_mainScene);

			AddOverrides(_camera, _reflection);
			var overridden = RenderAll(_camera);

			float maxDiff = 0;
			int worstIndex = 0;
			for (int i = 0; i < oracle.Length; i++)
			{
				var d = oracle[i] - overridden[i];
				float diff = Mathf.Max(Mathf.Abs(d.r), Mathf.Abs(d.g), Mathf.Abs(d.b), Mathf.Abs(d.a));
				if (diff > maxDiff)
				{
					maxDiff = diff;
					worstIndex = i;
				}
			}

			Log($"{_label} oracle at worst pixel {worstIndex % Size},{worstIndex / Size}", oracle[worstIndex]);
			Log($"{_label} override at worst pixel {worstIndex % Size},{worstIndex / Size}", overridden[worstIndex]);
			Debug.Log($"SPIKE {_label} max channel difference: {maxDiff:F4}");
			return maxDiff;
		}

		private static void AddOverrides( Camera _camera, Cubemap _reflection )
		{
			var settings = _camera.gameObject.AddComponent<UiCameraRenderSettings>();
			Enable(settings.AmbientMode, AmbientMode.Flat);
			Enable(settings.AmbientLight, Color.cyan);
			Enable(settings.AmbientIntensity, 1f);
			Enable(settings.Fog, false);

			if (_reflection == null)
				return;

			Enable(settings.DefaultReflectionMode, DefaultReflectionMode.Custom);
			Enable(settings.CustomReflection, (Texture)_reflection);
			Enable(settings.ReflectionIntensity, 1f);
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

		private static void Enable<T>( PerCameraSetting<T> _setting, T _value )
		{
			_setting.Value = _value;
			_setting.Enabled = true;
		}

		private static void SetAmbient( Color _color )
		{
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = _color;
			RenderSettings.ambientIntensity = 1;
		}

		private static void SetDenseRedFog()
		{
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Exponential;
			RenderSettings.fogColor = Color.red;
			RenderSettings.fogDensity = 1;
		}

		private GameObject CreateSphere( Scene _scene, Vector3 _position )
		{
			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "SpikeSphere";
			sphere.transform.position = _position;
			sphere.GetComponent<Renderer>().sharedMaterial = m_whiteLit;
			SceneManager.MoveGameObjectToScene(sphere, _scene);
			return sphere;
		}

		private Light CreateLight( Scene _scene, Color _color )
		{
			var go = new GameObject("SpikeLight");
			go.transform.rotation = Quaternion.LookRotation(Vector3.forward);   // shines along the camera's view
			var light = go.AddComponent<Light>();
			light.type = LightType.Directional;
			light.color = _color;
			light.intensity = 1.5f;
			light.shadows = LightShadows.None;
			SceneManager.MoveGameObjectToScene(go, _scene);
			return light;
		}

		private Camera CreateCamera( Scene _scene, Scene _renderScene, Vector3 _target = default )
		{
			var go = new GameObject("SpikeCamera");
			go.transform.position = _target + new Vector3(0, 0, -3);
			var camera = go.AddComponent<Camera>();
			camera.enabled = false;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Color.clear;
			camera.fieldOfView = 30;
			camera.targetTexture = m_target;
			camera.scene = _renderScene;
			SceneManager.MoveGameObjectToScene(go, _scene);
			return camera;
		}

		private Color RenderCenter( Camera _camera )
		{
			Render(_camera);
			return m_readback.GetPixel(Size / 2, Size / 2);
		}

		private Color[] RenderAll( Camera _camera )
		{
			Render(_camera);
			return m_readback.GetPixels();
		}

		private void Render( Camera _camera )
		{
			_camera.Render();
			var previous = RenderTexture.active;
			RenderTexture.active = m_target;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
		}

		private static void Log( string _what, Color _color ) =>
			Debug.Log($"SPIKE {_what}: r={_color.r:F3} g={_color.g:F3} b={_color.b:F3} a={_color.a:F3}");
	}
}
