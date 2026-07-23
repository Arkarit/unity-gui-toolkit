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
	"bake_screen",
	"Bake a screen description into a real Unity .prefab asset. 'screen' is the screen JSON " +
	"(see get_catalog for the component/template vocabulary): { name, root: { type|template, id, " +
	"props, style, text, children[] } }. Returns the project-relative path of the baked prefab.",
	{ screen: z.union([z.string(), z.record(z.any())]).describe("The screen description (JSON object or JSON string).") },
	async ({ screen }) => {
		try {
			const payload = typeof screen === "string" ? screen : JSON.stringify(screen);
			return ok(await callBridge("bakeScreen", payload));
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

const transport = new StdioServerTransport();
await server.connect(transport);
