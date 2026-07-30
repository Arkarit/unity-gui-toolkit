#!/usr/bin/env node
// Minimal MCP server ("Mini-DAL") for GUI Toolkit AI screen authoring.
// Thin stdio proxy: translates MCP tool calls into HTTP requests to the Unity Editor
// bridge (Editor/AiSupport/UiScreenMcpBridge.cs). Start that bridge in Unity first:
//   Gui Toolkit -> AI -> Start MCP Bridge
//
// Requires Node 18+ (global fetch). Install deps once:  npm install  (in this folder)

import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { readFile } from "node:fs/promises";

const BRIDGE_URL = process.env.UI_TOOLKIT_BRIDGE_URL ?? "http://127.0.0.1:17632/";

async function callBridge(method, payload) {
	let res;
	try {
		res = await fetch(BRIDGE_URL, {
			method: "POST",
			headers: { "content-type": "application/json" },
			body: JSON.stringify(payload === undefined ? { method } : { method, payload }),
		});
	} catch (e) {
		throw new Error(
			`Cannot reach the Unity bridge at ${BRIDGE_URL}. ` +
			`Is the Editor open and 'Gui Toolkit > AI > Start MCP Bridge' enabled? (${e.message})`
		);
	}

	const text = await res.text();
	if (!res.ok)
		throw new Error(`Unity bridge returned ${res.status}: ${text}`);
	return text;
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

// Like callBridge but never throws — returns the parsed JSON, or null when the bridge is
// unreachable (e.g. the HTTP listener is briefly down during a domain reload).
async function tryBridge(method) {
	try {
		const res = await fetch(BRIDGE_URL, {
			method: "POST",
			headers: { "content-type": "application/json" },
			body: JSON.stringify({ method }),
		});
		if (!res.ok) return null;
		return JSON.parse(await res.text());
	} catch {
		return null;
	}
}

function ok(text) {
	return { content: [{ type: "text", text }] };
}

function fail(error) {
	return { content: [{ type: "text", text: String(error?.message ?? error) }], isError: true };
}

const server = new McpServer({ name: "ui-toolkit", version: "0.1.0" });

server.tool(
	"ping",
	"Check that the Unity Editor GUI Toolkit bridge is reachable.",
	{},
	async () => {
		try { return ok(await callBridge("ping")); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"status",
	"Report whether the Unity Editor is currently compiling scripts or importing assets. " +
	"Returns { running, compiling, updating }.",
	{},
	async () => {
		try { return ok(await callBridge("status")); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"setup_status",
	"One-shot health check of this project's screen-authoring setup — call it FIRST when authored screens " +
	"come out looking wrong, or to check whether the catalog is stale. Returns { registry:{assigned, path, " +
	"entries, client, library}, prefabVariantsPath:{value, exists}, paletteConfig, catalog:{path, exists, " +
	"ageMinutes}, missingStandardElements[], standardElementAmbiguities[] }. The usual 'everything looks " +
	"like the library' cause is registry.client == 0 together with prefabVariantsPath.exists == false. " +
	"standardElementAmbiguities lists keys claimed by several prefabs — the generator silently takes the " +
	"alphabetically first, so a wrong-looking element is often hiding there (prefab variants inherit their " +
	"base's marker, which is the usual source). Far cheaper than reconstructing the project state by hand.",
	{},
	async () => {
		try { return ok(await callBridge("setupStatus")); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"recompile",
	"Force Unity to pick up and recompile changed editor/runtime C# scripts, then WAIT until the " +
	"compilation and the following domain reload have finished. Use this after editing toolkit C# so " +
	"you don't have to ask a human to click into the Unity window. Returns when the editor is idle again.",
	{},
	async () => {
		try {
			const started = await callBridge("recompile"); // returns immediately: {"recompiling":true}
			void started;

			const TIMEOUT_MS = 180000;
			const t0 = Date.now();
			let sawActivity = false;   // compiling/updating seen, or the bridge went down for a reload
			let reloaded = false;

			await sleep(1500); // let Unity begin compiling (RequestScriptCompilation runs next tick)

			while (Date.now() - t0 < TIMEOUT_MS) {
				const st = await tryBridge("status");
				if (st === null) {
					// Bridge unreachable — almost certainly the domain reload window. Keep waiting.
					sawActivity = true;
					reloaded = true;
					await sleep(1000);
					continue;
				}
				if (st.compiling || st.updating) {
					sawActivity = true;
					await sleep(1000);
					continue;
				}
				// Editor is idle.
				if (sawActivity)
					return ok(JSON.stringify({ recompiled: true, reloaded, ms: Date.now() - t0 }));
				// Idle but never saw activity yet — compile may not have kicked in; give it a short grace.
				if (Date.now() - t0 > 12000)
					return ok(JSON.stringify({ recompiled: true, reloaded, note: "no compilation activity detected", ms: Date.now() - t0 }));
				await sleep(1000);
			}
			return ok(JSON.stringify({ recompiled: false, reloaded, note: "timed out waiting for the editor to go idle", ms: Date.now() - t0 }));
		} catch (e) {
			return fail(e);
		}
	}
);

server.tool(
	"get_catalog",
	"Locate the GUI Toolkit screen-authoring catalog (the machine-readable vocabulary of authorable " +
	"components, props, styles and skins). Returns a small JSON summary — { path, absolutePath, version, " +
	"generatedAtUtc, byteSize, counts } — NOT the catalog body: the catalog is large (~750 KB) and this " +
	"server runs on your machine, so read the file at 'absolutePath' yourself with your own file tools " +
	"(offset/limit/search or a JSON query) instead of loading it all at once.",
	{},
	async () => {
		try { return ok(await callBridge("getCatalog")); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"regenerate_catalog",
	"Re-run the catalog generator inside Unity (reflects the latest components), then return the same " +
	"summary envelope as get_catalog ({ path, absolutePath, counts, ... }). Read the file at 'absolutePath' " +
	"yourself for the full vocabulary — the body is not returned inline.",
	{},
	async () => {
		try { return ok(await callBridge("regenerateCatalog")); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"get_console",
	"Read THIS editor session's console messages — what Unity actually said, with severities, instead of " +
	"pattern-matching Editor.log from outside. Editor.log spans several sessions and readily serves a previous " +
	"run's compiler errors as if they were current, which is a good way to reach a confident wrong conclusion. " +
	"Filters: 'severity' ('error' | 'warning' meaning warning-and-worse | 'all'), 'contains' (case-insensitive " +
	"substring of the message), 'limit' (newest N, default 100), 'withStackTraces'. The reliable way to ask " +
	"\"what did that action produce\" is 'sinceSequence': note 'nextSequence' from a call, do the thing, then " +
	"pass it back and get only what is new. 'bufferedTotals' counts everything still buffered, not just what " +
	"was returned. The buffer holds the last 1000 messages and starts empty after a domain reload.",
	{
		severity: z.enum(["error", "warning", "log", "all"]).optional().describe("Narrow by severity; 'warning' includes errors."),
		contains: z.string().optional().describe("Only messages containing this text (case-insensitive)."),
		sinceSequence: z.number().optional().describe("Only messages newer than this sequence number (from a previous call's nextSequence)."),
		limit: z.number().optional().describe("Return at most this many, newest kept (default 100)."),
		withStackTraces: z.boolean().optional().describe("Include stack traces — verbose, so off by default."),
	},
	async (args) => {
		try { return ok(await callBridge("getConsole", JSON.stringify(args ?? {}))); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"resolve_packages",
	"Make Unity pick up an edited Packages/manifest.json right now, then WAIT until the editor is idle again. " +
	"Unity normally only notices an externally edited manifest when it regains focus, so changing a package " +
	"version otherwise means waiting for a human to click into the editor. Use this straight after editing the " +
	"manifest — for example to point the toolkit at a different tag, or at a local working copy while testing a " +
	"fix. Expect a package re-resolve plus asset import, so it can take minutes on a large project, and the " +
	"bridge goes down during the domain reload (that is normal and waited out here).",
	{},
	async () => {
		try {
			await callBridge("resolvePackages");

			const TIMEOUT_MS = 600000;
			const t0 = Date.now();
			let sawActivity = false;
			let reloaded = false;

			await sleep(2000); // the resolve is queued, give it a moment to start

			while (Date.now() - t0 < TIMEOUT_MS) {
				const st = await tryBridge("status");
				if (st === null) {
					// Bridge unreachable: the import/domain-reload window. Keep waiting.
					sawActivity = true;
					reloaded = true;
					await sleep(2000);
					continue;
				}
				if (st.compiling || st.updating) {
					sawActivity = true;
					await sleep(2000);
					continue;
				}
				if (sawActivity)
					return ok(JSON.stringify({ resolved: true, reloaded, ms: Date.now() - t0 }));
				if (Date.now() - t0 > 15000)
					return ok(JSON.stringify({ resolved: true, reloaded, note: "no import activity detected — the manifest may already have been up to date", ms: Date.now() - t0 }));
				await sleep(2000);
			}
			return ok(JSON.stringify({ resolved: false, reloaded, note: "timed out waiting for the editor to go idle", ms: Date.now() - t0 }));
		} catch (e) {
			return fail(e);
		}
	}
);

server.tool(
	"capture_prefab_values",
	"Capture EVERY serialized value of every component in a baked prefab into a temporary snapshot, keyed by " +
	"node path and Unity's own propertyPath strings. Use it before re-baking a prefab a human has edited: the " +
	"screen description is a curated vocabulary, so an edit it cannot express (a font size, a gradient " +
	"direction, a value on a template's internals) is invisible to read_screen and to preserveEdits — a " +
	"snapshot is complete by construction instead. Returns a summary { snapshotPath, nodes, components, " +
	"values, objectReferences, byteSize }; read the file itself for the values. The snapshot lands under " +
	"Library/ and must stay uncommitted: it is a clipboard, not a second description competing to be the " +
	"source of truth. Object references are recorded descriptively (asset path, or the node they point at) for " +
	"ANALYSIS only — never write them back, since a re-bake makes fresh object ids and wiring belongs to the " +
	"description via \"#id\". Derived data (TMP text info, meshes, cached sizes) is deliberately not captured.",
	{ path: z.string().describe("Project-relative prefab path, e.g. 'Assets/.../MyScreen.prefab'.") },
	async ({ path }) => {
		try { return ok(await callBridge("capturePrefabValues", JSON.stringify({ path }))); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"list_styles",
	"List this project's STYLE vocabulary — the named looks (fonts, colours, sprites, gradients) a screen " +
	"node applies via its \"style\" field, grouped by the component type they target. Call this BEFORE " +
	"authoring any text or panel: using a project style is what makes an authored screen look like the rest " +
	"of the game, whereas leaving TMP/Image at their defaults is the single biggest reason a screen reads as " +
	"'assembled by a machine'. Cheap — it reads the catalog off disk, no Unity round-trip beyond locating it. " +
	"Style names carry NO reliable size/role semantics, and similar names can be unrelated looks (in BOTW, " +
	"'Text/Headline' is the uppercase display font while 'Text/Headline/Large' is a plain sans), so do not " +
	"infer the look from the name: bake a specimen screen — one text node per style, each labelled with its " +
	"own name — and screenshot_view it once. Two further rules the names cannot tell you: a style sets a " +
	"sprite but never the Image draw mode (add \"type\": \"Sliced\"/\"Tiled\" yourself), and props are applied " +
	"BEFORE the style, so the style wins on colours and sprites — darken via a separate overlay, not a tint.",
	{ targetType: z.string().optional().describe("Only return groups for this component type (e.g. \"TMP_Text\", \"Image\")."), },
	async ({ targetType }) => {
		try {
			const summary = JSON.parse(await callBridge("getCatalog"));
			const catalog = JSON.parse(await readFile(summary.absolutePath, "utf8"));
			let groups = catalog.styleGroups ?? [];
			if (targetType)
				groups = groups.filter((g) => (g.componentType ?? "") === targetType);
			return ok(JSON.stringify({
				catalogPath: summary.path,
				generatedAtUtc: summary.generatedAtUtc,
				styleGroups: groups.map((g) => ({ componentType: g.componentType, styleNames: g.styleNames ?? [] })),
			}, null, 1));
		} catch (e) {
			return fail(e);
		}
	}
);

server.tool(
	"bake_screen",
	"Bake a screen description into a real Unity .prefab asset. Pass the description either inline via " +
	"'screen' or, when it already exists on disk, via 'screenPath' — a baked screen writes its description to " +
	"'<name>.screen.src.json' next to the prefab, so re-baking one is bake_screen({ screenPath: that file }) " +
	"and never requires resending the description. 'screen' is the screen JSON " +
	"(see get_catalog for the component/template vocabulary): { name, root: { type|template, id, " +
	"props, style, text, children[] } }. The 'type' vocabulary is both the toolkit's Ui* components AND " +
		"raw UGUI/Unity building blocks exposed via an allow-list (Image, RawImage, Button, ScrollRect, Mask, " +
		"RectMask2D, LayoutElement, ContentSizeFitter, AspectRatioFitter, the LayoutGroups, CanvasGroup, ...); " +
		"their catalog entries carry a 'unityType' plus an optional 'prefer' hint naming a toolkit wrapper you " +
		"should usually use instead (e.g. ScrollRect → UiScrollRect). CanvasGroup props (alpha/interactable/...) " +
		"bake via property setters like any other prop. Returns { path, warnings }: 'warnings' lists non-fatal issues " +
	"(dropped props, templates that resolved to a different prefab, unresolved text) — read it to fix the " +
	"screen without having to screenshot and guess. Optionally pin 'outputPath' (a full .prefab path, or a " +
	"folder the screen name is appended to) so 'edit → re-bake' keeps hitting the same asset after you move " +
	"it; it can also be a top-level field in the screen JSON. " +
	"WORKFLOW: finish the visual layout first, THEN wire references — a re-bake rebuilds the prefab and " +
	"drops anything you set by hand. The baker DOES resolve component references via \"#id\" props " +
	"(m_target, m_closeButtons, animation slaves — a \"#nodeId\" string, or an array of them for a list/array " +
	"field) and AnimationCurve props (a preset name \"linear\"/\"easeInOut\"/\"constant\", an object " +
	"{ preset, from:[time,value], to:[time,value] }, or a keyframe list [ { time, value, inTangent?, outTangent? } ]). " +
	"A node with a ScrollRect can carry a \"scroll\" field to make it actually scroll — " +
	"{ direction:\"vertical\"|\"horizontal\"|\"both\", layout:\"vertical\"|\"horizontal\"|\"grid\"|\"none\", fit:true, " +
	"spacing, padding:[l,r,t,b], cellSize:[w,h], childAlignment } — which adds a layout group + ContentSizeFitter to " +
	"the Content (a bare ScrollRect defaults to a vertical list even without it). " +
	"To stack several components on one node (e.g. a UiView that is also a UiSimpleAnimation) add a " +
	"\"components\" array of type names (or { type, props } objects) — no wrapper node needed. " +
	"Set preserveEdits:true on a re-bake to keep hand edits made to the existing prefab since the last bake " +
	"(props/text that differ from the baseline and that this JSON does not itself specify are folded back in; " +
	"warnings list what was kept). Call setup_status first if screens come out looking wrong. " +
	"NEVER COPY A SUBTREE. Two children that look the same are already redundancy — author the repeated " +
	"part ONCE as its own prefab via the screen's \"prefabs\" array (each entry is a full screen description, " +
	"baked before this screen; their paths come back as 'companions'), then instantiate it per row with " +
	"\"template\": \"<its name>\" and vary the differing parts through \"overrides\": " +
	"{ \"Child/Path\": { props, style, text, rect, id } }, keyed by transform path inside the template (an " +
	"\"id\" there also makes an internal part addressable by \"#id\"). The baker warns when it sees identical " +
	"sibling subtrees. Better still for a list: rows filled from data at runtime need ONE authored instance " +
	"or none — author the container plus a prefab reference (a prefab-path string on a prefab-typed prop) and " +
	"let the runtime spawn the rows, which is how shipped screens do it. " +
	"LOOK: give text and panel nodes a \"style\" from the project's vocabulary (list_styles) instead of " +
	"leaving TMP/Image at their defaults, and compose from the catalog's 'palette' templates rather than " +
	"stacking raw Image + text nodes — a project ships ready-made headline/panel/button-bar/price-tag pieces, " +
	"and hand-building past them is what makes an authored screen look unfinished. Read a comparable shipped " +
	"screen with read_screen first to see which templates and styles it actually uses.",
	{
		screen: z.union([z.string(), z.record(z.any())]).optional().describe("The screen description (JSON object or JSON string). Omit when using screenPath."),
		screenPath: z.string().optional().describe("Path to a file holding the screen description instead of passing it inline — most usefully a baked screen's own '.screen.src.json' sidecar, to re-bake it unchanged or with outputPath/preserveEdits applied. Exactly one of 'screen' or 'screenPath'."),
		outputPath: z.string().optional().describe("Where to write the prefab (full .prefab path or a folder). Overrides the default Generated folder."),
		preserveEdits: z.boolean().optional().describe("On a re-bake, fold hand edits from the existing prefab back in so they survive (default false)."),
	},
	async ({ screen, screenPath, outputPath, preserveEdits }) => {
		try {
			if (!screen && !screenPath)
				return fail(new Error("Pass either 'screen' (inline) or 'screenPath' (a file holding it)."));
			if (screen && screenPath)
				return fail(new Error("Pass either 'screen' or 'screenPath', not both."));

			let payload;
			if (screenPath) {
				// Read here rather than in Unity: the server is local, and this is the whole point of the
				// parameter — a description that already exists on disk should not have to travel through the
				// conversation again just to be re-baked.
				const obj = JSON.parse(await readFile(screenPath, "utf8"));
				if (outputPath) obj.outputPath = outputPath;
				if (preserveEdits) obj.preserveEdits = true;
				payload = JSON.stringify(obj);
			} else if (outputPath || preserveEdits) {
				const obj = typeof screen === "string" ? JSON.parse(screen) : screen;
				if (outputPath) obj.outputPath = outputPath;
				if (preserveEdits) obj.preserveEdits = true;
				payload = JSON.stringify(obj);
			} else {
				payload = typeof screen === "string" ? screen : JSON.stringify(screen);
			}
			return ok(await callBridge("bakeScreen", payload));
		} catch (e) {
			return fail(e);
		}
	}
);

server.tool(
	"read_screen",
	"Read an existing baked (or hand-built) .prefab back into screen JSON in the same shape bake_screen " +
	"consumes — inspect what's in a prefab, tweak the JSON, and re-bake. Returns { screen, warnings }. " +
	"Read-back is BEST-EFFORT/structural, not a byte-perfect inverse: template nodes come back with their " +
	"standard-element key + the overridden props; element nodes with the primary component type + props that " +
	"differ from its default; cross-node references are re-expressed as \"#id\" with synthesized ids " +
	"(a reference into a template's internal part is dropped with a warning). Unsupported prop types are " +
	"omitted (they still live in the prefab).",
	{
		path: z.string().describe("Project-relative path of the prefab to read (e.g. from bake_screen)."),
		source: z.enum(["auto", "sidecar", "structural"]).optional().describe(
			"Where the JSON comes from. auto (default): the clean source sidecar from bake time if present, " +
			"else a structural read-back. sidecar: the sidecar only (errors if none). structural: always the " +
			"prefab's current state, including later hand edits."),
	},
	async ({ path, source }) => {
		try {
			return ok(await callBridge("readScreen", JSON.stringify(source ? { path, source } : { path })));
		} catch (e) {
			return fail(e);
		}
	}
);

server.tool(
	"screenshot_view",
	"Render a baked screen prefab to a PNG image (Edit-Mode, no Play Mode) so you can see the result " +
	"and iterate. 'path' is the project-relative prefab path returned by bake_screen. Returns the image.",
	{
		path: z.string().describe("Project-relative path of the baked prefab (from bake_screen)."),
		width: z.number().int().positive().optional().describe("Render width in px (default 1920)."),
		height: z.number().int().positive().optional().describe("Render height in px (default 1080)."),
	},
	async ({ path, width, height }) => {
		try {
			const payload = JSON.stringify({ path, width: width ?? 0, height: height ?? 0 });
			const text = await callBridge("screenshotView", payload);
			const result = JSON.parse(text);
			if (!result.png)
				throw new Error("Bridge returned no image data.");
			return {
				content: [{ type: "image", data: result.png, mimeType: "image/png" }],
			};
		} catch (e) {
			return fail(e);
		}
	}
);

server.tool(
	"tag_standard_element",
	"Tag one or more prefab roots with a standard-element identity (the UiStandardElement marker) so " +
	"they enter the toolkit's registry and screen-authoring palette. Use this after creating a prefab or " +
	"variant you want the toolkit/AI to recognise as a named standard element. 'key' is either a built-in " +
	"EStandardElement name (see the catalog) or any custom id for a project-specific element; a client " +
	"variant that reuses a built-in key out-ranks the toolkit default. Set 'internal' for sub-parts that " +
	"should resolve but stay out of the authoring vocabulary. A mixed base+variant batch is safe — it is " +
	"tagged base-before-variant internally. Returns one result per prefab. For large batches, send in " +
	"chunks so the bridge request does not time out.",
	{
		elements: z.array(z.object({
			prefabPath: z.string().describe("Project-relative prefab path, e.g. 'Assets/.../OkButton.prefab'."),
			key: z.string().describe("EStandardElement name (built-in) or a custom id (client element)."),
			internal: z.boolean().optional().describe("True for an internal sub-part (hidden from the authoring vocabulary)."),
		})).min(1).describe("The prefabs to tag."),
	},
	async ({ elements }) => {
		try { return ok(await callBridge("tagStandardElement", JSON.stringify({ elements }))); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"untag_standard_element",
	"Remove the UiStandardElement marker from one or more prefabs (no-op where none is present). " +
	"Returns one result per prefab.",
	{
		paths: z.array(z.string()).min(1).describe("Project-relative prefab paths to untag."),
	},
	async ({ paths }) => {
		try { return ok(await callBridge("untagStandardElement", JSON.stringify({ paths }))); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"set_ui_comment",
	"Set a 'flavor' description (a UiComment) on one or more prefab ROOTS — the note humans read in the " +
	"Inspector and, for a palette prefab, the description harvested into the screen-authoring catalog. Use it " +
	"to document a prefab or variant you created (e.g. 'OkButton — green confirm button'). Idempotent (updates " +
	"in place). A mixed base+variant batch is safe: written base-before-variant so a variant stores its own " +
	"text as an override on the inherited component. Returns one result per prefab.",
	{
		comments: z.array(z.object({
			prefabPath: z.string().describe("Project-relative prefab path."),
			comment: z.string().describe("The flavor description to store on the root's UiComment."),
		})).min(1).describe("The prefabs to describe."),
	},
	async ({ comments }) => {
		try { return ok(await callBridge("setUiComment", JSON.stringify({ comments }))); }
		catch (e) { return fail(e); }
	}
);

const transport = new StdioServerTransport();
await server.connect(transport);
