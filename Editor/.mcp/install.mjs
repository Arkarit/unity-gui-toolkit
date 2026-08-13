#!/usr/bin/env node
/*
 * Installs this package's agent tooling into a consuming project.
 *
 * WHAT IT SOLVES
 * A consumer gets this package from a git URL, so it lands in Library/PackageCache under a HASHED
 * folder name that changes on every version bump -- and `mcp~/node_modules` is gitignored, so the
 * cached copy has no dependencies and could not be npm-installed durably anyway. Everything the
 * README used to describe as a manual recipe ("write .mcp.json with the absolute path, run npm
 * install, ...") is therefore done here, against paths resolved at install time.
 *
 * WHY NODE AND NOT C#
 * Node is already a hard requirement for the proxy, `.mcp.json` needs a real JSON merge (the file
 * holds other people's servers and must never be clobbered), and this way the installer also runs
 * from a plain terminal without opening Unity. The Unity menu item is a thin front end over this.
 *
 * TWO MODES
 *   dev   .mcp.json points straight at this package's mcp~/server.mjs. For anyone who has the
 *         library checked out and edits it: a change to the server is live after reconnecting the
 *         MCP client, with no reinstall. Chosen automatically when the package is not in a cache.
 *   copy  mcp~ is copied into the project and npm-installed there. For consumers, whose cached
 *         package copy would be wiped by the next version bump. Pins proxy and Editor bridge to the
 *         same package version.
 *
 * Everything written is recorded in a manifest, and uninstall removes exactly that -- never a guess
 * at what "probably belongs to us". Files the user changed afterwards are left alone unless forced.
 */

import { createHash } from 'node:crypto';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import {
	copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync, writeFileSync,
} from 'node:fs';
import { basename, dirname, join, relative, resolve, sep } from 'node:path';

const HERE         = dirname(fileURLToPath(import.meta.url));   // <package>/Editor/.mcp
const PACKAGE_ROOT = resolve(HERE, '..', '..');                 // <package>

const MANIFEST_NAME    = '.uitoolkit-agent-tools.json';
const MANIFEST_VERSION = 1;
const MCP_COPY_DIR     = '.uitoolkit-mcp';
const MIN_NODE_MAJOR   = 18;

// Copied verbatim from mcp~. node_modules is deliberately absent (it is installed in place) and so
// is prototypes/, which is development scratch rather than part of the server.
const MCP_FILES = ['server.mjs', 'package.json', 'package-lock.json', 'README.md'];

/* ------------------------------------------------------------------ tiny helpers */

const forward = (p) => p.split(sep).join('/');

let quiet = false;
const say  = (msg) => { if (!quiet) process.stdout.write(`${msg}\n`); };
const warn = (msg) => process.stderr.write(`${msg}\n`);

function fail(msg) {
	warn(`ERROR: ${msg}`);
	process.exit(1);
}

function sha256(file) {
	return createHash('sha256').update(readFileSync(file)).digest('hex');
}

function readJson(file) {
	try {
		return JSON.parse(readFileSync(file, 'utf8'));
	} catch (err) {
		fail(`${file} is not valid JSON: ${err.message}`);
	}
}

function writeJson(file, value) {
	mkdirSync(dirname(file), { recursive: true });
	writeFileSync(file, `${JSON.stringify(value, null, 2)}\n`, 'utf8');
}

function copyInto(from, to) {
	mkdirSync(dirname(to), { recursive: true });
	copyFileSync(from, to);
}

/* ------------------------------------------------------------------ arguments */

function parseArgs(argv) {
	const args = { command: 'status', flags: {} };
	const rest = [];

	for (let i = 0; i < argv.length; i++) {
		const a = argv[i];
		if (!a.startsWith('--')) { rest.push(a); continue; }
		const key = a.slice(2);
		// Boolean flags take no value; everything else consumes the next token.
		if (['force', 'quiet', 'codex', 'no-codex', 'json'].includes(key)) { args.flags[key] = true; continue; }
		args.flags[key] = argv[++i];
	}
	if (rest.length) args.command = rest[0];
	return args;
}

