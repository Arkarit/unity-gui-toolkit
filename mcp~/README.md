# GUI Toolkit — Mini-MCP

A minimal MCP server ("Mini-DAL") that lets an external Claude drive the GUI Toolkit
Unity Editor for AI screen authoring. It is a thin stdio proxy that forwards MCP tool
calls over HTTP to a small listener running inside the Editor.

```
Claude (MCP client) ──stdio──> mcp~/server.mjs ──HTTP──> Unity Editor bridge
                                                          (UiScreenMcpBridge.cs, 127.0.0.1:17632)
```

> **Why the folder is named `mcp~`:** this is a Node tool, not Unity code. Unity's asset
> pipeline ignores any folder ending in `~` (same convention as `Documentation~`/`Samples~`),
> so the server ships *with* the package (version-synced to the Editor bridge) but is never
> imported as an asset in a consuming project. Run it with Node from disk; don't look for it
> in Unity's Project window.

## Setup

1. **Install the proxy deps** (once):
   ```
   cd mcp~
   npm install
   ```
2. **Start the Editor bridge** in Unity: menu **`Gui Toolkit → AI → Start MCP Bridge`**
   (stays on across domain reloads until you pick *Stop MCP Bridge*).
3. **Register the server** with your MCP client (see below), then restart the client and
   approve the `ui-toolkit` server. Verify with `/mcp` — you should see the tool list.

### One editor per project, at the same time

The bridge no longer owns a fixed port. It **probes upward from 17632** and then writes what it
found to `Library/UiToolkit/mcp-bridge.json` inside its own project:

```json
{ "port": 17633, "url": "http://127.0.0.1:17633/", "projectPath": "D:/_programming/botw-client",
  "unityVersion": "2022.3.62f2", "pid": 221928, "startedAtUtc": "..." }
```

The proxy starts from the directory it was launched in (or `--project <path>`) and looks for that
file: **upwards** first, in case it was started in a subfolder, then a bounded scan **downwards** —
because a repo may *contain* a Unity project rather than be one. This repo is exactly that case: its
dev app lives in `.Dev-App/Unity`, so a root-only lookup finds nothing. Several candidates are an
error rather than a guess; pass `--project` to disambiguate.

So several editors can each serve their own bridge with no configuration to keep straight, and this
repo and a client repo can be worked on side by side.

More importantly, the proxy then **verifies** that the bridge which answered reports the same project
it was started for, and refuses otherwise:

```
Refusing to use the bridge at http://127.0.0.1:17633/: it serves 'D:/…/botw-client',
but this MCP server was started for 'D:/…/unity-gui-toolkit'.
```

That check is the point of the whole mechanism. `/mcp` shows which project a server was *registered*
for, but nothing used to say which editor it had actually reached — so a proxy could bake into a
different project that merely happened to hold the port, without a word. Which project answers now
travels in every `ping` and `status`.

`UI_TOOLKIT_BRIDGE_URL` still overrides discovery for unusual setups. It replaces only the
*connecting*: discovery still runs to learn which project the launch directory belongs to, so the
check above still applies. (It has to work that way — holding the URL against the launch directory
instead would reject a nested project's own bridge, i.e. this repo's.)

Stale file after a crash: the proxy fails to connect, says so, and re-reads the file on the next call.

### One session across several editors

