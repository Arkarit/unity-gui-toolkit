#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit.Test
{
	public sealed class TestEditorCodeUtility_Blockers
	{
		[SetUp]
		public void SetUp()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}

		[Test]
		public void ReplaceUITextWithTMP_RestoresGraphicBlockers()
		{
			var go = new GameObject("A", typeof(RectTransform));

			var txt = go.AddComponent<Text>();
			txt.text = "x";

			var outline = go.AddComponent<Outline>();
			outline.effectColor = new Color(0.25f, 0.5f, 0.75f, 1f);
			outline.effectDistance = new Vector2(3f, -2f);
			outline.useGraphicAlpha = false;

			var shadow = go.AddComponent<Shadow>();
			shadow.effectColor = new Color(0.9f, 0.1f, 0.2f, 0.8f);
			shadow.effectDistance = new Vector2(-4f, 6f);
			shadow.useGraphicAlpha = true;

			// Sanity before
			Assert.That(go.GetComponent<Text>(), Is.Not.Null);
			Assert.That(go.GetComponent<Outline>(), Is.Not.Null);
			Assert.That(go.GetComponent<Shadow>(), Is.Not.Null);

			var results = GuiToolkit.Editor.EditorCodeUtility.ReplaceUITextWithTMPInActiveScene();
			Assert.That(results, Is.Not.Null);
			Assert.That(results.Count, Is.EqualTo(1));

			// Text is gone, TMP is present
			Assert.That(go.GetComponent<Text>(), Is.Null);
			var tmp = go.GetComponent<TextMeshProUGUI>();
			Assert.That(tmp, Is.Not.Null);

			// Blockers are restored
			var outlineAfter = go.GetComponent<Outline>();
			var shadows = go.GetComponents<Shadow>();

			Shadow shadowAfter = null;
			for (int i = 0; i < shadows.Length; i++)
			{
				if (shadows[i] != null && shadows[i].GetType() == typeof(Shadow))
				{
					shadowAfter = shadows[i];
					break;
				}
			}

			Assert.That(outlineAfter, Is.Not.Null);
			Assert.That(shadowAfter, Is.Not.Null);

			// Values preserved (this verifies JSON snapshot restore works)
			Assert.That(outlineAfter.effectColor, Is.EqualTo(new Color(0.25f, 0.5f, 0.75f, 1f)));
			Assert.That(outlineAfter.effectDistance, Is.EqualTo(new Vector2(3f, -2f)));
			Assert.That(outlineAfter.useGraphicAlpha, Is.False);

			Assert.That(shadowAfter.effectColor, Is.EqualTo(new Color(0.9f, 0.1f, 0.2f, 0.8f)));
			Assert.That(shadowAfter.effectDistance, Is.EqualTo(new Vector2(-4f, 6f)));
			Assert.That(shadowAfter.useGraphicAlpha, Is.True);
		}
	}
}
#endif