/* ------------------------------------------------------------------ discovery */

// The project root is where .mcp.json belongs: the repo, which may CONTAIN a Unity project rather
// than being one (this is the documented nested-layout case, and it is the usual one here).
function resolveProjectRoot(given) {
	const start = resolve(given ?? process.cwd());
	if (!existsSync(start)) fail(`Project path does not exist: ${start}`);

	// A .mcp.json or a .git marks the root; otherwise take the path as given.
	for (let dir = start; ; dir = dirname(dir)) {
		if (existsSync(join(dir, '.mcp.json')) || existsSync(join(dir, '.git'))) return dir;
		if (dirname(dir) === dir) return start;
	}
}

// Same reasoning as the proxy's own lookup: upwards first, then a bounded scan downwards. Several
// candidates are an error rather than a guess -- picking the wrong one silently is how a tool ends
// up baking into somebody else's project.
function detectUnityProject(projectRoot, given) {
	if (given) {
		const p = resolve(given);
		if (!existsSync(join(p, 'ProjectSettings', 'ProjectVersion.txt'))) {
			fail(`Not a Unity project (no ProjectSettings/ProjectVersion.txt): ${p}`);
		}
		return p;
	}

	const isUnity = (d) => existsSync(join(d, 'ProjectSettings', 'ProjectVersion.txt'));
	if (isUnity(projectRoot)) return projectRoot;

	const found = [];
	const scan = (dir, depth) => {
		if (depth > 2) return;
		for (const entry of readdirSync(dir, { withFileTypes: true })) {
			if (!entry.isDirectory()) continue;
			if (entry.name.startsWith('.') || entry.name === 'node_modules') continue;
			const child = join(dir, entry.name);
			if (isUnity(child)) { found.push(child); continue; }
			scan(child, depth + 1);
		}
	};
	scan(projectRoot, 0);

	if (found.length === 1) return found[0];
	if (found.length === 0) fail(`No Unity project found under ${projectRoot}. Pass --unity-project <path>.`);
	fail(`Several Unity projects under ${projectRoot}:\n  ${found.join('\n  ')}\nPass --unity-project <path>.`);
}

// A package resolved from a git URL or the registry sits in a cache Unity rewrites on every version
// bump; anything installed into it is lost. An embedded or local-path package is the developer's own
// working tree, and pointing at it directly is what keeps library edits live.
function isCachedPackage(packageRoot) {
	const p = forward(packageRoot);
	return p.includes('/Library/PackageCache/') || p.includes('/Library/PackageCache@');
}

function nodeMajor() {
	return Number.parseInt(process.versions.node.split('.')[0], 10);
}

function haveCodexCli() {
	// Whole command in one string, no args array: on Windows `codex` is a .cmd shim and needs a shell,
	// but passing an args array alongside shell:true is deprecated (Node warns about unescaped
	// concatenation). Nothing user-supplied goes in here.
	const probe = spawnSync('codex --version', { shell: true, encoding: 'utf8' });
	return probe.status === 0;
}

/* ------------------------------------------------------------------ manifest */

const manifestPath = (projectRoot) => join(projectRoot, MANIFEST_NAME);

function loadManifest(projectRoot) {
	const file = manifestPath(projectRoot);
	if (!existsSync(file)) return null;
	const m = readJson(file);
	if (m.manifestVersion !== MANIFEST_VERSION) {
		warn(`Note: manifest was written by another installer version (${m.manifestVersion}).`);
	}
	return m;
}

/* ------------------------------------------------------------------ .mcp.json */

// Merge, never overwrite: this file routinely holds servers we know nothing about (a project-local
// DAL bridge, for instance), and losing one would be a silent, hard-to-spot breakage.
function mergeMcpJson(projectRoot, entries) {
	const file = join(projectRoot, '.mcp.json');
	const doc  = existsSync(file) ? readJson(file) : {};
	if (!doc.mcpServers || typeof doc.mcpServers !== 'object') doc.mcpServers = {};

	const touched = [];
	for (const [name, entry] of Object.entries(entries)) {
		doc.mcpServers[name] = entry;
		touched.push(name);
	}
	writeJson(file, doc);
	return touched;
}

