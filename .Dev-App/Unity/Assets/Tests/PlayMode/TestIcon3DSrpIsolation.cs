using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuiToolkit.Test
{
	/// <summary>
	/// A hostile scene must not change an icon, in any render pipeline: the same icon is rendered in an empty scene and
	/// with the scene full of lights, fog and ambient, and the two images must be the same. The Built-in oracle
	/// (TestIcon3DRenderer) is stricter but swaps Built-in's RenderSettings; this one only needs what every pipeline has.
	/// </summary>
	public class TestIcon3DSrpIsolation
	{
		private const int TestLayer = 31;
		private const int Size = 32;

		private readonly List<Object> m_cleanup = new();
		private Texture2D m_readback;
		private AmbientMode m_ambientMode;
		private Color m_ambientLight;
		private bool m_fog;
		private Color m_fogColor;
		private float m_fogDensity;

		[SetUp]
		public void SetUp()
		{
			m_ambientMode = RenderSettings.ambientMode;
			m_ambientLight = RenderSettings.ambientLight;
			m_fog = RenderSettings.fog;
			m_fogColor = RenderSettings.fogColor;
			m_fogDensity = RenderSettings.fogDensity;

			UiIcon3DRenderer.Layer = TestLayer;
			UiIcon3DRenderer.MsaaSamples = 1;
			m_readback = Track(new Texture2D(Size, Size, TextureFormat.RGBA32, false));
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_cleanup)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_cleanup.Clear();

			RenderSettings.ambientMode = m_ambientMode;
			RenderSettings.ambientLight = m_ambientLight;
			RenderSettings.fog = m_fog;
			RenderSettings.fogColor = m_fogColor;
			RenderSettings.fogDensity = m_fogDensity;

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
		}

		[Test]
		public void Hostile_Scene_Lights_Fog_And_Ambient_Do_Not_Change_The_Icon()
		{
			var presetGo = Track(new GameObject("Preset"));
			presetGo.SetActive(false);
			var preset = presetGo.AddComponent<UiIcon3DPreset>();
			var lightGo = new GameObject("Light");
			lightGo.transform.SetParent(presetGo.transform, false);
			lightGo.transform.localRotation = Quaternion.Euler(30, 20, 0);
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1;

			var sphere = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
			sphere.SetActive(false);
			sphere.GetComponent<Renderer>().sharedMaterial = Track(Icon3DTestModels.CreateOpaque("White", Color.white, 0, 0.5f));

			var calm = RenderIcon(sphere, preset, out var handle);

			// Hostile: green sun with every culling mask, a blue point light right at the stage, magenta ambient, dense red fog
			var sun = Track(new GameObject("Hostile Sun"));
			sun.transform.rotation = Quaternion.Euler(60, 0, 0);
			var sunLight = sun.AddComponent<Light>();
			sunLight.type = LightType.Directional;
			sunLight.color = Color.green;
			sunLight.intensity = 3;
			sunLight.cullingMask = ~0;

			var lamp = Track(new GameObject("Hostile Lamp"));
			lamp.transform.position = UiIcon3DRenderer.StagePosition + new Vector3(0, 1, -1);
			var lampLight = lamp.AddComponent<Light>();
			lampLight.type = LightType.Point;
			lampLight.color = Color.blue;
			lampLight.intensity = 5;
			lampLight.range = 50;
			lampLight.cullingMask = ~0;

			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = Color.magenta;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Exponential;
			RenderSettings.fogColor = Color.red;
			RenderSettings.fogDensity = 1;

			handle.SetDirty();
			UiIcon3DRenderer.Flush();
			var hostile = Read(handle.Texture);

			float worst = 0;
			for (int i = 0; i < calm.Length; i++)
				worst = Mathf.Max(worst, Mathf.Abs(calm[i].r - hostile[i].r), Mathf.Abs(calm[i].g - hostile[i].g),
					Mathf.Abs(calm[i].b - hostile[i].b), Mathf.Abs(calm[i].a - hostile[i].a));

			Debug.Log($"ICON3D {Icon3DTestModels.Pipeline}: largest channel difference with a hostile scene {worst:F4}");
			Assert.Less(worst, 0.02f, "the scene's lights, fog or ambient reach the icon");
			Assert.IsTrue(sunLight.enabled && lampLight.enabled, "the scene's lights were not switched back on");
			Assert.AreEqual(Color.magenta, RenderSettings.ambientLight, "the scene's ambient was not restored");
		}

		private Color[] RenderIcon( GameObject _object, UiIcon3DPreset _preset, out Icon3DHandle _handle )
		{
			_handle = UiIcon3DRenderer.RenderStatic(_object, _preset, new Vector2Int(Size, Size));
			UiIcon3DRenderer.Flush();
			Assert.IsTrue(_handle.IsRendered);
			return Read(_handle.Texture);
		}

		private Color[] Read( Texture _texture )
		{
			var previous = RenderTexture.active;
			RenderTexture.active = (RenderTexture)_texture;
			m_readback.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
			m_readback.Apply();
			RenderTexture.active = previous;
			return m_readback.GetPixels();
		}

		private T Track<T>( T _obj ) where T : Object
		{
			m_cleanup.Add(_obj);
			return _obj;
		}
	}
}
