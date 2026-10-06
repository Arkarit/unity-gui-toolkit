using System.Collections.Generic;
using System.Linq;
using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The preset studio window: one render request per object x preset cell, rendered by the shared renderer, and
	/// every request released again when the window goes. The drawing itself is looked at, not tested.
	/// </summary>
	[EditorAware]
	public class TestIcon3DPresetStudio
	{
		private readonly List<Object> m_created = new();
		private int m_requestsBefore;

		[SetUp]
		public void SetUp()
		{
			Icon3DTestModels.EnsureAssets();
			UiIcon3DRenderer.Layer = 31;
			UiIcon3DRenderer.MsaaSamples = 1;
			m_requestsBefore = UiIcon3DRenderer.RequestCount;
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_created)
				if (obj != null)
					Object.DestroyImmediate(obj);
			m_created.Clear();

			UiIcon3DRenderer.Shutdown();
			UiIcon3DRenderer.Layer = -1;
			UiIcon3DRenderer.MsaaSamples = 0;
		}

		private Icon3DPresetStudio CreateStudio( int _objects, int _presets )
		{
			var studio = ScriptableObject.CreateInstance<Icon3DPresetStudio>();
			m_created.Add(studio);

			var objects = new[] { Icon3DTestModels.TrophyPath, Icon3DTestModels.GobletPath, Icon3DTestModels.PuppetPath }
				.Take(_objects).Select(AssetDatabase.LoadAssetAtPath<GameObject>);
			var presets = new[] { "Icon3DPreset_Neutral", "Icon3DPreset_Warm", "Icon3DPreset_Dramatic" }
				.Take(_presets).Select(LoadPreset);
			studio.SetContent(objects, presets);
			return studio;
		}

		private static UiIcon3DPreset LoadPreset( string _name )
		{
			var guid = AssetDatabase.FindAssets(_name + " t:Prefab").First();
			return AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)).GetComponent<UiIcon3DPreset>();
		}

		[Test]
		public void Every_Object_Preset_Pair_Gets_One_Cell_That_Renders()
		{
			var studio = CreateStudio(3, 3);
			studio.Rebuild();

			Assert.AreEqual(9, studio.CellCount);
			UiIcon3DRenderer.Flush();
			Assert.AreEqual(9, studio.RenderedCount);
		}

		[Test]
		public void Closing_The_Window_Releases_Every_Request()
		{
			var studio = CreateStudio(2, 3);
			studio.Rebuild();
			Assert.AreEqual(m_requestsBefore + 6, UiIcon3DRenderer.RequestCount);

			Object.DestroyImmediate(studio);
			m_created.Remove(studio);

			Assert.AreEqual(m_requestsBefore, UiIcon3DRenderer.RequestCount, "the studio leaked render requests");
		}

		[Test]
		public void Rebuilding_Does_Not_Pile_Up_Requests()
		{
			var studio = CreateStudio(2, 2);
			studio.Rebuild();
			studio.Rebuild();
			studio.Rebuild();

			Assert.AreEqual(m_requestsBefore + 4, UiIcon3DRenderer.RequestCount);
		}

		[Test]
		public void Missing_And_Duplicate_Entries_Are_Ignored()
		{
			var studio = ScriptableObject.CreateInstance<Icon3DPresetStudio>();
			m_created.Add(studio);
			var trophy = AssetDatabase.LoadAssetAtPath<GameObject>(Icon3DTestModels.TrophyPath);
			var warm = LoadPreset("Icon3DPreset_Warm");

			studio.SetContent(new GameObject[] { trophy, null, trophy }, new[] { warm, null, warm });

			Assert.AreEqual(1, studio.Objects.Count);
			Assert.AreEqual(1, studio.Presets.Count);
		}

		[Test]
		public void An_Empty_Studio_Costs_Nothing()
		{
			var studio = ScriptableObject.CreateInstance<Icon3DPresetStudio>();
			m_created.Add(studio);
			studio.SetContent(new GameObject[0], new UiIcon3DPreset[0]);
			studio.Rebuild();

			Assert.AreEqual(0, studio.CellCount);
			Assert.AreEqual(m_requestsBefore, UiIcon3DRenderer.RequestCount);
		}

		[Test]
		public void The_Preset_Order_Is_Kept()
		{
			var studio = CreateStudio(1, 3);
			Assert.AreEqual(new[] { "Icon3DPreset_Neutral", "Icon3DPreset_Warm", "Icon3DPreset_Dramatic" }, studio.Presets.Select(_p => _p.name).ToArray());
		}
	}
}
