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
import { readFile, readdir } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { homedir } from "node:os";
import { AsyncLocalStorage } from "node:async_hooks";

// Where this proxy was launched. Claude Code uses the project directory; "--project <path>" overrides
// it. This is only the STARTING POINT for finding a bridge, not the answer: a repo root and a Unity
// project root are not always the same directory.
const LAUNCH_ROOT = (() => {
	const flag = process.argv.indexOf("--project");
	const raw = flag >= 0 && process.argv[flag + 1] ? process.argv[flag + 1] : process.cwd();
	return resolve(raw).replace(/\\/g, "/");
})();


const DISCOVERY_RELATIVE = "Library/UiToolkit/mcp-bridge.json";
const FORCED_URL = process.env.UI_TOOLKIT_BRIDGE_URL ?? null;

/** Directories never worth descending into while looking for a Unity project. */
const SKIP_DIRS = new Set(["node_modules", ".git", "Library", "Temp", "obj", "Build", "Logs", "Assets"]);
const SCAN_DEPTH = 3;

/** Where every running bridge on this machine announces itself, mirroring the editor's own path. */
const REGISTRY_DIR = join(process.env.LOCALAPPDATA ?? join(homedir(), ".local/share"),
	"UiToolkit", "bridges").replace(/\\/g, "/");

/**
 * The target of the call in flight. Carried in async context rather than a module variable that a tool
 * could switch: which project a call writes to must be readable from the call itself. A "switch project"
 * command would put that back out of sight, which is the failure this whole mechanism exists to prevent.
 */
const g_callContext = new AsyncLocalStorage();

/** Resolved bridges by target key ("" = the launch project). Re-resolved whenever one stops answering. */
const g_bridges = new Map();

const normalisePath = (p) => String(p ?? "").replace(/\\/g, "/").replace(/\/+$/, "");
const sameProject = (a, b) => normalisePath(a).toLowerCase() === normalisePath(b).toLowerCase();

async function readDiscoveryAt(dir) {
	try {
		const info = JSON.parse(await readFile(join(dir, DISCOVERY_RELATIVE), "utf8"));
		return info?.url || info?.port ? info : null;
	} catch {
		return null;
	}
}

/**
 * Finds the bridge belonging to this launch directory. Upwards first, for being started in a
 * subfolder; then a bounded scan downwards, because a repo may CONTAIN its Unity project rather than
 * be one — the toolkit's own dev app lives in .Dev-App/Unity, which is exactly this case and which a
 * root-only lookup missed. Several candidates are an error rather than a guess: picking the wrong one
 * would mean writing into the wrong project.
 */
async function findDiscovery() {
	const searched = [];

	let dir = LAUNCH_ROOT;
	for (let i = 0; i <= SCAN_DEPTH; i++) {
		searched.push(dir);
		const info = await readDiscoveryAt(dir);
		if (info)
			return { info, root: dir };

		const parent = dirname(dir).replace(/\\/g, "/");
		if (parent === dir)
			break;
		dir = parent;
	}

	const found = [];
	await scanDown(LAUNCH_ROOT, 1, found, searched);
	if (found.length === 1)
		return found[0];

	if (found.length > 1) {
		throw new Error(
			`Several Unity projects below '${LAUNCH_ROOT}' have a running bridge: ` +
			found.map((f) => f.root).join(", ") +
			`. Pass --project <path> in the MCP registration to say which one this server serves.`
		);
	}

	throw new Error(
		`No Unity bridge found for '${LAUNCH_ROOT}'. A running bridge writes ${DISCOVERY_RELATIVE} into its ` +
		`own project; none was found in or below the directories searched (${searched.join(", ")}). Open the ` +
		`project in Unity and enable 'Gui Toolkit > AI > Start MCP Bridge' — or pass --project <path> if the ` +
		`Unity project lives somewhere this search does not reach.`
	);
}

async function scanDown(dir, depth, found, searched) {
	if (depth > SCAN_DEPTH)
		return;

	let entries;
	try {
		entries = await readdir(dir, { withFileTypes: true });
	} catch {
		return;
	}

	for (const entry of entries) {
		if (!entry.isDirectory() || SKIP_DIRS.has(entry.name))
			continue;

		const child = join(dir, entry.name).replace(/\\/g, "/");
		searched.push(child);

		const info = await readDiscoveryAt(child);
		if (info) {
			found.push({ info, root: child });
			continue; // a Unity project does not contain another
		}
		await scanDown(child, depth + 1, found, searched);
	}
}

