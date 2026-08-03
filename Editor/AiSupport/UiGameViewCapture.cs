using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Captures the Game View while the editor is playing — the running app with its real data, real resolution
	/// and real state, rather than a prefab rendered in isolation.
	///
	/// This is the one thing the Edit-Mode tools cannot substitute. A prefab preview shows what was authored; a
	/// filmstrip shows how it moves. Neither shows a screen filled by the server, laid out at the device
	/// resolution, after a user got there by tapping through the app.
	///
	/// Uses <see cref="ScreenCapture.CaptureScreenshot(string)"/> and waits for the file rather than reading
	/// pixels directly: the pixel path has to run after the frame has rendered, which means a coroutine and a
	/// frame of latency to arrange from editor code. Unity already solves that internally for the file path.
	/// </summary>
	public static class UiGameViewCapture
	{
		private const string CaptureDir = "Library/UiToolkit/GameViewCaptures";

		private static string s_pendingPath;
		private static int s_pendingSuperSize;
		private static long s_lastSize = -1;
		private static int s_stableChecks;

		/// <summary>
		/// Started and collected in SEPARATE requests, which is not ceremony — it is the only thing that can work.
		/// Handlers run on the editor's main thread, and the capture only completes once a frame has RENDERED. A
		/// handler that waits for the file therefore blocks the very thread that would produce it: the first
		/// version deadlocked itself and timed out every time, blaming the Game View.
		/// </summary>
		public static JObject Capture( string _payload )
		{
			var request = string.IsNullOrWhiteSpace(_payload) ? new JObject() : JObject.Parse(_payload);
			string action = ((string)request["action"] ?? "start").ToLowerInvariant();

			return action == "fetch" ? Fetch() : Start(request);
		}

		private static JObject Start( JObject _request )
		{
			if (!EditorApplication.isPlaying)
			{
				throw new Exception(
					"Not in Play Mode, so there is no Game View to capture. Start it with playMode/enter (or ask " +
					"the human to bring the app to the state you want to see), and use screenshot_view for a " +
					"prefab in Edit Mode.");
			}

			// A paused editor renders no further frame, so the capture could never complete. Say what is actually
			// wrong rather than letting the caller time out against a misleading message.
			if (EditorApplication.isPaused)
			{
				throw new Exception(
					"Play Mode is paused, so no further frame will be rendered and the capture cannot complete. " +
					"Resume, or step a frame, and try again.");
			}

			s_pendingSuperSize = Mathf.Clamp((int?)_request["superSize"] ?? 1, 1, 4);

			string directory = Path.GetFullPath(CaptureDir);
			Directory.CreateDirectory(directory);
			s_pendingPath = Path.Combine(directory, $"gameview-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.png");
			s_lastSize = -1;
			s_stableChecks = 0;

			// Stale leftovers would be indistinguishable from a fresh capture.
			if (File.Exists(s_pendingPath))
				File.Delete(s_pendingPath);

			ScreenCapture.CaptureScreenshot(s_pendingPath, s_pendingSuperSize);

			return new JObject
			{
				["pending"] = true,
				["file"] = s_pendingPath.Replace('\\', '/'),
			};
		}

		private static JObject Fetch()
		{
			if (string.IsNullOrEmpty(s_pendingPath))
				throw new Exception("No capture was started. Call with action 'start' first.");

			if (!File.Exists(s_pendingPath))
				return new JObject { ["pending"] = true };

			// The file appears before it is fully written, so wait for its size to settle rather than returning
			// a half-written PNG.
			long size = new FileInfo(s_pendingPath).Length;
			if (size <= 0 || size != s_lastSize)
			{
				s_lastSize = size;
				s_stableChecks = 0;
				return new JObject { ["pending"] = true };
			}

			if (++s_stableChecks < 2)
				return new JObject { ["pending"] = true };

			string path = s_pendingPath;
			s_pendingPath = null;
			return Result(path, s_pendingSuperSize);
		}

		private static JObject Result( string _path, int _superSize )
		{
			byte[] png = File.ReadAllBytes(_path);
			var size = SizeFromPngHeader(png);

			// Kept on disk as well: the file is handy for a human to look at, and it costs nothing under Library.
			return new JObject
			{
				["png"] = Convert.ToBase64String(png),
				["file"] = _path.Replace('\\', '/'),
				["byteSize"] = png.Length,
				["width"] = size.x,
				["height"] = size.y,
				["superSize"] = _superSize,
				["timeScale"] = Time.timeScale,
			};
		}

		/// <summary>
		/// Read from the PNG header rather than from Screen.width/height: those report the Game View's current
		/// size, while the image is what superSize actually produced — and a caller reading pixel coordinates off
		/// the image needs the image's own dimensions to hand them back to probe_ui.
		/// </summary>
		private static Vector2Int SizeFromPngHeader( byte[] _png )
		{
			// IHDR width/height are big-endian 32-bit values at offsets 16 and 20 of every PNG.
			if (_png.Length < 24)
				return new Vector2Int(0, 0);

			int width = (_png[16] << 24) | (_png[17] << 16) | (_png[18] << 8) | _png[19];
			int height = (_png[20] << 24) | (_png[21] << 16) | (_png[22] << 8) | _png[23];
			return new Vector2Int(width, height);
		}
	}
}
