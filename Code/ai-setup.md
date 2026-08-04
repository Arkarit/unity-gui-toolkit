---
layout: default
title: AI Support — Setup
---

# AI Support — Setup

Getting the AI bridge running takes about five minutes. Steps 1–4 are the install; step 5 is the one
people skip and then wonder why authored screens look wrong.

Examples use [Claude Code](https://claude.com/claude-code) as the MCP client. Any MCP client works —
only step 3 differs.

---

## Requirements

| Requirement | Details |
|---|---|
| **Node.js** | 18 or newer (`node --version`) |
| **Unity** | The same project you already use the toolkit in |
| **An MCP client** | Claude Code, or any other client that can launch a stdio MCP server |

---

## Step 1: Install the proxy dependencies

Once per checkout of the toolkit repository:

```bash
cd mcp~
npm install
```

> **Tip:** If you consume the toolkit as a UPM git package, this folder lives in your project's
> package cache and is replaced on every version change. Prefer a local clone of the toolkit
> repository for the proxy, and point your MCP client at that — the proxy talks to whatever editor
> answers, so it does not have to be the same copy as the package.

---

## Step 2: Start the Editor bridge

In Unity: **Gui Toolkit → AI → Start MCP Bridge**

It stays on across domain reloads until you pick **Stop MCP Bridge**, so this is a once-per-session
click at most. The console tells you which port it took.

---

## Step 3: Register the server with your client

The registration holds a **machine-specific absolute path**, so it is gitignored on purpose —
everyone generates their own.

**The easy way:** open Claude Code in the toolkit repository and ask:

> *"Set up the ui-toolkit MCP server for this repo."*

It knows the recipe, checks your Node version, runs `npm install` if needed, and writes the file.

**By hand:** create `.mcp.json` at the repository root:

```jsonc
{
  "mcpServers": {
    "ui-toolkit": {
      "command": "node",
      // Absolute path to THIS repo's mcp~/server.mjs. Forward slashes work on Windows too.
      "args": ["D:/dev/unity-gui-toolkit/mcp~/server.mjs"]
    }
  }
}
```

To use it from a **client project** instead of the toolkit repo, register it there with local scope so
no absolute path lands in a shared repository:

```bash
claude mcp add ui-toolkit --scope local -- node D:/dev/unity-gui-toolkit/mcp~/server.mjs
```

Then restart the client and approve the server.

---

## Step 4: Verify

In Claude Code, `/mcp` should list `ui-toolkit` with its tools. Then ask for the two cheapest calls:

> *"Ping the toolkit bridge and show me the status."*

You want to see the project path you expect. If it names a different project, that is the safety check
doing its job — see [Troubleshooting](#troubleshooting).

---

## Step 5: Check the project's health

This is the step that decides whether authored screens look like *your* project or like the raw
library. Ask:

> *"Run setup_status and tell me what's missing."*

It reports, in one call:

| Field | What it means |
|---|---|
| `registry.assigned` | A standard-element registry is set on the toolkit configuration |
| `registry.client` / `library` | How many identities come from **your** prefabs vs. the library's defaults |
| `prefabVariantsPath` | The folder that is scanned for your prefab variants — and whether it exists |
| `paletteConfig` | Extra folders/prefabs offered as building blocks |
| `catalog.ageMinutes` | How stale the vocabulary is |
| `missingStandardElements` | Identities nothing claims — these fall back to library defaults |
| `standardElementAmbiguities` | Identities **several** prefabs claim; the generator silently takes the alphabetically first |

The classic broken setup is `registry.client == 0` together with `prefabVariantsPath.exists == false`:
the scan folder points somewhere that does not exist, so nothing of yours is found and every screen
comes out in library colours.

Ambiguities are the subtler trap. Prefab **variants inherit their base's marker**, so a variant can
outrank the prefab you meant just by sorting earlier. The fix is to give the interloper its own custom
identity rather than removing its marker — that keeps it available as a building block.

---

## A second project

Nothing to configure. Start the bridge in the second editor too; it takes the next free port and
announces itself machine-wide. Then:

> *"List the projects with a running bridge."*

Every tool takes an optional project — a path, a folder name, or the Unity product name. Left out, it
means the project the server was started for, so ordinary work is unchanged.

This is worth doing when you work **on the toolkit itself**: its own dev app uses the working copy
directly, so a C# change can be compiled and tried there in seconds instead of pointing a client's
package reference at a local path and back for every iteration.

---

## Troubleshooting

**`/mcp` shows the server but every call fails.**
The bridge is not running in Unity, or the editor is not open. Start it from the menu.

**"Refusing to use the bridge at … it serves 'X', but this MCP server was started for 'Y'."**
Working as intended: the proxy found a bridge belonging to a different project. Either start the
bridge in the project you meant, or name the project explicitly on the call.

**The proxy finds no bridge, or finds several.**
Discovery looks upward from the launch directory first, then a bounded scan downward — a repository
may *contain* a Unity project rather than be one. Several candidates are reported as an error rather
than guessed; pass `--project <path>` in the registration to disambiguate.

**Stale announcement after a crash.**
The proxy says it cannot connect and re-reads the file on the next call. Restarting the bridge also
prunes registry entries whose process is gone.

**A tool is refused with "Editor is busy (importing for 40s)".**
Also intended. Anything that changes project state waits until the editor is idle; polling `status`
until `busyWith` is `null` is the answer. Read-only calls always work.

---

## Next steps

**[Working With It](ai-workflow.html)** — the habits that make the difference between a useful assistant
and a confusing one.