/** Every bridge this machine knows about, alive or not. */
async function listRegistryEntries() {
	let files;
	try {
		files = await readdir(REGISTRY_DIR);
	} catch {
		return [];
	}

	const entries = [];
	for (const file of files) {
		if (!file.endsWith(".json"))
			continue;
		try {
			const info = JSON.parse(await readFile(join(REGISTRY_DIR, file), "utf8"));
			if (info?.projectPath && (info.url || info.port))
				entries.push(info);
		} catch { /* an unreadable entry is simply not offered */ }
	}
	return entries;
}

/**
 * Finds the bridge a "project" argument asks for. A full path or a bare folder name both work; an
 * ambiguous name is an error naming the candidates, because picking one would mean writing into a
 * project the caller did not name.
 */
async function findByProjectSpec(spec) {
	const wanted = normalisePath(spec);
	const entries = await listRegistryEntries();

	const byPath = entries.filter((e) => sameProject(e.projectPath, wanted));
	if (byPath.length === 1)
		return { info: byPath[0], root: byPath[0].projectPath };

	// Folder name OR Unity product name: "botw-client" and "BOW" should both find the same editor, and a
	// project nested in a repo often has a useless folder name ("Unity") but a meaningful product name.
	const short = wanted.split("/").filter(Boolean).pop()?.toLowerCase() ?? "";
	const byName = entries.filter(
		(e) => String(e.projectName ?? "").toLowerCase() === short ||
		       String(e.productName ?? "").toLowerCase() === short ||
		       normalisePath(e.projectPath).split("/").pop()?.toLowerCase() === short);

	if (byName.length === 1)
		return { info: byName[0], root: byName[0].projectPath };

	if (byName.length > 1) {
		throw new Error(
			`'${spec}' matches several running bridges: ${byName.map((e) => e.projectPath).join(", ")}. ` +
			`Pass the full project path instead.`
		);
	}

	// Not announced machine-wide (an older bridge, or a registry that could not be written): fall back to
	// reading the project's own file, which is the authority anyway.
	const direct = await readDiscoveryAt(resolve(spec));
	if (direct)
		return { info: direct, root: normalisePath(resolve(spec)) };

	throw new Error(
		`No running bridge for project '${spec}'. Running: ` +
		(entries.length ? entries.map((e) => `${e.projectName ?? "?"} (${e.projectPath})`).join(", ") : "none") +
		`. Use list_projects to see what is available, and start a bridge with ` +
		`'Gui Toolkit > AI > Start MCP Bridge' in the editor you mean.`
	);
}

async function resolveBridge() {
	const spec = g_callContext.getStore()?.project ?? null;
	const key = spec ? normalisePath(spec).toLowerCase() : "";

	const cached = g_bridges.get(key);
	if (cached)
		return cached;

	let info;
	let root;
	if (spec) {
		({ info, root } = await findByProjectSpec(spec));
	} else if (FORCED_URL) {
		// A forced URL replaces only the CONNECTING, not the checking. Discovery still runs to learn which
		// project this launch directory belongs to — comparing against the launch directory itself would
		// wrongly reject a nested project's own bridge, which is precisely the toolkit's own layout. If
		// nothing is discoverable there is simply nothing to hold it to.
		try { ({ info, root } = await findDiscovery()); }
		catch { info = null; root = null; }
	} else {
		({ info, root } = await findDiscovery());
	}

	// Compare against what the ANNOUNCEMENT claims, not against the launch directory: the project may
	// legitimately sit below it. This still catches the case that matters — an entry left behind by a crash
	// whose port is now held by a different editor.
	const bridge = {
		url: (spec ? null : FORCED_URL) ?? info?.url ?? (info?.port ? `http://127.0.0.1:${info.port}/` : null),
		expectedProject: info ? normalisePath(info.projectPath ?? root) : null,
		verified: false,
		key,
	};

	g_bridges.set(key, bridge);
	return bridge;
}

function forget(bridge) {
	g_bridges.delete(bridge.key);
}

// The whole point of the discovery file: confirm the editor that answered is the one belonging to this
// project. Cheap (one status call per URL) and it turns a silent catastrophe — baking into a different
// project that merely happened to hold the port — into a refusal.
function unreachable(bridge, cause) {
	forget(bridge);
	return new Error(
		`Cannot reach the Unity bridge at ${bridge.url} (expected project ` +
		`'${bridge.expectedProject ?? LAUNCH_ROOT}'). Is that Editor open and ` +
		`'Gui Toolkit > AI > Start MCP Bridge' enabled? (${cause})`
	);
}

