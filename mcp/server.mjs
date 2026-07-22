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
	"get_catalog",
	"Return the current GUI Toolkit screen-authoring catalog (the machine-readable vocabulary of authorable components, props, styles and skins) as JSON.",
	{},
	async () => {
		try { return ok(await callBridge("getCatalog")); }
		catch (e) { return fail(e); }
	}
);

server.tool(
	"regenerate_catalog",
	"Re-run the catalog generator inside Unity (reflects the latest components), then return the fresh catalog JSON.",
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
