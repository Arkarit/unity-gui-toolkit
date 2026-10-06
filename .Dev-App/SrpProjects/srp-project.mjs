#!/usr/bin/env node
// Test projects for the render pipelines the 3D icons support (URP, HDRP), next to the Built-in dev app.
//
//   node srp-project.mjs create <urp|hdrp> <targetDir> [unityVersion]   create (once) and set up the project
//   node srp-project.mjs sync   <targetDir>                             copy the 3D icon tests into it again
//   node srp-project.mjs test   <targetDir> [groupRegex]                run them in batch mode (editor must be closed)
//
// The project references this repository as a package ("file:"), so a change here is a change there. The tests
// are the dev app's own (Tests/PlayMode/Icon3DTestModels.cs, TestIcon3D*.cs, TestUiIcon3D.cs), copied, not forked:
// run "sync" after editing them. Results of "test" end up in <targetDir>/test-results.xml.
import { spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const repo = path.resolve(here, "..", "..");
const devTests = path.join(repo, ".Dev-App", "Unity", "Assets", "Tests", "PlayMode");
const DEFAULT_VERSION = "2022.3.62f2";   // the package's target version

const PIPELINES = {
	// version per Unity generation: 2022.3 -> 14.x, Unity 6 -> 17.x
	urp: { package: "com.unity.render-pipelines.universal", versions: { 2022: "14.0.12", 6000: "17.0.4" }, setup: "SetupUrp" },
	hdrp: { package: "com.unity.render-pipelines.high-definition", versions: { 2022: "14.0.12", 6000: "17.0.4" }, setup: "SetupHdrp" },
};

function unityExe( version ) {
	const candidates = [
		`C:/Program Files/Unity/Hub/Editor/${version}/Editor/Unity.exe`,
		`/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity`,
		`${process.env.HOME}/Unity/Hub/Editor/${version}/Editor/Unity`,
	];
	const found = candidates.find((c) => fs.existsSync(c));
	if (!found) throw new Error(`Unity ${version} not found (looked in the default Hub locations)`);
	return found;
}

function run( version, project, args, logFile ) {
	const log = path.join(project, logFile);
	const result = spawnSync(unityExe(version), ["-batchmode", "-projectPath", project, "-logFile", log, ...args], { stdio: "inherit" });
	restoreMinimalMetas();
	return { code: result.status, log };
}

// The test project holds this repository as a local package, so its Unity writes importer settings into the
// repository's minimal .meta files (two lines: version and guid). Put those back; anything else is left alone.
function restoreMinimalMetas() {
	const git = ( ...args ) => spawnSync("git", ["-C", repo, ...args], { encoding: "utf8" });
	const changed = git("diff", "--name-only", "--", "*.meta").stdout.split("\n").filter(Boolean);
	let restored = 0;
	for (const file of changed) {
		const head = git("show", "HEAD:" + file).stdout.replace(/\r\n/g, "\n").trimEnd();
		const now = fs.readFileSync(path.join(repo, file), "utf8").replace(/\r\n/g, "\n");
		if (head.split("\n").length <= 2 && now.startsWith(head)) {
			git("checkout", "--", file);
			restored++;
		}
	}
	if (restored) console.log(`restored ${restored} .meta files that Unity had expanded`);
}

function projectVersion( project ) {
	const text = fs.readFileSync(path.join(project, "ProjectSettings", "ProjectVersion.txt"), "utf8");
	return /m_EditorVersion:\s*(\S+)/.exec(text)[1];
}

function writeManifest( project, pipeline, unityVersion ) {
	const file = path.join(project, "Packages", "manifest.json");
	const manifest = JSON.parse(fs.readFileSync(file, "utf8"));
	Object.assign(manifest.dependencies, {
		[pipeline.package]: pipeline.versions[unityVersion.startsWith("6000") ? 6000 : 2022],
		"com.unity.test-framework": "1.1.33",
		"com.unity.nuget.newtonsoft-json": "3.2.1",
		"com.unity.ugui": "1.0.0",
		"com.unity.textmeshpro": "3.0.9",
		"de.phoenixgrafik.ui-toolkit": "file:" + repo.replace(/\\/g, "/"),
	});
	fs.writeFileSync(file, JSON.stringify(manifest, null, 2));
}

function sync( project ) {
	const target = path.join(project, "Assets", "Tests", "Icon3D");
	fs.mkdirSync(target, { recursive: true });

	// The test assembly must carry this name: the toolkit lets exactly it see its internals
	fs.writeFileSync(path.join(target, "de.phoenixgrafik.ui-toolkit.Test.PlayMode.asmdef"), JSON.stringify({
		name: "de.phoenixgrafik.ui-toolkit.Test.PlayMode",
		rootNamespace: "",
		references: ["de.phoenixgrafik.ui-toolkit", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
		includePlatforms: [],
		excludePlatforms: [],
		allowUnsafeCode: false,
		overrideReferences: true,
		precompiledReferences: ["nunit.framework.dll"],
		autoReferenced: false,
		defineConstraints: ["UNITY_INCLUDE_TESTS"],
		versionDefines: [],
		noEngineReferences: false,
	}, null, 4));

	for (const old of fs.readdirSync(target)) if (old.endsWith(".cs")) fs.rmSync(path.join(target, old));
	let count = 0;
	for (const file of fs.readdirSync(devTests)) {
		if (file.endsWith(".cs") && (file === "Icon3DTestModels.cs" || /^TestIcon3D/.test(file) || file === "TestUiIcon3D.cs")) {
			fs.copyFileSync(path.join(devTests, file), path.join(target, file));
			count++;
		}
	}
	console.log(`copied ${count} test files into ${target}`);
}

function summarise( file ) {
	if (!fs.existsSync(file)) { console.log("no results file - see test.log"); return; }
	const xml = fs.readFileSync(file, "utf8");
	const run = /<test-run[^>]*total="(\d+)"[^>]*passed="(\d+)"[^>]*failed="(\d+)"/.exec(xml);
	if (run) console.log(`total ${run[1]}, passed ${run[2]}, failed ${run[3]}`);
	const failure = /<test-case[^>]*name="([^"]*)"[^>]*result="Failed"[\s\S]*?<message><!\[CDATA\[([\s\S]*?)\]\]>/g;
	let m;
	while ((m = failure.exec(xml))) console.log("FAILED", m[1].replace(/^.*\./, ""), "|", m[2].replace(/\s+/g, " ").slice(0, 200));
}

