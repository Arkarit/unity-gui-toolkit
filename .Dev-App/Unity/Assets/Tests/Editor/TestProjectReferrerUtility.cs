#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for ProjectReferrerUtility integration with CaptureComponentUtility.
	/// Verifies that references to blocking components are preserved across removal/restoration.
	/// </summary>
	public sealed class TestProjectReferrerUtility
	{
		[SetUp]
		public void SetUp()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}

		/// <summary>
		/// Forces Unity to serialize all components in the scene.
		/// This is critical for tests that rely on SerializedObject API.
		/// </summary>
		private void ForceSerializeScene()
		{
			var scene = SceneManager.GetActiveScene();
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene, "Temp/TestScene.unity");
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

		[Test]
		public void CollectReferrersInProject_FindsReferencesInScene()
		{
			var targetGo = new GameObject("Target");
			var targetComp = targetGo.AddComponent<Outline>();

			var referrerGo = new GameObject("Referrer");
			var holder = referrerGo.AddComponent<TestReferenceHolder>();
			holder.OutlineRef = targetComp;

			// CRITICAL: Force serialization before scanning
			ForceSerializeScene();

			var referrers = Editor.ProjectReferrerUtility.CollectReferrersInCurrentContext(targetComp);

			Assert.That(referrers, Is.Not.Null);
			Assert.That(referrers.Count, Is.EqualTo(1), "Should find exactly one reference");
			Assert.That(referrers[0].owner, Is.SameAs(holder));
			Assert.That(referrers[0].propertyPath, Is.EqualTo("m_outlineRef"));
		}

		[Test]
		public void RewireReferrers_UpdatesReferencesToNewComponent()
		{
			var oldGo = new GameObject("Old");
			var oldComp = oldGo.AddComponent<Outline>();

			var referrerGo = new GameObject("Referrer");
			var holder = referrerGo.AddComponent<TestReferenceHolder>();
			holder.OutlineRef = oldComp;

			// Collect references
			var referrers = new List<(UnityEngine.Object owner, string propertyPath)>
			{
				(holder, "m_outlineRef")
			};

			// Create new component
			var newGo = new GameObject("New");
			var newComp = newGo.AddComponent<Outline>();

			// Rewire
			Editor.ProjectReferrerUtility.RewireReferrers(referrers, newComp);

			Assert.That(holder.OutlineRef, Is.SameAs(newComp));
		}

		[Test]
		public void CaptureAndRemoveBlockers_PreservesReferences_WhenScanProjectEnabled()
		{
			var go = new GameObject("A", typeof(RectTransform));
			var txt = go.AddComponent<Text>();
			var outline = go.AddComponent<Outline>();

			// Create a referrer component that points to the outline
			var referrerGo = new GameObject("Referrer");
			var holder = referrerGo.AddComponent<TestReferenceHolder>();
			holder.OutlineRef = outline;

			// CRITICAL: Force serialization before capturing blockers
			ForceSerializeScene();

			// Verify setup
			Assert.That(holder.OutlineRef, Is.SameAs(outline));

			// Capture and remove blockers WITH reference preservation
			var blockers = Editor.CaptureComponentUtility.CaptureAndRemoveBlockers(
				go, 
				txt, 
				_replacementType: null, 
				_preserveReferences: true,
				_scanEntireProject: false
			);

			// Outline should be removed
			Assert.That(go.GetComponent<Outline>(), Is.Null);
			Assert.That(blockers.Count, Is.EqualTo(1));

			// Reference should now be null (component was destroyed)
			Assert.That(holder.OutlineRef == null, Is.True);

			// Restore blockers
			Editor.CaptureComponentUtility.RestoreBlockers(go, blockers);

			// Outline should be restored
			var restoredOutline = go.GetComponent<Outline>();
			Assert.That(restoredOutline, Is.Not.Null);

			// Reference should be rewired to the restored component
			Assert.That(holder.OutlineRef == null, Is.False);
			Assert.That(holder.OutlineRef, Is.SameAs(restoredOutline));
		}

		[Test]
		public void CaptureAndRemoveBlockers_WithoutProjectScan_DoesNotPreserveExternalReferences()
		{
			var go = new GameObject("A", typeof(RectTransform));
			var txt = go.AddComponent<Text>();
			var outline = go.AddComponent<Outline>();

			// Create a referrer component that points to the outline
			var referrerGo = new GameObject("Referrer");
			var holder = referrerGo.AddComponent<TestReferenceHolder>();
			holder.OutlineRef = outline;

			// CRITICAL: Force serialization
			ForceSerializeScene();

			// Verify setup
			Assert.That(holder.OutlineRef, Is.SameAs(outline));

			// Capture and remove blockers WITHOUT reference preservation
			var blockers = Editor.CaptureComponentUtility.CaptureAndRemoveBlockers(
				go, 
				txt, 
				_replacementType: null, 
				_preserveReferences: false
			);

			// Outline should be removed
			Assert.That(go.GetComponent<Outline>(), Is.Null);
			Assert.That(blockers.Count, Is.EqualTo(1));

			// Restore blockers
			Editor.CaptureComponentUtility.RestoreBlockers(go, blockers);

			// Outline should be restored
			var restoredOutline = go.GetComponent<Outline>();
			Assert.That(restoredOutline, Is.Not.Null);

			// Reference should still be null (no reference preservation was done)
			Assert.That(holder.OutlineRef == null, Is.True);
		}

		[Test]
		public void CaptureAndRemoveBlockers_PreservesMultipleReferences()
		{
			var go = new GameObject("A", typeof(RectTransform));
			var txt = go.AddComponent<Text>();
			var outline = go.AddComponent<Outline>();
			outline.effectColor = Color.red;

			// Create multiple referrers
			var referrer1Go = new GameObject("Referrer1");
			var holder1 = referrer1Go.AddComponent<TestReferenceHolder>();
			holder1.OutlineRef = outline;

			var referrer2Go = new GameObject("Referrer2");
			var holder2 = referrer2Go.AddComponent<TestReferenceHolder>();
			holder2.OutlineRef = outline;

			// CRITICAL: Force serialization before capturing
			ForceSerializeScene();

			// Verify setup
			Assert.That(holder1.OutlineRef, Is.SameAs(outline));
			Assert.That(holder2.OutlineRef, Is.SameAs(outline));

			// Capture and remove blockers
			var blockers = Editor.CaptureComponentUtility.CaptureAndRemoveBlockers(
				go, 
				txt, 
				_replacementType: null, 
				_preserveReferences: true,
				_scanEntireProject: false
			);

			Assert.That(go.GetComponent<Outline>(), Is.Null);

			// Restore blockers
			Editor.CaptureComponentUtility.RestoreBlockers(go, blockers);

			var restoredOutline = go.GetComponent<Outline>();
			Assert.That(restoredOutline, Is.Not.Null);

			// Both references should be rewired
			Assert.That(holder1.OutlineRef, Is.SameAs(restoredOutline));
			Assert.That(holder2.OutlineRef, Is.SameAs(restoredOutline));

			// Values should be preserved
			Assert.That(restoredOutline.effectColor, Is.EqualTo(Color.red));
		}

		[Test]
		public void CaptureAndRemoveBlockers_PreservesReferences_InChainedDependencies()
		{
			// Setup: Component A requires Component B, Component B requires Graphic (Text)
			var go = new GameObject("A", typeof(RectTransform));
			var txt = go.AddComponent<Text>();
			var shadow = go.AddComponent<Shadow>(); // Shadow requires Graphic
			var outline = go.AddComponent<Outline>(); // Outline requires Graphic

			// Create referrers to both blockers
			var referrerGo = new GameObject("Referrer");
			var holder = referrerGo.AddComponent<TestReferenceHolder>();
			holder.OutlineRef = outline;
			holder.ShadowRef = shadow;

			// CRITICAL: Force serialization
			ForceSerializeScene();

			// Verify setup
			Assert.That(holder.OutlineRef, Is.SameAs(outline));
			Assert.That(holder.ShadowRef, Is.SameAs(shadow));

			// Capture and remove blockers
			var blockers = Editor.CaptureComponentUtility.CaptureAndRemoveBlockers(
				go, 
				txt, 
				_replacementType: null, 
				_preserveReferences: true,
				_scanEntireProject: false
			);

			// Both blockers should be removed
			Assert.That(go.GetComponent<Outline>(), Is.Null);
			Assert.That(go.GetComponent<Shadow>(), Is.Null);
			Assert.That(blockers.Count, Is.EqualTo(2));

			// Restore blockers
			Editor.CaptureComponentUtility.RestoreBlockers(go, blockers);

			var restoredOutline = SafeGetComponent<Outline>(go);
			var restoredShadow = SafeGetComponent<Shadow>(go);

			Assert.That(restoredOutline, Is.Not.Null);
			Assert.That(restoredShadow, Is.Not.Null);

			// Both references should be rewired
			Assert.That(holder.OutlineRef, Is.SameAs(restoredOutline));
			Assert.That(holder.ShadowRef, Is.SameAs(restoredShadow));
		}

		private T SafeGetComponent<T>(GameObject _go) where T : Component
		{
			var components = _go.GetComponents<T>();
			foreach (var component in components)
			{
				if (component.GetType() == typeof(T))
					return (T)component;
			}

			return null;
		}

		[Test]
		public void RewireReferrers_HandlesNullOwners()
		{
			var newComp = new GameObject("New").AddComponent<Outline>();

			var referrers = new List<(UnityEngine.Object owner, string propertyPath)>
			{
				(null, "somePath")
			};

			// Should not throw
			Assert.DoesNotThrow(() => 
			{
				Editor.ProjectReferrerUtility.RewireReferrers(referrers, newComp);
			});
		}

		[Test]
		public void RewireReferrers_HandlesInvalidPropertyPaths()
		{
			var holder = new GameObject("Holder").AddComponent<TestReferenceHolder>();
			var newComp = new GameObject("New").AddComponent<Outline>();

			var referrers = new List<(UnityEngine.Object owner, string propertyPath)>
			{
				(holder, "nonExistentProperty")
			};

			// Should not throw
			Assert.DoesNotThrow(() => 
			{
				Editor.ProjectReferrerUtility.RewireReferrers(referrers, newComp);
			});

			// Reference should remain null
			Assert.That(holder.OutlineRef, Is.Null);
		}

		/// <summary>
		/// Test helper component that holds references to other components.
		/// </summary>
		private sealed class TestReferenceHolder : MonoBehaviour
		{
			[SerializeField] private Outline m_outlineRef;
			[SerializeField] private Shadow m_shadowRef;

			public Outline OutlineRef
			{
				get => m_outlineRef;
				set => m_outlineRef = value;
			}

			public Shadow ShadowRef
			{
				get => m_shadowRef;
				set => m_shadowRef = value;
			}
		}
	}
}
#endif