using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Minimal "Mini-DAL" bridge for the AI screen-authoring MCP. Runs a loopback-only
	/// <see cref="HttpListener"/> inside the Unity Editor and answers a tiny JSON protocol
	/// (<c>{"method":"..."}</c>) used by the Node MCP proxy (mcp~/server.mjs).
	///
	/// Requests are accepted on background threads but every handler runs on the Editor main
	/// thread (AssetDatabase / the catalog generator are main-thread only), marshalled via
	/// <see cref="EditorApplication.update"/>.
	///
	/// Deliberately tiny: methods are ping / status / recompile / getCatalog / regenerateCatalog /
	/// bakeScreen / screenshotView / tagStandardElement / untagStandardElement. Methods that need input
	/// carry it in the envelope's <c>payload</c> string (raw JSON the handler parses itself).
	/// </summary>
	[EditorAware]
	[InitializeOnLoad]
	public static class UiScreenMcpBridge
	{
		public const int Port = 17632;
		private const string UrlPrefix = "http://127.0.0.1:17632/";
		private const string EnabledPrefKey = "GuiToolkit.AiSupport.McpBridge.Enabled";
		// Max time to wait for a handler on the main thread. Generous because batch operations
		// (e.g. tagging many prefabs in one call) re-serialize each asset and can take a while;
		// fast handlers (ping/status) still return immediately — this is only an upper bound.
		private const int HandlerTimeoutMs = 300000;

		private static HttpListener s_listener;
		private static Thread s_acceptThread;
		private static volatile bool s_running;
		private static volatile bool s_recompileRequested;
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

			// Handled here (not in the request handler) so the recompile's domain reload happens on a
			// clean editor tick after the HTTP response was already sent — and because this update path
			// keeps ticking while the window is unfocused, which is the whole point of the feature.
			if (s_recompileRequested)
			{
				s_recompileRequested = false;
				try
				{
					AssetDatabase.Refresh();
					CompilationPipeline.RequestScriptCompilation();
				}
				catch (Exception e) { UiLog.LogError($"MCP bridge recompile failed: {e.Message}"); }
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

				case "status":
					return "{\"running\":true,\"compiling\":" + (EditorApplication.isCompiling ? "true" : "false") +
					       ",\"updating\":" + (EditorApplication.isUpdating ? "true" : "false") + "}";

				case "recompile":
					// Trigger the refresh from Pump() (the EditorApplication.update path, which provably
					// ticks even while the window is unfocused) rather than here: doing it inline would
					// let the compilation's domain reload tear down this request's HTTP response, and
					// EditorApplication.delayCall fires unreliably while Unity is in the background.
					// Refresh() imports new/changed scripts; RequestScriptCompilation() forces the rebuild.
					s_recompileRequested = true;
					return "{\"recompiling\":true}";

				case "setupStatus":
					return SetupStatusJson();

				case "getCatalog":
					return CatalogSummaryJson();

				case "regenerateCatalog":
					string path = UiScreenCatalogGenerator.Generate();
					if (string.IsNullOrEmpty(path))
						throw new Exception("Catalog generation failed — see the Unity console.");
					return CatalogSummaryJson();

				case "bakeScreen":
					if (string.IsNullOrWhiteSpace(_payload))
						throw new Exception("bakeScreen requires a 'payload' holding the screen JSON.");
					var bakeResult = UiScreenBaker.Bake(_payload);
					var warnings = new JArray();
					foreach (var w in bakeResult.warnings)
						warnings.Add(w);
					var companions = new JArray();
					foreach (var c in bakeResult.companions)
						companions.Add(c);
					var bakeJson = new JObject { ["path"] = bakeResult.path, ["warnings"] = warnings };
					if (companions.Count > 0)
						bakeJson["companions"] = companions;
					return bakeJson.ToString(Newtonsoft.Json.Formatting.None);

				case "readScreen":
					if (string.IsNullOrWhiteSpace(_payload))
						throw new Exception("readScreen requires a 'payload' holding the prefab path.");
					var readResult = UiScreenReader.Read(ReadScreenPath(_payload), ReadScreenSource(_payload));
					var readWarnings = new JArray();
					foreach (var w in readResult.warnings)
						readWarnings.Add(w);
					return new JObject { ["screen"] = readResult.screen, ["warnings"] = readWarnings }
						.ToString(Newtonsoft.Json.Formatting.None);

				case "resolvePackages":
					// Unity only notices an externally edited manifest.json when the editor regains focus, which
					// left an agent waiting on a human to alt-tab. Resolve() does it directly. It returns as soon
					// as the resolve is triggered — the caller polls "status" for idle, like recompile does.
					UnityEditor.PackageManager.Client.Resolve();
					return new JObject { ["resolving"] = true }.ToString(Newtonsoft.Json.Formatting.None);

				case "getConsole":
					return ReadConsoleQuery(_payload).ToString(Newtonsoft.Json.Formatting.None);

				case "capturePrefabValues":
					if (string.IsNullOrWhiteSpace(_payload))
						throw new Exception("capturePrefabValues requires a 'payload' holding the prefab path.");
					return UiPrefabValueSnapshot.Capture(ReadScreenPath(_payload))
						.ToString(Newtonsoft.Json.Formatting.None);

				case "applyPrefabValues":
					if (string.IsNullOrWhiteSpace(_payload))
						throw new Exception("applyPrefabValues requires a 'payload' holding the prefab path.");
					return ApplyPrefabValues(_payload).ToString(Newtonsoft.Json.Formatting.None);

				case "screenshotView":
					return Screenshot(_payload);

				case "screenshotMotion":
					return MotionFilmstrip(_payload).ToString(Newtonsoft.Json.Formatting.None);

				case "tagStandardElement":
					return TagStandardElement(_payload);

				case "untagStandardElement":
					return UntagStandardElement(_payload);

				case "setUiComment":
					return SetUiComment(_payload);

				default:
					throw new Exception($"Unknown method '{_method}'.");
			}
		}

		/// <summary>
		/// Returns a small JSON envelope describing the catalog file (path + cheap metadata) rather
		/// than its full body. The MCP client runs on the same machine (loopback bridge), so it reads
		/// the file itself with its own file tools — piping ~750 KB of JSON through the tool result
		/// would blow the client's context budget for no reason.
		/// </summary>
		private static string CatalogSummaryJson()
		{
			string relPath = UiScreenCatalogGenerator.CatalogPath;
			if (!File.Exists(relPath))
				throw new FileNotFoundException($"Catalog not found at '{relPath}'. Run regenerateCatalog first.");

			string absPath = Path.GetFullPath(relPath).Replace('\\', '/');
			var fileInfo = new FileInfo(absPath);

			int Count(JObject o, string key) => o[key] is JArray a ? a.Count : 0;

			var catalog = JObject.Parse(File.ReadAllText(relPath));
			var summary = new JObject
			{
				["path"]           = relPath,
				["absolutePath"]   = absPath,
				["version"]        = catalog["version"],
				["generatedAtUtc"] = catalog["generatedAtUtc"],
				["toolkitAssembly"] = catalog["toolkitAssembly"],
				["byteSize"]       = fileInfo.Length,
				["counts"] = new JObject
				{
					["components"]  = Count(catalog, "components"),
					["palette"]     = Count(catalog, "palette"),
					["skins"]       = Count(catalog, "skins"),
					["styleGroups"] = Count(catalog, "styleGroups"),
				},
				["hint"] = "This is a summary, not the catalog itself. Read the file at 'absolutePath' " +
					"with your own file tools (offset/limit/search or a JSON query) — the full catalog is large.",
			};
			return summary.ToString(Newtonsoft.Json.Formatting.None);
		}

		/// <summary>
		/// Stamps the UiStandardElement marker onto one or more prefab roots. Payload:
		/// <c>{ "elements": [ { "prefabPath": "Assets/.../X.prefab", "key": "OkButton", "internal": false }, ... ] }</c>.
		/// <c>key</c> is an EStandardElement name (toolkit built-in) or any custom id (client element).
		/// The batch is tagged base-before-variant internally, so a client can safely pass a whole set.
		/// </summary>
		// readScreen's payload is either a bare prefab path or a small { "path": "..." } JSON envelope.
		private static JObject ReadConsoleQuery( string _payload )
		{
			var request = string.IsNullOrWhiteSpace(_payload) ? new JObject() : JObject.Parse(_payload);
			return UiEditorConsoleLog.Query(
				(string)request["severity"],
				(string)request["contains"],
				(long?)request["sinceSequence"] ?? 0,
				(int?)request["limit"] ?? 0,
				(bool?)request["withStackTraces"] ?? false);
		}

		/// <summary>
		/// Frame size defaults small on purpose: a filmstrip is several images in one, and the point is the
		/// sequence rather than the detail. A single shot is the tool for looking closely.
		/// </summary>
		private static JObject MotionFilmstrip( string _payload )
		{
			if (string.IsNullOrWhiteSpace(_payload))
				throw new Exception("screenshotMotion requires a 'payload' with at least a prefab path.");

			var request = JObject.Parse(_payload.Trim());

			string path = (string)request["path"];
			if (string.IsNullOrEmpty(path))
				throw new Exception("screenshotMotion payload object must contain a 'path'.");

			return UiScreenMotionPreview.Capture(
				path,
				(string)request["animationNode"],
				(int?)request["frames"] ?? 5,
				(int?)request["width"] ?? 640,
				(int?)request["height"] ?? 360,
				(bool?)request["backwards"] ?? false);
		}

		/// <summary>
		/// Dry run is the default here as well as in the tool description: the request has to ASK to write, so a
		/// malformed payload can only ever produce a report.
		/// </summary>
		private static JObject ApplyPrefabValues( string _payload )
		{
			var request = JObject.Parse(_payload.Trim());

			string path = (string)request["path"];
			if (string.IsNullOrEmpty(path))
				throw new Exception("applyPrefabValues payload object must contain a 'path'.");

			var include = (request["include"] as JArray)?.Select(_t => (string)_t).ToArray();

			return UiPrefabValueRestore.Apply(
				path,
				(string)request["snapshotPath"],
				(bool?)request["dryRun"] ?? true,
				include);
		}

		private static string ReadScreenPath( string _payload )
		{
			string trimmed = _payload.Trim();
			if (trimmed.StartsWith("{"))
			{
				string path = (string) JObject.Parse(trimmed)["path"];
				if (string.IsNullOrEmpty(path))
					throw new Exception("readScreen payload object must contain a 'path'.");
				return path;
			}
			return trimmed.Trim('"');
		}

		// Optional read source ("auto"/"sidecar"/"structural"); only meaningful when the payload is a JSON
		// envelope. A bare path defaults to "auto".
		private static string ReadScreenSource( string _payload )
		{
			string trimmed = _payload.Trim();
			if (trimmed.StartsWith("{"))
				return (string) JObject.Parse(trimmed)["source"] ?? "auto";
			return "auto";
		}

		private static string TagStandardElement( string _payload )
		{
			if (string.IsNullOrWhiteSpace(_payload))
				throw new Exception("tagStandardElement requires a 'payload' with an 'elements' array.");

			if (JObject.Parse(_payload)["elements"] is not JArray elements || elements.Count == 0)
				throw new Exception("tagStandardElement payload must contain a non-empty 'elements' array.");

			var requests = new List<UiStandardElementTagger.TagRequest>();
			foreach (var e in elements)
			{
				string prefabPath = (string) e["prefabPath"];
				if (string.IsNullOrEmpty(prefabPath))
					throw new Exception("Each entry in 'elements' needs a 'prefabPath'.");

				requests.Add(new UiStandardElementTagger.TagRequest
				{
					PrefabPath = prefabPath,
					Key = (string) e["key"] ?? "",
					Internal = (bool?) e["internal"] ?? false,
				});
			}

			return ResultsJson(UiStandardElementTagger.Tag(requests));
		}

		/// <summary>Removes the marker from prefabs. Payload: <c>{ "paths": [ "Assets/.../X.prefab", ... ] }</c>.</summary>
		private static string UntagStandardElement( string _payload )
		{
			if (string.IsNullOrWhiteSpace(_payload))
				throw new Exception("untagStandardElement requires a 'payload' with a 'paths' array.");

			if (JObject.Parse(_payload)["paths"] is not JArray pathsArray || pathsArray.Count == 0)
				throw new Exception("untagStandardElement payload must contain a non-empty 'paths' array.");

			var paths = new List<string>();
			foreach (var p in pathsArray)
			{
				string s = (string) p;
				if (!string.IsNullOrEmpty(s))
					paths.Add(s);
			}

			return ResultsJson(UiStandardElementTagger.Untag(paths));
		}

		/// <summary>
		/// Sets a root <see cref="UiComment"/> flavor description on prefabs. Payload:
		/// <c>{ "comments": [ { "prefabPath": "...", "comment": "..." }, ... ] }</c>.
		/// </summary>
		private static string SetUiComment( string _payload )
		{
			if (string.IsNullOrWhiteSpace(_payload))
				throw new Exception("setUiComment requires a 'payload' with a 'comments' array.");

			if (JObject.Parse(_payload)["comments"] is not JArray comments || comments.Count == 0)
				throw new Exception("setUiComment payload must contain a non-empty 'comments' array.");

			var requests = new List<UiCommentSetter.CommentRequest>();
			foreach (var c in comments)
			{
				string prefabPath = (string) c["prefabPath"];
				if (string.IsNullOrEmpty(prefabPath))
					throw new Exception("Each entry in 'comments' needs a 'prefabPath'.");

				requests.Add(new UiCommentSetter.CommentRequest
				{
					PrefabPath = prefabPath,
					Comment = (string) c["comment"] ?? "",
				});
			}

			var arr = new JArray();
			foreach (var r in UiCommentSetter.Set(requests))
			{
				arr.Add(new JObject
				{
					["prefabPath"] = r.PrefabPath,
					["ok"] = r.Ok,
					["message"] = r.Message,
				});
			}
			return new JObject { ["results"] = arr }.ToString(Newtonsoft.Json.Formatting.None);
		}

		private static string ResultsJson( List<UiStandardElementTagger.TagResult> _results )
		{
			var arr = new JArray();
			foreach (var r in _results)
			{
				arr.Add(new JObject
				{
					["prefabPath"]  = r.PrefabPath,
					["resolvedKey"] = r.ResolvedKey,
					["ok"]          = r.Ok,
					["message"]     = r.Message,
				});
			}

			return new JObject { ["results"] = arr }.ToString(Newtonsoft.Json.Formatting.None);
		}

		/// <summary>
		/// A one-shot project-state health check for the authoring AI: why does everything resolve to the
		/// library look, is the catalog stale, is the variants folder real, etc. Answering this from a
		/// snapshot is far cheaper than reconstructing it, which was the single biggest cost the first time
		/// a fresh instance drove the loop.
		/// </summary>
		private static string SetupStatusJson()
		{
			var config = UiToolkitConfiguration.Instance;
			var registry = config != null ? config.StandardElementRegistry : null;

			int client = 0, library = 0;
			var registryKeys = new HashSet<string>(StringComparer.Ordinal);
			if (registry != null)
			{
				foreach (var e in registry.Entries)
				{
					if (e == null)
						continue;
					if (e.fromLibrary) library++; else client++;
					if (!string.IsNullOrEmpty(e.Key)) registryKeys.Add(e.Key);
				}
			}

			string variantsPath = config != null ? config.PrefabVariantsPath : null;
			string variantsTrim = variantsPath?.TrimEnd('/');
			bool variantsExists = !string.IsNullOrEmpty(variantsTrim) && AssetDatabase.IsValidFolder(variantsTrim);

			var paletteConfig = UiAuthorablePaletteConfig.FindFirst();
			string palettePath = paletteConfig != null ? AssetDatabase.GetAssetPath(paletteConfig) : null;

			string catalogPath = UiScreenCatalogGenerator.CatalogPath;
			bool catalogExists = File.Exists(catalogPath);
			JToken catalogAge = JValue.CreateNull();
			var ambiguities = new JArray();
			if (catalogExists)
			{
				try
				{
					// JObject.Parse turns the ISO-8601 timestamp into a Date JValue; casting that back to
					// string drops the "Z", so re-parsing it would yield Kind=Unspecified and be read as
					// local time — the reported age was off by exactly the UTC offset. Take the DateTime
					// directly, and only fall back to string parsing if date handling ever changes.
					var catalogJson = JObject.Parse(File.ReadAllText(catalogPath));

					// Standard-element key collisions: the generator logs them, but the Unity console is
					// not reachable over MCP, so they travel in the catalog and get surfaced here.
					if (catalogJson["standardElementAmbiguities"] is JArray fromCatalog)
						ambiguities = fromCatalog;

					var generatedAt = catalogJson["generatedAtUtc"];
					DateTime genUtc = default;
					if (generatedAt != null && generatedAt.Type == JTokenType.Date)
						genUtc = generatedAt.Value<DateTime>().ToUniversalTime();
					else if (generatedAt != null)
						DateTime.TryParse((string)generatedAt, CultureInfo.InvariantCulture,
							DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal, out genUtc);

					if (genUtc != default)
						catalogAge = (int)(DateTime.UtcNow - genUtc).TotalMinutes;
				}
				catch { /* leave null */ }
			}

			var missing = new JArray();
			foreach (EStandardElement v in Enum.GetValues(typeof(EStandardElement)))
			{
				if (v == EStandardElement.None || v == EStandardElement.Custom)
					continue;
				if (!registryKeys.Contains(v.ToString()))
					missing.Add(v.ToString());
			}

			var status = new JObject
			{
				["registry"] = new JObject
				{
					["assigned"] = registry != null,
					["path"] = registry != null ? AssetDatabase.GetAssetPath(registry) : null,
					["entries"] = client + library,
					["client"] = client,
					["library"] = library,
				},
				["prefabVariantsPath"] = new JObject
				{
					["value"] = variantsPath,
					["exists"] = variantsExists,
				},
				["paletteConfig"] = palettePath,
				["catalog"] = new JObject
				{
					["path"] = catalogPath,
					["exists"] = catalogExists,
					["ageMinutes"] = catalogAge,
				},
				["missingStandardElements"] = missing,
				["standardElementAmbiguities"] = ambiguities,
				["hint"] = "Screens or UiMain resolving to the LIBRARY look usually means registry.client == 0 " +
					"(no client variants discovered) and/or prefabVariantsPath.exists == false. Fix the path or " +
					"tag the client prefabs, then regenerate_catalog. A large catalog.ageMinutes means the " +
					"vocabulary may be stale — regenerate. A non-empty standardElementAmbiguities means a key " +
					"is claimed by several prefabs and silently resolved to the alphabetically first one — " +
					"give the losing candidates their own custom ids via tag_standard_element.",
			};
			return status.ToString(Newtonsoft.Json.Formatting.None);
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