function removeMcpEntries(projectRoot, names) {
	const file = join(projectRoot, '.mcp.json');
	if (!existsSync(file)) return [];
	const doc = readJson(file);
	if (!doc.mcpServers) return [];

	const removed = names.filter((n) => n in doc.mcpServers);
	for (const n of removed) delete doc.mcpServers[n];
	writeJson(file, doc);
	return removed;
}

/* ------------------------------------------------------------------ install */

function cmdInstall(args) {
	if (nodeMajor() < MIN_NODE_MAJOR) {
		fail(`Node ${MIN_NODE_MAJOR}+ required, found ${process.versions.node}.`);
	}

	const projectRoot  = resolveProjectRoot(args.flags.project);
	const unityProject = detectUnityProject(projectRoot, args.flags['unity-project']);
	const pkgVersion   = existsSync(join(PACKAGE_ROOT, 'package.json'))
		? readJson(join(PACKAGE_ROOT, 'package.json')).version : 'unknown';

	const mode = args.flags.mode ?? (isCachedPackage(PACKAGE_ROOT) ? 'copy' : 'dev');
	if (!['dev', 'copy'].includes(mode)) fail(`Unknown --mode '${mode}'. Use dev or copy.`);

	const withCodex = args.flags['no-codex'] ? false : (args.flags.codex ? true : haveCodexCli());

	say(`Package     ${forward(PACKAGE_ROOT)}  (v${pkgVersion})`);
	say(`Project     ${forward(projectRoot)}`);
	say(`Unity       ${forward(unityProject)}`);
	say(`Mode        ${mode}${args.flags.mode ? '' : ' (auto)'}`);
	say('');

	const manifest = {
		manifestVersion: MANIFEST_VERSION,
		installedAt: new Date().toISOString(),
		packageRoot: forward(PACKAGE_ROOT),
		packageVersion: pkgVersion,
		mode,
		unityProject: forward(unityProject),
		serverPath: null,
		files: [],
		directories: [],
		mcpServers: [],
		createdFiles: [],
	};

	/* -------- the MCP proxy itself */

	let serverPath;
	if (mode === 'dev') {
		serverPath = join(PACKAGE_ROOT, 'mcp~', 'server.mjs');
		if (!existsSync(serverPath)) fail(`mcp~/server.mjs missing from the package at ${PACKAGE_ROOT}`);
		if (!existsSync(join(PACKAGE_ROOT, 'mcp~', 'node_modules'))) {
			warn(`Note: ${forward(join(PACKAGE_ROOT, 'mcp~', 'node_modules'))} does not exist yet -- run 'npm install' there once.`);
		}
		say(`proxy       used in place (library checkout stays live)`);
	} else {
		const target = join(projectRoot, MCP_COPY_DIR);
		mkdirSync(target, { recursive: true });
		for (const name of MCP_FILES) {
			const from = join(PACKAGE_ROOT, 'mcp~', name);
			if (!existsSync(from)) continue;   // README/lock are nice to have, not required
			copyInto(from, join(target, name));
		}
		manifest.directories.push(MCP_COPY_DIR);
		serverPath = join(target, 'server.mjs');
		say(`proxy       copied to ${MCP_COPY_DIR}/`);

		say(`            npm install ...`);
		const npm = spawnSync('npm install --omit=dev --no-audit --no-fund',
			{ cwd: target, shell: true, encoding: 'utf8' });
		if (npm.status !== 0) {
			warn(npm.stderr || npm.stdout || '');
			fail(`npm install failed in ${forward(target)}. Fix that and re-run; nothing else was changed.`);
		}
		say(`            dependencies installed`);
	}
	manifest.serverPath = forward(serverPath);

	/* -------- payload: the PowerShell tooling */

	const payloadRoot = join(HERE, 'payload');
	if (existsSync(payloadRoot)) {
		const walk = (dir) => {
			for (const entry of readdirSync(dir, { withFileTypes: true })) {
				const from = join(dir, entry.name);
				if (entry.isDirectory()) { walk(from); continue; }
				const rel = relative(payloadRoot, from);
				copyInto(from, join(projectRoot, rel));
				manifest.files.push({ path: forward(rel), sha256: sha256(from) });
			}
		};
		walk(payloadRoot);
		say(`tools       ${manifest.files.length} file(s) copied`);
	}

	/* -------- registrations */

	const entries = {
		'ui-toolkit': { command: 'node', args: [forward(serverPath), '--project', forward(unityProject)] },
	};
	if (withCodex) {
		entries.codex = process.platform === 'win32'
			? { command: 'cmd', args: ['/c', 'codex', 'mcp-server'] }
			: { command: 'codex', args: ['mcp-server'] };
	}
	manifest.mcpServers = mergeMcpJson(projectRoot, entries);
	say(`.mcp.json   merged: ${manifest.mcpServers.join(', ')}`);

	/* -------- Codex's own config, which is project-scoped and therefore per project */

	if (withCodex) {
		const template = join(HERE, 'templates', 'codex-config.toml');
		const target   = join(projectRoot, '.codex', 'config.toml');

		if (!existsSync(template)) {
			warn('Note: templates/codex-config.toml missing from the package; skipped.');
		} else if (existsSync(target)) {
			// Never clobber a hand-tuned allow list. Offer the new one alongside instead -- but only when
			// it would actually say something different, or a routine re-install litters the project with
			// a .new that is byte-identical to the file it claims to improve on.
			const rendered = renderTemplate(template, serverPath, unityProject);
			const proposal = `${target}.new`;
			if (readFileSync(target, 'utf8') === rendered) {
				say('.codex      already up to date');
			} else {
				writeFileSync(proposal, rendered, 'utf8');
				say(`.codex      already exists -- wrote ${forward(relative(projectRoot, proposal))} for comparison`);
			}
		} else {
			mkdirSync(dirname(target), { recursive: true });
			writeFileSync(target, renderTemplate(template, serverPath, unityProject), 'utf8');
			manifest.createdFiles.push({ path: '.codex/config.toml', sha256: sha256(target) });
			say(`.codex      config.toml written`);
		}
	}

	writeJson(manifestPath(projectRoot), manifest);
	say('');
	say(`Manifest    ${MANIFEST_NAME}`);
	say('');
	say('Next:');
	say('  1. In Unity: Gui Toolkit > AI > Start MCP Bridge');
	say('  2. Restart your MCP client and approve the "ui-toolkit" server, then check /mcp');
	if (mode === 'copy') say(`  3. Consider gitignoring ${MCP_COPY_DIR}/ -- it contains node_modules`);
}