// The whole point of announcing a project: confirm the editor that answered is the one meant. Cheap (one
// status call per target) and it turns a silent catastrophe — writing into a different project that merely
// happened to hold the port — into a refusal.
async function verifyBridge(bridge) {
	if (bridge.verified)
		return;

	if (!bridge.url) {
		forget(bridge);
		throw new Error(`No bridge address could be resolved for '${bridge.expectedProject ?? LAUNCH_ROOT}'.`);
	}

	let res;
	try {
		res = await fetch(bridge.url, {
			method: "POST",
			headers: { "content-type": "application/json" },
			body: JSON.stringify({ method: "status" }),
		});
	} catch (e) {
		throw unreachable(bridge, e.message);
	}

	const info = JSON.parse(await res.text());
	const reported = normalisePath(info.projectPath);

	// Nothing to hold it to: either an older bridge that does not report its project, or a hand-set URL
	// with no project named. Allowed, because refusing would break setups that were fine before.
	if (!reported || !bridge.expectedProject) {
		bridge.verified = true;
		return;
	}

	if (!sameProject(reported, bridge.expectedProject)) {
		forget(bridge);
		throw new Error(
			`Refusing to use the bridge at ${bridge.url}: it serves '${reported}', but this call expects ` +
			`'${bridge.expectedProject}'. Writing through it would modify the wrong project. The announcement ` +
			`is probably stale — restart that bridge, or use list_projects to see what is actually running.`
		);
	}

	bridge.verified = true;
}