Each bridge also announces itself machine-wide, in `%LOCALAPPDATA%\UiToolkit\bridges\` (or
`~/.local/share/UiToolkit/bridges/`), so **one** session can enumerate and reach all of them:

- `list_projects` lists every project with a bridge — path, folder name, Unity product name, port,
  and whether it still answers.
- **every** tool takes an optional `project`: a full path, a folder name, or the Unity product name
  (`botw-client` and `BOW` both work). Left out, it means the project the server was started for, so
  ordinary work is unchanged.

The point is not convenience. Working on the toolkit itself, a C# change can be compiled and tried in
**this repo's own dev app** — which uses the working copy directly — instead of pointing a client's
package reference at `file:` and back for every iteration. That round trip costs a package reimport
plus a recompile each way.

There is deliberately **no** "switch to project X" command. A mutable session target would make the
answer to *which project does this call write to* invisible again, which is the exact failure the
announcement mechanism exists to prevent. An argument is slightly more to type and always readable.

Ports really do move: with two editors restarting at once, the client and the dev app swapped 17632 and
17633 between them. Nothing needed adjusting, which is the whole idea — but it is also why a proxy from
before this change must be reconnected rather than trusted.

The registry is pruned by the editor on bridge start: entries whose process is gone are deleted, since
the editor is the one participant that can cheaply tell whether a pid is still alive.

### Registering with Claude Code — the easy way

The registration file (`.mcp.json`) holds a **machine-specific absolute path**, so it is
**gitignored on purpose** — everyone generates their own. You don't have to write it by hand:
just open Claude Code in this repo and ask it to do it, e.g.

> *"Set up the ui-toolkit MCP server for this repo."*

Claude has the recipe below and will create the local `.mcp.json` with the correct path for
your machine.

**The recipe** (what Claude does, or you can do manually — write `.mcp.json` at the repo root):

```jsonc
{
  "mcpServers": {
    "ui-toolkit": {
      "command": "node",
      // Absolute path to THIS repo's mcp~/server.mjs. Forward slashes work on Windows too.
      // e.g. Windows:  "D:/dev/unity-gui-toolkit/mcp~/server.mjs"
      //      macOS:    "/Users/you/dev/unity-gui-toolkit/mcp~/server.mjs"
      "args": ["<ABSOLUTE-PATH-TO-REPO>/mcp~/server.mjs"]
    }
  }
}
```

Steps Claude follows:
1. Resolve the repo root (`git rev-parse --show-toplevel`) → build the absolute path to `mcp~/server.mjs`.
2. Confirm `node --version` ≥ 18 and that `mcp~/node_modules` exists (else run `npm install` in `mcp~/`).
3. Write `.mcp.json` at the repo root with that path (forward slashes are fine on Windows).
4. Tell you to restart Claude Code and approve the `ui-toolkit` server, then check `/mcp`.

(You can also use `claude mcp add`, but the file is simplest and lives with the repo checkout.)

## Tools (current)

| Tool | Description |
|------|-------------|
| `ping` | Verify the Editor bridge is reachable. |
| `status` | Report whether the editor is compiling/importing (`{ running, compiling, updating }`). |
| `recompile` | Force Unity to pick up changed C# and recompile, then wait until the editor is idle again (no manual window focus needed). |
| `setup_status` | One-shot project-state health check (registry, prefab-variants path, catalog freshness, missing standard elements). |
| `get_catalog` | Locate the screen-authoring catalog: returns a small summary (`{ path, absolutePath, counts, … }`), NOT the body — read the file yourself, it's large. |
| `regenerate_catalog` | Re-run the generator in Unity, then return the same summary envelope as `get_catalog`. |
| `list_styles` | List the project's style vocabulary (named looks per target component type), read off the catalog on disk. Call it before authoring text/panels; optional `targetType` filter. |
| `bake_screen` | Bake a screen description (`{ name, root }`) into a real `.prefab`; returns `{ path, warnings }`. Pass it inline via `screen` or from disk via `screenPath` (a baked screen's `.screen.src.json` sidecar — re-baking never needs the description resent). `preserveEdits` keeps hand edits on a re-bake. |
| `read_screen` | Read a `.prefab` back into screen JSON (`{ screen, warnings }`) — inspect, tweak, re-bake. `source`: `auto` (sidecar if present, else structural) / `sidecar` / `structural`. |
| `screenshot_view` | Render a baked prefab to a PNG (Edit-Mode) and return the image — the AI preview loop. |
| `screenshot_motion` | See an ANIMATION: the prefab sampled at several points along its timeline, composed into one contact sheet. Edit-Mode, master **and** slaves. See below. |
| `tag_standard_element` | Tag prefab roots with a standard-element identity so they enter the registry/palette (base+variant batch safe). |
| `untag_standard_element` | Remove the standard-element marker from prefabs. |
| `set_ui_comment` | Set a flavor description (UiComment) on prefab roots — Inspector doc, and palette description for palette prefabs. |
| `capture_prefab_values` | Snapshot EVERY serialized value of a baked prefab (node path + Unity `propertyPath`) to `Library/`. Take it BEFORE re-baking something a human edited. |
| `apply_prefab_values` | Restore that snapshot's residue into the re-baked prefab. Dry run by default — read the plan first. See below. |
| `get_console` | Read this editor session's console (ring buffer), filterable by `severity`/`contains`, with `sinceSequence` for "what did that action produce". |
| `resolve_packages` | Make Unity pick up an externally edited `manifest.json` without waiting for window focus, then wait for idle. |

### Surviving a re-bake: `preserveEdits` vs. the snapshot pair

Two mechanisms, and they answer different questions.

`preserveEdits` works entirely in the description's vocabulary. It can only keep what `read_screen`
can see, so a value the catalog does not advertise — an inherited TMP `fontSize`, a gradient
direction — is invisible to it and gets overwritten.

`capture_prefab_values` + `apply_prefab_values` is complete by construction instead: it reads through
`SerializedObject`, so it holds everything Unity serializes whether or not the authoring vocabulary
has a word for it. Use it for the edit workflow:

```
capture_prefab_values   →  adapt the description  →  bake_screen  →  apply_prefab_values (dry run)
```

The restore compares the snapshot against the prefab **as it is after the bake**, so anything the bake
already reproduced is left alone and only the residue is proposed. That residue is ambiguous by
nature, and no tool can resolve it: a difference means either "a human edit the description cannot
carry" (restore it) or "a change you deliberately made to the description" (do not — you would undo
your own decision). Hence dry run first, then apply with `include` narrowed to what you meant.

Object references are reported, never written: wiring belongs to the description via `#id`. Read
`propertyHistogram` as a roadmap — a property that keeps needing a restore is a gap worth closing in
the baker itself.

