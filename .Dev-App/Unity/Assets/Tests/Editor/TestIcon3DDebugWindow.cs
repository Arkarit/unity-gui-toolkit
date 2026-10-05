using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestIcon3DDebugWindow
	{
		[SetUp]
		public void SetUp()
		{
			UiIcon3DRenderer.Layer = 31;
			UiIcon3DRenderer.MsaaSamples = 1;
		}

		[TearDown]
		public void TearDown()
		{
			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
		}

		[Test]
		public void The_Report_Has_A_Summary_And_One_Line_Per_Icon()
		{
			var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "ReportSphere";
			sphere.SetActive(false);
			var handle = UiIcon3DRenderer.RenderStatic(sphere, null, new Vector2Int(32, 32));
			try
			{
				UiIcon3DRenderer.Flush();
				string report = Icon3DDebugWindow.BuildReport();

				StringAssert.Contains("3D icons: 1 requests", report);
				StringAssert.Contains("ReportSphere", report);
				StringAssert.Contains("rendered", report);
			}
			finally
			{
				handle.Release();
				Object.DestroyImmediate(sphere);
			}
		}

		[Test]
		public void The_Report_For_An_Empty_Renderer_Still_Has_A_Summary()
		{
			string report = Icon3DDebugWindow.BuildReport();
			StringAssert.Contains("3D icons: 0 requests", report);
		}
	}
}
