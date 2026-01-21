using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class GameScreenshotTool
	{
		private const int kJpgQuality = 85;

		[MenuItem(StringConstants.SCREENSHOT_TO_DESKTOP)]
		public static void CaptureGameScreenshot()
		{
			if (!Application.isPlaying)
			{
				EditorUtility.DisplayDialog(
					"Capture Game Screenshot",
					"Enter Play Mode first so the Game View has a defined resolution.",
					"OK");
				return;
			}

			string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			string project = Sanitize(Application.productName);
			string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
			string path = Path.Combine(desktop, $"screenshot_{project}_{timestamp}.jpg");

			var go = new GameObject("~ScreenshotRunner");
			go.hideFlags = HideFlags.HideAndDontSave;
			var runner = go.AddComponent<ScreenshotRunner>();
			runner.StartCapture(path, kJpgQuality);
		}

		private static string Sanitize( string _name )
		{
			if (string.IsNullOrEmpty(_name)) return "project";
			foreach (char c in Path.GetInvalidFileNameChars())
				_name = _name.Replace(c.ToString(), "_");
			return _name.ToLowerInvariant();
		}

		private class ScreenshotRunner : MonoBehaviour
		{
			private string m_Path;
			private int m_Quality;

			public void StartCapture( string _path, int _quality )
			{
				m_Path = _path;
				m_Quality = _quality;
				StartCoroutine(CaptureRoutine());
			}

			private IEnumerator CaptureRoutine()
			{
				// Give the Game View a moment to settle (resolution changes, layout, etc.)
				yield return new WaitForEndOfFrame();
				yield return null; // one extra frame helps with RT sizing on some pipelines
				yield return new WaitForEndOfFrame();

				Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
				if (tex == null)
				{
					// Retry once next frame if RT was not ready yet
					yield return null;
					yield return new WaitForEndOfFrame();
					tex = ScreenCapture.CaptureScreenshotAsTexture();
				}

				if (tex == null)
				{
					Debug.LogError("GameScreenshotTool: Capture failed (texture null).");
					Destroy(gameObject);
					yield break;
				}

				try
				{
					byte[] jpg = tex.EncodeToJPG(m_Quality);
					File.WriteAllBytes(m_Path, jpg);
//					EditorUtility.RevealInFinder(m_Path);
//					EditorUtility.DisplayDialog("Capture Game Screenshot", $"Saved:\n{m_Path}", "OK");
					UiLog.LogInternal($"Saved Screenshot:\n{m_Path}");
				}
				catch (Exception ex)
				{
					Debug.LogError("GameScreenshotTool: Failed to save screenshot.\n" + ex);
					EditorUtility.DisplayDialog("Capture Game Screenshot", "Failed to save screenshot.", "OK");
				}
				finally
				{
					Destroy(tex);
					Destroy(gameObject);
				}
			}
		}
	}
}