function renderTemplate(file, serverPath, unityProject) {
	return readFileSync(file, 'utf8')
		.split('{{SERVER_PATH}}').join(forward(serverPath))
		.split('{{UNITY_PROJECT}}').join(forward(unityProject));
}

/* ------------------------------------------------------------------ status */

function cmdStatus(args) {
	const projectRoot = resolveProjectRoot(args.flags.project);
	const manifest    = loadManifest(projectRoot);

	if (!manifest) {
		say(`Not installed in ${forward(projectRoot)}.`);
		say(`Run:  node "${forward(join(HERE, 'install.mjs'))}" install --project "${forward(projectRoot)}"`);
		return 1;
	}

	say(`Installed   ${manifest.installedAt}  (package v${manifest.packageVersion}, mode ${manifest.mode})`);
	say(`Source      ${manifest.packageRoot}`);
	say(`Proxy       ${manifest.serverPath}${existsSync(manifest.serverPath) ? '' : '   MISSING'}`);

	// Staleness is the whole reason this command exists: copies drift silently, and the point is to
	// be told rather than to find out through a bug.
	const payloadRoot = join(HERE, 'payload');
	let stale = 0, missing = 0, edited = 0;

	for (const rec of manifest.files) {
		const installed = join(projectRoot, rec.path);
		const source    = join(payloadRoot, rec.path);

		if (!existsSync(installed)) { say(`  missing   ${rec.path}`); missing++; continue; }

		const now = sha256(installed);
		if (now !== rec.sha256) { say(`  edited    ${rec.path}  (changed since install)`); edited++; }
		if (existsSync(source) && sha256(source) !== rec.sha256) { say(`  stale     ${rec.path}  (package has a newer version)`); stale++; }
	}

	const mcpFile = join(projectRoot, '.mcp.json');
	const servers = existsSync(mcpFile) ? Object.keys(readJson(mcpFile).mcpServers ?? {}) : [];
	for (const name of manifest.mcpServers) {
		if (!servers.includes(name)) say(`  missing   .mcp.json entry '${name}'`);
	}

	if (!stale && !missing && !edited) say(`  all ${manifest.files.length} file(s) match the package`);
	else if (stale) say(`\nRe-run install to update ${stale} stale file(s).`);

	return 0;
}