### Seeing an animation: `screenshot_motion`

A single screenshot cannot show motion, so an authored animation could only be verified by reading its
serialized numbers. This plays the animation in Edit Mode — through the same `EditorPlay` /
`UpdateInEditor` the Inspector's test buttons use — and returns several points along the timeline as
one contact sheet, read left to right, top to bottom, each cell carrying a progress strip at its
bottom edge.

It steps the master **and every animation the master drives**, slaves transitively. That matters:
each animation advances through its own `Update`, which Unity calls in Play Mode but not here, so
stepping only the master would show a panel popping while its click catcher never fades — and report
half the motion as all of it. `drivenAnimations` in the response says what actually moved, so a slave
you expected but do not see there is not wired as a slave.

Duration is measured by playing it, not derived: the real length includes slaves and their delays and
is not exposed anywhere.

What it reliably catches: nothing moves at all, the wrong node moves, an overshoot that leaves the
panel clipped, and a curve that ends anywhere other than its end value — which leaves the element
permanently wrong rather than briefly. What it cannot tell you is whether the motion *feels* good.
Timing and easing are human judgements, so prefer curves already used elsewhere in the project over
invented ones, and when a new curve matters, offer a couple of candidates and let a human pick.

### Screen JSON shape (for `bake_screen`)

```json
{
  "name": "MyDialog",
  "root": {
    "type": "UiView",                       // element node: a catalogued component
    "props": { "layer": "Dialog" },
    "children": [
      { "template": "StandardPanelBackgroundWithHeadline",   // template node: a palette prefab
        "text": "@text:Title",
        "children": [
          { "template": "StandardButtonBar", "children": [
            { "template": "OkButton",     "id": "okButton",     "text": "@text:OK" },
            { "template": "CancelButton", "id": "cancelButton", "text": "@text:Cancel" }
          ]}
        ]
      }
    ]
  }
}
```

A node has **either** `type` (build a component from scratch) **or** `template` (instantiate a palette
prefab). Optional per-node fields: `id`, `name`, `props` (serialized fields), `style` (style name),
`text` (`@loca:` key or `@text:` literal), `rect` (layout), `overrides`, `children`.

### Do not copy subtrees

Two children with the same shape and look are already redundancy. Author the repeated part once as its
own prefab through the screen's top-level **`prefabs`** array — each entry is a full screen description,
baked before the screen itself, and their paths come back in `companions` — then instantiate it with
`template` and vary the parts via **`overrides`**, keyed by transform path inside the template:

```json
{
  "name": "RewardTrack",
  "prefabs": [
    { "name": "RewardCard", "outputPath": "Assets/…/RewardCard.prefab", "root": { … } }
  ],
  "root": { "type": "UiView", "children": [
    { "template": "RewardCard", "overrides": {
        "Title":  { "text": "@text:TIER 1" },
        "Icon":   { "props": { "sprite": "Assets/…/Coin.png" } },
        "Footer/Label": { "id": "claim1", "text": "@text:CLAIM" }
      }}
  ]}
}
```

An `id` inside `overrides` also registers that internal part for `"#id"` wiring — the only way to
reference something inside a template. The baker **warns** when it finds identical sibling subtrees.

For a list whose rows come from data at runtime, author **one** instance or none: the container plus a
prefab reference (give a prefab-typed prop the prefab's project path) is what shipped screens do.

**`rect`** controls the RectTransform:

```json
"rect": {
  "anchor": "center",        // preset: stretch/fill, center, top, bottom, left, right,
                             //   top-left/-right, bottom-left/-right, top-/bottom-/left-/right-stretch,
                             //   stretch-horizontal, stretch-vertical
  "size": [900, 600],        // sizeDelta [w,h]
  "position": [0, 40],       // anchoredPosition [x,y]
  "anchorMin": [0,0], "anchorMax": [1,1], "pivot": [0.5,0.5],   // explicit overrides
  "offsetMin": [20,20], "offsetMax": [-20,-20]                  // stretch margins
}
```

A `UiView` root's Canvas + CanvasScaler are configured automatically (screen-space, toolkit reference
resolution) — the runtime setup that a baked prefab would otherwise miss.

## Config

- `UI_TOOLKIT_BRIDGE_URL` — override the Editor bridge URL (default `http://127.0.0.1:17632/`).

## Notes

- Requires Node 18+ (uses global `fetch`).
- The bridge binds **loopback only** (`127.0.0.1`) and processes every request on the
  Editor main thread; if Unity is compiling or unfocused a call may time out (~20s).
- After editing toolkit C#, call `recompile` instead of clicking into the Unity window —
  it triggers the rebuild and waits out the domain reload, so authoring stays hands-free.