const [command, a, b, c] = process.argv.slice(2);

if (command === "create") {
	const pipelineName = a;
	const project = path.resolve(b);
	const version = c ?? DEFAULT_VERSION;
	const pipeline = PIPELINES[pipelineName];
	if (!pipeline) throw new Error("pipeline must be urp or hdrp");

	if (!fs.existsSync(path.join(project, "ProjectSettings"))) {
		fs.mkdirSync(path.dirname(project), { recursive: true });
		const r = spawnSync(unityExe(version), ["-batchmode", "-nographics", "-quit", "-createProject", project, "-logFile", path.join(path.dirname(project), "create.log")], { stdio: "inherit" });
		if (r.status !== 0) throw new Error("creating the project failed, see create.log");
	}

	writeManifest(project, pipeline, projectVersion(project));
	sync(project);
	fs.mkdirSync(path.join(project, "Assets", "Editor"), { recursive: true });
	fs.copyFileSync(path.join(here, `${pipeline.setup}.cs`), path.join(project, "Assets", "Editor", `${pipeline.setup}.cs`));

	// First open: resolves the packages, imports, then sets the pipeline up
	const r = run(projectVersion(project), project, ["-nographics", "-quit", "-executeMethod", `${pipeline.setup}.Run`], "setup.log");
	console.log(`setup exited with ${r.code}; log: ${r.log}`);
	process.exit(r.code ?? 1);
} else if (command === "sync") {
	sync(path.resolve(a));
} else if (command === "test") {
	const project = path.resolve(a);
	// The skinned test models are assets written in edit mode (they lose references when written in a running test)
	run(projectVersion(project), project, ["-nographics", "-quit", "-executeMethod", "GuiToolkit.Test.Icon3DTestModels.EnsureAssets"], "models.log");
	const args = ["-runTests", "-testPlatform", "PlayMode", "-testResults", path.join(project, "test-results.xml")];
	if (b) args.push("-testFilter", b);
	const r = run(projectVersion(project), project, args, "test.log");
	summarise(path.join(project, "test-results.xml"));
	console.log(`tests exited with ${r.code}`);
	process.exit(r.code ?? 1);
} else {
	console.log(fs.readFileSync(fileURLToPath(import.meta.url), "utf8").split("\n").slice(1, 9).join("\n"));
}