/* ------------------------------------------------------------------ uninstall */

function cmdUninstall(args) {
	const projectRoot = resolveProjectRoot(args.flags.project);
	const manifest    = loadManifest(projectRoot);
	const force       = Boolean(args.flags.force);

	if (!manifest) { say(`Nothing to uninstall in ${forward(projectRoot)} (no ${MANIFEST_NAME}).`); return 0; }

	let kept = 0;
	for (const rec of manifest.files) {
		const file = join(projectRoot, rec.path);
		if (!existsSync(file)) continue;
		// A file the user edited is theirs now; removing it would destroy work we never wrote.
		if (!force && sha256(file) !== rec.sha256) { say(`  kept      ${rec.path}  (edited since install)`); kept++; continue; }
		rmSync(file);
		say(`  removed   ${rec.path}`);
	}

	for (const rec of manifest.createdFiles ?? []) {
		const file = join(projectRoot, rec.path);
		if (!existsSync(file)) continue;
		if (!force && sha256(file) !== rec.sha256) { say(`  kept      ${rec.path}  (edited since install)`); kept++; continue; }
		rmSync(file);
		say(`  removed   ${rec.path}`);
	}

	for (const dir of manifest.directories ?? []) {
		const path = join(projectRoot, dir);
		if (!existsSync(path)) continue;
		rmSync(path, { recursive: true, force: true });
		say(`  removed   ${dir}/`);
	}

	const removed = removeMcpEntries(projectRoot, manifest.mcpServers ?? []);
	if (removed.length) say(`  .mcp.json entries removed: ${removed.join(', ')}`);

	rmSync(manifestPath(projectRoot));
	say(`  removed   ${MANIFEST_NAME}`);
	if (kept) say(`\n${kept} edited file(s) kept. Re-run with --force to remove them too.`);

	// Empty parent folders left behind by removed payload files are the user's to keep or delete;
	// deleting a 'tools' folder that also holds their own scripts would be a nasty surprise.
	return 0;
}

/* ------------------------------------------------------------------ main */

const args = parseArgs(process.argv.slice(2));
quiet = Boolean(args.flags.quiet);

switch (args.command) {
	case 'install':   cmdInstall(args); break;
	case 'status':    process.exitCode = cmdStatus(args); break;
	case 'uninstall': process.exitCode = cmdUninstall(args); break;
	default:
		say('Usage: node install.mjs <install|status|uninstall> [options]');
		say('');
		say('  --project <path>         Project root (default: current directory, walked up to a .git/.mcp.json)');
		say('  --unity-project <path>   Unity project, when the repo contains several or none');
		say('  --mode dev|copy          Override the automatic choice (cached package -> copy)');
		say('  --codex / --no-codex     Force Codex CLI wiring on or off (default: on if `codex` is on PATH)');
		say('  --force                  uninstall: also remove files edited since install');
		say('  --quiet                  Suppress progress output');
		process.exitCode = 2;
}
