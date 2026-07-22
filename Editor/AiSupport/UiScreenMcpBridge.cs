using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Minimal "Mini-DAL" bridge for the AI screen-authoring MCP. Runs a loopback-only
	/// <see cref="HttpListener"/> inside the Unity Editor and answers a tiny JSON protocol
	/// (<c>{"method":"..."}</c>) used by the Node MCP proxy (mcp/server.mjs).
	///
	/// Requests are accepted on background threads but every handler runs on the Editor main
	/// thread (AssetDatabase / the catalog generator are main-thread only), marshalled via
	/// <see cref="EditorApplication.update"/>.
	///
	/// Deliberately tiny: methods are ping / getCatalog / regenerateCatalog / bakeScreen /
	/// screenshotView. Methods that need input carry it in the envelope's <c>payload</c> string
	/// (raw JSON the handler parses itself).
	/// </summary>
	[InitializeOnLoad]
	public static class UiScreenMcpBridge
	{
		public const int Port = 17632;
		private const string UrlPrefix = "http://127.0.0.1:17632/";
		private const string EnabledPrefKey = "GuiToolkit.AiSupport.McpBridge.Enabled";
		private const int HandlerTimeoutMs = 20000;

		private static HttpListener s_listener;
		private static Thread s_acceptThread;
		private static volatile bool s_running;
		private static readonly ConcurrentQueue<Action> s_mainThreadQueue = new();

		public static bool IsRunning => s_running;

		static UiScreenMcpBridge()
		{
			// Restart across domain reloads if the user had it enabled.
			EditorApplication.delayCall += () =>
			{
				if (EditorPrefs.GetBool(EnabledPrefKey, false) && !s_running)
					Start();
			};
			AssemblyReloadEvents.beforeAssemblyReload += StopInternal;
			EditorApplication.quitting += StopInternal;
		}

		#region Menu

		[MenuItem(StringConstants.AI_MCP_BRIDGE_START_MENU_NAME)]
		private static void StartMenu()
		{
			EditorPrefs.SetBool(EnabledPrefKey, true);
			Start();
		}

		[MenuItem(StringConstants.AI_MCP_BRIDGE_START_MENU_NAME, true)]
		private static bool StartMenuValidate() => !s_running;

		[MenuItem(StringConstants.AI_MCP_BRIDGE_STOP_MENU_NAME)]
		private static void StopMenu()
		{
			EditorPrefs.SetBool(EnabledPrefKey, false);
			StopInternal();
		}

		[MenuItem(StringConstants.AI_MCP_BRIDGE_STOP_MENU_NAME, true)]
		private static bool StopMenuValidate() => s_running;

		#endregion

		#region Lifecycle

		public static void Start()
		{
			if (s_running)
				return;

			try
			{
				s_listener = new HttpListener();
				s_listener.Prefixes.Add(UrlPrefix);
				s_listener.Start();
			}
			catch (Exception e)
			{
				UiLog.LogError($"MCP bridge could not start on {UrlPrefix}: {e.Message}");
				s_listener = null;
				return;
			}

			s_running = true;
			EditorApplication.update += Pump;

			s_acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "UiScreenMcpBridge" };
			s_acceptThread.Start();

			UiLog.LogInternal($"MCP bridge listening on {UrlPrefix}");
		}

		private static void StopInternal()
		{
			if (!s_running)
				return;

			s_running = false;
			EditorApplication.update -= Pump;

			try { s_listener?.Stop(); } catch { /* ignore */ }
			try { s_listener?.Close(); } catch { /* ignore */ }
			s_listener = null;

			// Drain any pending jobs so blocked request threads unblock.
			while (s_mainThreadQueue.TryDequeue(out var job))
			{
				try { job(); } catch { /* ignore */ }
			}
		}

		#endregion

		#region Threading

		private static void Pump()
		{
			while (s_mainThreadQueue.TryDequeue(out var job))
			{
				try { job(); } catch (Exception e) { UiLog.LogError($"MCP bridge job failed: {e.Message}"); }
			}
		}

		private static void AcceptLoop()
		{
			while (s_running)
			{
				HttpListenerContext ctx;
				try
				{
					ctx = s_listener.GetContext();
				}
				catch
				{
					break; // listener stopped
				}

				ThreadPool.QueueUserWorkItem(_ => Process(ctx));
			}
		}

		private static void Process( HttpListenerContext _ctx )
		{
			string body;
			using (var reader = new StreamReader(_ctx.Request.InputStream, _ctx.Request.ContentEncoding ?? Encoding.UTF8))
				body = reader.ReadToEnd();

			int status = 200;
			string response;

			try
			{
				var envelope = ParseEnvelope(body);
				response = RunOnMainThread(envelope.method, envelope.payload);
			}
			catch (Exception e)
			{
				status = 500;
				response = "{\"error\":" + JsonString(e.Message) + "}";
			}

			WriteResponse(_ctx, status, response);
		}

		// Runs the handler on the main thread and blocks the request thread for the result.
		private static string RunOnMainThread( string _method, string _payload )
		{
			string result = null;
			Exception error = null;
			using var done = new ManualResetEventSlim(false);

			s_mainThreadQueue.Enqueue(() =>
			{
				try { result = Handle(_method, _payload); }
				catch (Exception e) { error = e; }
				finally { done.Set(); }
			});

			if (!done.Wait(HandlerTimeoutMs))
				throw new TimeoutException("Editor did not process the request in time (is it compiling or unfocused?).");

			if (error != null)
				throw error;

			return result;
		}

		#endregion

		#region Handlers

		private static string Handle( string _method, string _payload )
		{
			switch (_method)
			{
				case "ping":
					return "{\"unity\":true,\"toolkit\":" + JsonString(typeof(UiThing).Assembly.GetName().Name) + "}";

				case "getCatalog":
					return ReadCatalogOrThrow();

				case "regenerateCatalog":
					string path = UiScreenCatalogGenerator.Generate();
					if (string.IsNullOrEmpty(path))
						throw new Exception("Catalog generation failed — see the Unity console.");
					return ReadCatalogOrThrow();

				case "bakeScreen":
					if (string.IsNullOrWhiteSpace(_payload))
						throw new Exception("bakeScreen requires a 'payload' holding the screen JSON.");
					string bakedPath = UiScreenBaker.Bake(_payload);
					return "{\"path\":" + JsonString(bakedPath) + "}";

				case "screenshotView":
					return Screenshot(_payload);

				default:
					throw new Exception($"Unknown method '{_method}'.");
			}
		}

		private static string ReadCatalogOrThrow()
		{
			string path = UiScreenCatalogGenerator.CatalogPath;
			if (!File.Exists(path))
				throw new FileNotFoundException($"Catalog not found at '{path}'. Run regenerateCatalog first.");
			return File.ReadAllText(path);
		}

		[Serializable]
		private class ScreenshotArgs { public string path; public int width; public int height; }

		private static string Screenshot( string _payload )
		{
			if (string.IsNullOrWhiteSpace(_payload))
				throw new Exception("screenshotView requires a 'payload' with at least a prefab path.");

			var args = JsonUtility.FromJson<ScreenshotArgs>(_payload);
			if (args == null || string.IsNullOrEmpty(args.path))
				throw new Exception("screenshotView payload must contain a 'path' to the baked prefab.");

			int width = args.width > 0 ? args.width : UiScreenPreview.DefaultWidth;
			int height = args.height > 0 ? args.height : UiScreenPreview.DefaultHeight;

			string base64 = UiScreenPreview.CaptureBase64(args.path, width, height);
			return "{\"png\":" + JsonString(base64) + ",\"width\":" + width + ",\"height\":" + height + "}";
		}

		#endregion

		#region Helpers

		[Serializable]
		private class MethodEnvelope { public string method; public string payload; }

		private static MethodEnvelope ParseEnvelope( string _body )
		{
			if (string.IsNullOrWhiteSpace(_body))
				throw new Exception("Empty request body; expected {\"method\":\"...\"}.");

			var envelope = JsonUtility.FromJson<MethodEnvelope>(_body);
			if (envelope == null || string.IsNullOrEmpty(envelope.method))
				throw new Exception("Missing 'method' in request body.");

			return envelope;
		}

		private static void WriteResponse( HttpListenerContext _ctx, int _status, string _body )
		{
			try
			{
				byte[] buffer = Encoding.UTF8.GetBytes(_body ?? "");
				_ctx.Response.StatusCode = _status;
				_ctx.Response.ContentType = "application/json; charset=utf-8";
				_ctx.Response.ContentLength64 = buffer.Length;
				_ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
			}
			catch { /* client gone */ }
			finally
			{
				try { _ctx.Response.OutputStream.Close(); } catch { /* ignore */ }
			}
		}

		private static string JsonString( string _value )
		{
			var sb = new StringBuilder(_value.Length + 2);
			sb.Append('"');
			foreach (char c in _value)
			{
				switch (c)
				{
					case '"': sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': sb.Append("\\r"); break;
					case '\t': sb.Append("\\t"); break;
					default:
						if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
						else sb.Append(c);
						break;
				}
			}
			sb.Append('"');
			return sb.ToString();
		}

		#endregion
	}
}