async function callBridge(method, payload) {
	const bridge = await resolveBridge();
	await verifyBridge(bridge);

	let res;
	try {
		res = await fetch(bridge.url, {
			method: "POST",
			headers: { "content-type": "application/json" },
			body: JSON.stringify(payload === undefined ? { method } : { method, payload }),
		});
	} catch (e) {
		// Forgotten so the next call re-resolves: the editor may have restarted on a different port, and a
		// stale announcement would otherwise keep us pointed at nothing.
		throw unreachable(bridge, e.message);
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
	let bridge = null;
	try {
		bridge = await resolveBridge();
		const res = await fetch(bridge.url, {
			method: "POST",
			headers: { "content-type": "application/json" },
			body: JSON.stringify({ method }),
		});
		if (!res.ok) return null;
		return JSON.parse(await res.text());
	} catch {
		// Forgotten here too. This is the polling path used while the editor reloads, and the bridge removes
		// its announcement while it is down — so the next poll should re-resolve rather than keep asking a
		// port the editor may no longer hold.
		if (bridge)
			forget(bridge);
		return null;
	}
}

// Whether a status payload means "do not start anything heavy". Prefers busyWith, because that carries the
// bridge's hysteresis: a compile or import is a CHAIN of phases with brief quiet gaps between them, and
// compiling/updating sampled on their own read false in those gaps. Believing such a gap is how a caller
// declares an operation finished mid-chain and fires the next one into it. Falls back to the raw flags for
// a bridge older than v-00-05-187, which is then only as good as it can be.
function isEditorBusy(status) {
	if (!status) return false;
	if ("busyWith" in status) return status.busyWith !== null && status.busyWith !== undefined;
	return Boolean(status.compiling || status.updating);
}

function ok(text) {
	return { content: [{ type: "text", text }] };
}

function fail(error) {
	return { content: [{ type: "text", text: String(error?.message ?? error) }], isError: true };
}

const server = new McpServer({ name: "ui-toolkit", version: "0.1.0" });

const PROJECT_ARG = z.string().optional().describe(
	"Which Unity project to act on — a full project path, or just its folder name. Defaults to the project " +
	"this server was started for, so leave it out for ordinary work. Give it to reach ANOTHER editor that " +
	"has a bridge running: several can run at once, one per project. Use list_projects to see them. Most " +
	"useful for working on the toolkit itself, where a change can be tried in the toolkit's own dev app " +
	"without switching the client's package reference back and forth.");

/**
 * Registers a tool with the project selector added, and puts the chosen target into async context for the
 * duration of the call. Done here rather than in each tool so no tool can be forgotten — and as an argument
 * rather than a switchable session state, so which project a call touches stays readable from the call.
 */
function tool(name, description, schema, handler) {
	server.tool(name, description, { ...schema, project: PROJECT_ARG }, (args, extra) =>
		g_callContext.run({ project: args?.project ?? null }, () => handler(args, extra)));
}

tool(
	"list_projects",
	"List every Unity project on this machine with a running toolkit bridge, so one session can work across " +
	"several editors. Returns { projects: [{ projectPath, projectName, port, unityVersion, alive, isDefault, " +
	"startedAtUtc }], registryDir }. Pass a 'projectName' or 'projectPath' from here as the 'project' " +
	"argument of any other tool to act on that editor instead of the default one. 'alive:false' means the " +
	"announcement is stale — that editor is gone, or is mid domain-reload. Especially useful when working on " +
	"the toolkit itself: its own dev app can compile and test a C# change directly, with no need to point a " +
	"client's package reference at the working copy and back.",
	{},
	async () => {
		try {
			const entries = await listRegistryEntries();

			// The launch project may not be in the registry (older bridge, or an unwritable registry), and it is
			// the one that matters most — so make sure it is listed either way.
			let launch = null;
			try { launch = (await findDiscovery()).info; } catch { /* none running here */ }
			if (launch && !entries.some((e) => sameProject(e.projectPath, launch.projectPath)))
				entries.push(launch);

			const projects = [];
			for (const e of entries) {
				const url = e.url ?? `http://127.0.0.1:${e.port}/`;
				let alive = false;
				try {
					const res = await fetch(url, {
						method: "POST",
						headers: { "content-type": "application/json" },
						body: JSON.stringify({ method: "ping" }),
					});
					alive = res.ok;
				} catch { /* stale */ }

				projects.push({
					projectPath: normalisePath(e.projectPath),
					projectName: e.projectName ?? normalisePath(e.projectPath).split("/").pop(),
					productName: e.productName ?? null,
					port: e.port ?? null,
					unityVersion: e.unityVersion ?? null,
					startedAtUtc: e.startedAtUtc ?? null,
					isDefault: launch ? sameProject(e.projectPath, launch.projectPath) : false,
					alive,
				});
			}

			projects.sort((a, b) => Number(b.isDefault) - Number(a.isDefault) ||
				a.projectName.localeCompare(b.projectName));
			return ok(JSON.stringify({ projects, registryDir: REGISTRY_DIR }));
		} catch (e) {
			return fail(e);
		}
	}
);

tool(
	"ping",
	"Check that the Unity Editor GUI Toolkit bridge is reachable.",
	{},
	async () => {
		try { return ok(await callBridge("ping")); }
		catch (e) { return fail(e); }
	}
);

tool(
	"status",
	"What the editor is doing AND what it currently holds open — check it before anything heavy or anything " +
	"that writes an asset. Returns { running, compiling, updating, isPlaying, hasFocus, openScenes:[{path, " +
	"name, isDirty, isLoaded}], prefabStage:{path, isDirty}|null, busyWith:'compiling'|'importing'|null, " +
	"busySinceSeconds, projectPath, port }.\n\n" +
	"Two things here are easy to get wrong from outside. 'busyWith' with 'busySinceSeconds' distinguishes an " +
	"import that just started from one that has been running for a minute — fire a second heavy request into " +
	"either and it is refused, so poll until busyWith is null instead. 'openScenes' and 'prefabStage' say which " +
	"files the editor OWNS right now: rewriting one of those from a text tool loses against the in-memory copy, " +
	"and a scene reverted underneath the editor is how a session ends up needing a restart.",
	{},
	async () => {
		try { return ok(await callBridge("status")); }
		catch (e) { return fail(e); }
	}
);

tool(
	"asset_state",
	"Pre-flight check for writing or saving specific assets — ask this instead of finding out afterwards. " +
	"Per path: { path, kind, exists, openInEditor, inPrefabStage, dirty, missingScripts:{count, gameObjects[]}, " +
	"safeToWriteFromOutside, savableByUnity, why }.\n\n" +
	"It answers two separate questions that are easy to conflate. 'safeToWriteFromOutside' is false when the " +
	"editor has the file open — write it through the editor or ask for it to be closed, because otherwise the " +
	"in-memory copy wins and your write is silently undone. 'savableByUnity' is false when the asset contains " +
	"components whose script cannot be loaded: Unity then REFUSES to save it and logs one error per offending " +
	"GameObject, which is how one prefab produces hundreds of console errors. Checking first turns that " +
	"avalanche into an answer.",
	{
		paths: z.array(z.string()).min(1).describe(
			"Project-relative asset paths (prefabs, scenes, anything else). Missing-script detection applies " +
			"to prefabs."),
	},
	async ({ paths }) => {
		try { return ok(await callBridge("assetState", JSON.stringify({ paths }))); }
		catch (e) { return fail(e); }
	}
);

tool(
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

tool(
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
				if (isEditorBusy(st)) {
					sawActivity = true;
					await sleep(1000);
					continue;
				}
				// Idle — and with a hysteresis-aware bridge, idle that has HELD, not a gap between phases.
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

tool(
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

tool(
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

tool(
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

tool(
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
			const NO_ACTIVITY_GRACE_MS = 120000;
			const t0 = Date.now();
			let sawActivity = false;
			let reloaded = false;

			while (Date.now() - t0 < TIMEOUT_MS) {
				const st = await tryBridge("status");
				if (st === null) {
					// Bridge unreachable: the import/domain-reload window. Keep waiting.
					sawActivity = true;
					reloaded = true;
					await sleep(2000);
					continue;
				}
				// resolvePending covers the window between "triggered" and "observably started", which is the
				// one that looks exactly like idle; isEditorBusy covers the gaps between the resolve's phases.
				if (st.resolvePending || isEditorBusy(st)) {
					if (isEditorBusy(st))
						sawActivity = true;
					await sleep(2000);
					continue;
				}
				if (sawActivity)
					return ok(JSON.stringify({ resolved: true, reloaded, ms: Date.now() - t0 }));
				// Never saw it start. On an old bridge (no resolvePending) this is the only stopping condition,
				// so keep it generous: 15s used to declare "already up to date" on a resolve that had simply
				// not begun yet, and the caller then worked straight into the import.
				if (Date.now() - t0 > NO_ACTIVITY_GRACE_MS)
					return ok(JSON.stringify({ resolved: true, reloaded, note: "no import activity detected — the manifest may already have been up to date", ms: Date.now() - t0 }));
				await sleep(2000);
			}
			return ok(JSON.stringify({ resolved: false, reloaded, note: "timed out waiting for the editor to go idle", ms: Date.now() - t0 }));
		} catch (e) {
			return fail(e);
		}
	}
);

tool(
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

tool(
	"apply_prefab_values",
	"Restore values from a capture_prefab_values snapshot into a prefab that has since been re-baked — the " +
	"second half of: capture -> adapt the description -> bake_screen -> apply. It compares the snapshot " +
	"against the prefab AS IT IS NOW, so everything the bake already reproduced is left alone and only the " +
	"RESIDUE is a candidate: exactly the edits the authoring vocabulary cannot express. DRY RUN IS THE " +
	"DEFAULT and you should keep it for the first call: a difference can equally mean 'a human edit the " +
	"description cannot carry' (restore it) or 'a change you deliberately made to the description' (do NOT " +
	"restore it, you would undo your own decision), and no comparison can tell those apart — only you can, " +
	"because you wrote the description. So: dry run, read the plan, then apply with 'include' narrowed to the " +
	"entries you actually meant. Object references are REPORTED, never written: wiring belongs to the " +
	"description via \"#id\". Read 'propertyHistogram' as a roadmap — a property that keeps needing a restore " +
	"is a gap worth closing in the baker itself. The full plan is written to 'reportPath'; only the first " +
	"entries travel back inline. Writing is refused outright while any script in the prefab fails to load, " +
	"since saving would delete those components for good.",
	{
		path: z.string().describe("Project-relative prefab path, the same one that was captured and re-baked."),
		dryRun: z.boolean().optional().describe(
			"Default true: report the plan and write nothing. Pass false only after reading a dry-run plan."),
		include: z.array(z.string()).optional().describe(
			"Case-insensitive substrings matched against node path, component type or property path. Use this to " +
			"apply the reviewed part of a plan, e.g. ['m_fontSize'] or ['progressCount']."),
		snapshotPath: z.string().optional().describe(
			"Defaults to the snapshot capture_prefab_values wrote for this prefab."),
	},
	async ({ path, dryRun, include, snapshotPath }) => {
		try {
			return ok(await callBridge("applyPrefabValues", JSON.stringify({ path, dryRun, include, snapshotPath })));
		}
		catch (e) { return fail(e); }
	}
);

tool(
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

tool(
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

tool(
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

tool(
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

tool(
	"screenshot_motion",
	"See an ANIMATION, not just a state: renders the prefab at several points along an animation's timeline " +
	"and returns them composed into one contact sheet, read left to right, top to bottom. Each cell carries a " +
	"progress strip at its bottom edge showing where in the timeline it sits. Edit Mode only, no Play Mode. " +
	"Use it after authoring or changing any UiSimpleAnimation. What it reliably catches: nothing moves at all " +
	"(usually an unwired target, or no master animation driving a slave), the wrong node moves, an overshoot " +
	"that leaves the panel clipped, and a curve that ends anywhere other than its end value — which leaves " +
	"the element permanently wrong, not just briefly. What it CANNOT tell you is whether the motion feels " +
	"good: timing and easing are human judgements, so prefer curves already used elsewhere in the project " +
	"over invented ones, and when a new curve matters, offer a couple of candidates and let a human pick. " +
	"Defaults to the animation on the prefab root, which is the one UiPanel plays on open and close; pass " +
	"'animationNode' for any other. Duration is measured, so it includes slave animations and their delays.",
	{
		path: z.string().describe("Project-relative path of the baked prefab."),
		animationNode: z.string().optional().describe(
			"Node path of the animation to play, e.g. 'clickCatcher' or 'animated/Panel/buttonBand/claimAll/" +
			"WiggleAnimation'. Omit for the root's animation. The error message lists what a prefab contains."),
		frames: z.number().int().optional().describe("Samples along the timeline, 2-12 (default 5)."),
		width: z.number().int().positive().optional().describe("Width of ONE frame in px (default 640)."),
		height: z.number().int().positive().optional().describe("Height of ONE frame in px (default 360)."),
		backwards: z.boolean().optional().describe(
			"Play in reverse — how a panel closes. Default false."),
		populate: z.object({
			container: z.string().describe("Node path of the container, e.g. 'animated/Panel/cardBand/cardStrip'."),
			prefab: z.string().describe("Project-relative path of the row prefab to instantiate."),
			count: z.number().int().optional().describe("How many rows, 1-32 (default 3)."),
		}).optional().describe(
			"Fill a container with rows before filming. REQUIRED to see a staggered list: a screen that spawns " +
			"its rows at runtime holds none as an asset, so the entrance has nothing to collect and films as an " +
			"empty container — the animation looks absent when it is merely unpopulated. Rows exist only in the " +
			"throw-away preview scene; the asset is never touched."),
	},
	async ({ path, animationNode, frames, width, height, backwards, populate }) => {
		try {
			const payload = JSON.stringify({ path, animationNode, frames, width, height, backwards, populate });
			const result = JSON.parse(await callBridge("screenshotMotion", payload));
			if (!result.png)
				throw new Error("Bridge returned no image data.");
			const { png, ...meta } = result;
			return {
				content: [
					{ type: "image", data: png, mimeType: "image/png" },
					{ type: "text", text: JSON.stringify(meta) },
				],
			};
		} catch (e) {
			return fail(e);
		}
	}
);

tool(
	"play_mode",
	"Query, enter or leave Play Mode. 'action': 'status' (default), 'enter', 'exit'. Entering or leaving " +
	"reloads the domain, which stops and restarts this bridge — so the call returns before the change has " +
	"happened, and you must poll with 'status' until 'isPlaying' matches what you asked for. Expect the bridge " +
	"to be unreachable for a few seconds in between; that is normal, not a failure. Note that entering Play " +
	"Mode starts the app at its FIRST scene, which for a real game usually means splash, login and network — " +
	"reaching a particular screen from there is often not something you can drive. The productive pattern is " +
	"usually the other way round: ask the human to bring the app to the state in question, then use " +
	"screenshot_game and probe_ui on it.",
	{
		action: z.enum(["status", "enter", "exit"]).optional().describe("Default 'status'."),
	},
	async ({ action }) => {
		try { return ok(await callBridge("playMode", JSON.stringify({ action: action ?? "status" }))); }
		catch (e) { return fail(e); }
	}
);

tool(
	"screenshot_game",
	"Capture the Game View of the RUNNING app — real data, real resolution, real state. This is what the " +
	"Edit-Mode tools cannot substitute: screenshot_view shows what was authored and screenshot_motion shows " +
	"how it moves, but neither shows a screen the server filled in after a user navigated to it. Requires Play " +
	"Mode and an open Game View window; a closed or fully occluded one renders no frames and the capture will " +
	"time out. Also fails clearly while paused, since no further frame is produced. Returns the image plus the " +
	"file it was also written to under Library/.",
	{
		superSize: z.number().int().optional().describe(
			"Resolution multiplier, 1-4 (default 1). Above 1 costs render time and tokens; prefer 1."),
	},
	async ({ superSize }) => {
		try {
			await callBridge("screenshotGame", JSON.stringify({ action: "start", superSize }));

			// Polled from HERE rather than waited for inside the editor: a handler that waits blocks the main
			// thread, which is the thread that has to render the frame the capture is waiting for.
			for (let attempt = 0; attempt < 80; attempt++) {
				await sleep(150);
				const result = JSON.parse(await callBridge("screenshotGame", JSON.stringify({ action: "fetch" })));
				if (result.png) {
					const { png, ...meta } = result;
					return {
						content: [
							{ type: "image", data: png, mimeType: "image/png" },
							{ type: "text", text: JSON.stringify(meta) },
						],
					};
				}
			}
			throw new Error(
				"The capture never completed. Is a Game View window open and visible? A Game View that is closed, " +
				"or on a hidden tab, renders no frames."
			);
		} catch (e) { return fail(e); }
	}
);

tool(
	"probe_ui",
	"Ask the running UI what a tap would actually hit, and optionally perform it. This answers what no " +
	"screenshot can: whether a button is REACHABLE. A full-rect frame overlay with raycastTarget left on " +
	"swallows every click beneath it while looking perfectly correct — so the answer is the whole raycast hit " +
	"stack, topmost first. Name a 'target' (node name, or a path ending in one) and it is aimed at its centre, " +
	"reporting 'targetReceivesInput' and, when something is in the way, 'blockedBy'. With 'click' the tap goes " +
	"THROUGH the raycast as down/up/click on whatever is actually on top — deliberately not by invoking the " +
	"button's event directly, because that would bypass the very thing worth testing. 'handledBy' names the " +
	"node whose handler ran, or null if the click landed somewhere that ignores it. Requires Play Mode.",
	{
		target: z.string().optional().describe(
			"Node to aim at, e.g. 'claimAll' or 'Panel/buttonBand/claimAll'. Ambiguous names are an error " +
			"listing the candidates."),
		x: z.number().optional().describe("Screen X, if aiming by position instead of target."),
		y: z.number().optional().describe("Screen Y. Top-left origin by default, matching a captured image."),
		origin: z.enum(["topLeft", "bottomLeft"]).optional().describe(
			"Origin for x/y. Default 'topLeft' because that is how screenshots read; Unity's own is bottomLeft."),
		click: z.boolean().optional().describe(
			"Perform the tap as well as reporting it. Default false — look first."),
	},
	async ({ target, x, y, origin, click }) => {
		try {
			return ok(await callBridge("probeUi", JSON.stringify({ target, x, y, origin, click })));
		} catch (e) { return fail(e); }
	}
);

tool(
	"harvest_motion",
	"Read this project's EXISTING animations out of its prefabs and group them by how the motion looks, most " +
	"used first. Call it before authoring any animation: a shape that many prefabs share has already had the " +
	"one judgement you cannot make — whether it FEELS right — applied by whoever watched it, so reusing it " +
	"beats inventing curves whose tangents you can only guess at. Returns { shapes: [{ type, count, summary, " +
	"values, examples }] }, where 'summary' is a one-line reading (channels, duration, stagger, and ↗ for a " +
	"curve that overshoots) and 'values' is the full field set ready to copy into a bake. Targets, slaves and " +
	"callbacks are excluded from the grouping, so two animations differing only in what they drive count as " +
	"one shape. 'examples' names prefabs and nodes to look at — pair it with screenshot_motion.",
	{
		folders: z.array(z.string()).optional().describe(
			"Project-relative folders to scan (default ['Assets'])."),
		minOccurrences: z.number().int().optional().describe(
			"Only report shapes used at least this often (default 2). Set 1 to see one-offs too."),
		maxExamples: z.number().int().optional().describe("Example locations per shape (default 3)."),
	},
	async ({ folders, minOccurrences, maxExamples }) => {
		try {
			return ok(await callBridge("harvestMotion", JSON.stringify({ folders, minOccurrences, maxExamples })));
		} catch (e) { return fail(e); }
	}
);

tool(
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

tool(
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

tool(
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
