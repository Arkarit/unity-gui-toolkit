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
| `status` | What the editor is doing **and what it holds open**: compiling/importing with `busyWith` + `busySinceSeconds`, plus `isPlaying`, `hasFocus`, `openScenes[]` with dirty flags and `prefabStage`. See below. |
| `asset_state` | Pre-flight for specific assets: is the editor holding this file, and would Unity even accept saving it (`missingScripts`)? See below. |
| `recompile` | Force Unity to pick up changed C# and recompile, then wait until the editor is idle again (no manual window focus needed). |
| `setup_status` | One-shot project-state health check (registry, prefab-variants path, catalog freshness, missing standard elements). |
| `get_catalog` | Locate the screen-authoring catalog: returns a small summary (`{ path, absolutePath, counts, … }`), NOT the body — read the file yourself, it's large. |
| `regenerate_catalog` | Re-run the generator in Unity, then return the same summary envelope as `get_catalog`. |
| `list_styles` | List the project's style vocabulary (named looks per target component type), read off the catalog on disk. Call it before authoring text/panels; optional `targetType` filter. |
| `bake_screen` | Bake a screen description (`{ name, root }`) into a real `.prefab`; returns `{ path, warnings }`. Pass it inline via `screen` or from disk via `screenPath` (a baked screen's `.screen.src.json` sidecar — re-baking never needs the description resent). `preserveEdits` keeps hand edits on a re-bake. |
| `read_screen` | Read a `.prefab` back into screen JSON (`{ screen, warnings }`) — inspect, tweak, re-bake. `source`: `auto` (sidecar if present, else structural) / `sidecar` / `structural`. |
| `screenshot_view` | Render a baked prefab to a PNG (Edit-Mode) and return the image — the AI preview loop. |
| `screenshot_motion` | See an ANIMATION: the prefab sampled at several points along its timeline, composed into one contact sheet. Edit-Mode, master **and** slaves. See below. |
| `harvest_motion` | Read the project's existing animations out of its prefabs and group them by how the motion looks, most used first. Call it **before** authoring any animation. See below. |
| `play_mode` | Query, enter or leave Play Mode. The bridge survives the domain reload; poll `status` until it matches. |
| `screenshot_game` | Capture the Game View of the **running** app — real data, real resolution, real state. |
| `probe_ui` | Ask the running UI what a tap would actually hit, and optionally perform it. See below. |
| `tag_standard_element` | Tag prefab roots with a standard-element identity so they enter the registry/palette (base+variant batch safe). |
| `untag_standard_element` | Remove the standard-element marker from prefabs. |
| `set_ui_comment` | Set a flavor description (UiComment) on prefab roots — Inspector doc, and palette description for palette prefabs. |
| `capture_prefab_values` | Snapshot EVERY serialized value of a baked prefab (node path + Unity `propertyPath`) to `Library/`. Take it BEFORE re-baking something a human edited. |
| `apply_prefab_values` | Restore that snapshot's residue into the re-baked prefab. Dry run by default — read the plan first. See below. |
| `get_console` | Read this editor session's console (ring buffer), filterable by `severity`/`contains`, with `sinceSequence` for "what did that action produce". |
| `resolve_packages` | Make Unity pick up an externally edited `manifest.json` without waiting for window focus, then wait for idle. |
| `clone_style_config` | Give the project its OWN style config, copied out of the package, and repoint the configuration at it. The first step of any theming. See below. |
| `read_skin` | Read the VALUES behind the style names — colours, fonts, sizes, sprites. Applicable values only unless asked otherwise. See below. |
| `write_skin` | Write those values. One edit reaches every prefab using the style. `dryRun` reports before/after without writing. See below. |
| `execute_code` | Run a C# snippet inside the editor and get its return value. The escape hatch for everything with no tool of its own. See below. |

### Before you write or wait: `status` and `asset_state`

The editor's own state is invisible from outside, and both blind spots cost a session:

**What it holds open.** A scene or prefab that is open belongs to the editor's memory, not to the file on
disk. Rewrite that file from a text tool and the in-memory copy wins — your change is silently undone, or
worse, half-merged on the next save. Revert one underneath the editor and you get "Prefab instance data
layout did not match" warnings and an editor that needs restarting. `status` reports `openScenes[]` with
`isDirty` and the current `prefabStage`; `asset_state` answers it per path as `safeToWriteFromOutside`.
When it is false, the honest options are: write through the editor, or ask a human to close the file.

**Whether Unity would accept the save at all.** A prefab containing a component whose script cannot be
loaded cannot be saved: Unity refuses and logs one error *per offending GameObject*. A prefab open in
Prefab Mode with auto-save retries on every repaint, so a single asset can bury the console in hundreds of
identical errors — and none of it says what the actual problem is. `asset_state` reports
`savableByUnity: false` with `missingScripts.count` and the first few GameObject names. Cheap to ask, and
it turns an avalanche into one sentence. Old projects tend to carry this quietly: prefabs whose scripts
were deleted years ago sit there harmlessly until something tries to save them.

**Both of those are refused, not merely reported.** A check that only an agent's good intentions enforce
is worth little, so the bridge says no by itself:

- `bake_screen` and `apply_prefab_values` refuse when the target is the asset the editor currently has open
  (Prefab Mode, or an open scene), naming it.
- `resolve_packages` refuses while a domain reload would put unsaved state at risk: Play Mode running, a
  dirty scene, or a dirty prefab stage. With none of those present it just runs — a package change is an
  ordinary step and should not need a human's blessing every time. A question answered "yes" nine times out
  of ten trains everyone to stop reading it; what deserves a stop is the tenth time, and that is exactly
  what these blockers are.

**Not two heavy things at once.** `busyWith` is `"compiling"`, `"importing"` or `null`, with
`busySinceSeconds` so a fresh import is distinguishable from one that has been running a minute. Heavy
methods (`recompile`, `resolve_packages`, `bake_screen`, `apply_prefab_values`, `regenerate_catalog`,
`screenshot_motion`, `harvest_motion`, the tagging/comment writers, entering or leaving Play Mode) are
**refused** while the editor is busy, with a message saying what is running and for how long. That is
deliberately a refusal rather than a queue: firing into a running import is what produces a timeout, and a
timeout is the worst outcome — it takes away your view of the editor exactly when you need it most. Poll
`status` until `busyWith` is null instead. Read-only calls (`status`, `asset_state`, `get_console`,
`ping`, `read_screen`, `get_catalog`) always answer.

### Theming: `clone_style_config`, `read_skin`, `write_skin`

`list_styles` names the vocabulary. These three change what it looks like.

**Clone first, and understand why.** A fresh project uses the style config that ships *inside the
package*. Editing it is not a smaller version of theming, it is a mistake with a delay on it: the asset
lives in the immutable package copy, so the edit is either refused or quietly discarded at the next
version bump. `clone_style_config` copies it into the project and repoints the `UiToolkitConfiguration`
at the copy. It is idempotent — on an already project-local config it reports that and changes nothing —
and `write_skin` refuses to write a package-owned config, so the order cannot be got wrong by accident.

The clone also repairs something a plain duplicate leaves broken: every skin and every style holds a
reference back to the config it belongs to, and `Instantiate` copies those verbatim, still naming the
original. In a 2-skin default config that is 128 references pointing into the package.

**Read before you write, and read again after.** `read_skin` returns the values that are switched on
(`applicable`) — a style carries an entry for every serialized field of its target component, and the
handful that are on are the ones that define the look. Values use the same notation as screen props:
colours `"#RRGGBBAA"`, assets as project-relative paths, enums by name, TMP text gradients as
`{ topLeft, topRight, bottomLeft, bottomRight }` (or `[top, bottom]`, or one colour, when writing).

**Write by name and component type.** A style name is not unique on its own:
`Buttons/Standard/Background` exists as an `Image`, a `UiGradientSimple`, a `UiDistort`, a `Shadow` and
a `RectTransform` — five different aspects of one button. Ambiguity is an error naming the candidates,
never a guess. A bare value means "use this and switch it on"; `{ value, applicable: false }` hands the
value back to whatever the component itself carries. The result reports before/after per value, and
`dryRun: true` reports exactly that plan without writing.

Appliers pick a change up immediately, in Edit Mode too — so `screenshot_view` shows the new look with
no re-bake and no reimport. Style *values* need no catalog regeneration; renaming skins or styles does.

### The escape hatch: `execute_code`

Runs a C# snippet inside the editor and hands back its return value. It exists so the toolkit stays
fully drivable in a project with no separate code-execution bridge installed — reachable as far as the
editor's own API goes, rather than as far as someone has already built a tool.

Bare statements are wrapped for you, with `System`, `UnityEngine`, `UnityEditor`, `TMPro`, `GuiToolkit`,
`GuiToolkit.Style` and `Newtonsoft.Json.Linq` already imported, so
`return UiStyleConfig.Instance.Skins.Count;` is a complete snippet. A full compilation unit is compiled
as written and needs its own usings plus exactly one parameterless `public static Run()`. Compile errors
come back as diagnostics with line numbers **in your source**, not in the generated wrapper; an
exception comes back with its stack; anything the snippet logs is captured.

It is **not** a sandbox, and the honest reading of that is: it runs with the editor's rights, on the
main thread, and a write is a real write. An endless loop freezes the editor — the handler stops waiting
for an answer, the loop does not stop running. Every compiled snippet stays loaded in the domain until
the next reload, because .NET cannot unload one. Use `validateOnly` while you are unsure, and prefer the
narrow tools where they exist: those refuse the unsafe cases (writing a file the editor holds open) that
this one performs without comment.

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

### Authoring animation: harvest before you invent

Keyframes are computable. The **slope between them** is not — tangents are what decide whether motion
feels snappy or rubbery, and they get set by someone watching it. So an animation shape that a project
already uses many times has had the one judgement you cannot make applied to it.

`harvest_motion` reads every animation out of the project's prefabs and groups them by how the motion
looks, ignoring targets and slaves so two animations differing only in what they drive count as one.
Each entry carries a count, a one-line `summary`, the full `values` ready to copy, and example
locations to pair with `screenshot_motion`. In the `summary`:

- `↗` — the curve leaves the 0..1 band, i.e. it overshoots or undershoots
- `~` — the curve has **hand-set tangents**. Prefer these; a shape without them was probably typed,
  not tuned

It is worth trusting. A bump authored from scratch here — `1 → 1.1 → 1` over 0.32s — turned out to
duplicate a shape the project already carried 18 times, at 0.3s, with tangents of 0.2 / 0 / -0.2 and
the opposite encoding (`start 0 → end 1` with the curve holding absolute values). Same motion on paper,
and the harvested one is the one that had been watched.

### What the baker checks about motion

Animation has a failure mode a still cannot show and a filmstrip only shows if someone thinks to film
that particular animation: values that look plausible but leave the target **permanently** wrong. The
baker now reports

- a supported channel whose curve has no keys — the value is pinned at its start (for Alpha, at 0, i.e.
  invisible, because alpha IS the curve value rather than a lerp)
- a curve authored on a channel that `support` does not include, so it never runs
- `Alpha` supported with neither `alphaGraphic` nor `alphaCanvasGroup` — nothing to fade
- a transform channel with no `target` (`m_target` carries no `[Mandatory]`, so the wiring check cannot
  see it)
- `duration` of 0
- a one-shot whose curve ends at neither 0 nor 1, leaving the target stuck between its two ends

The last one only applies when `backwardsPlayable` is off. An animation that can be reversed is a state
animation and is *meant* to hold its end — a hover grows to 1.15 and stays there until it is played
back. Checking those too made the first run five false alarms out of five.

### The running app: `play_mode`, `screenshot_game`, `probe_ui`

The Edit-Mode tools cover what a screen *is* and how it *moves*. What they cannot show is a screen the
server filled in, laid out at the device resolution, that a user reached by tapping — and above all,
whether its buttons are actually **reachable**.

`probe_ui` is the interesting one. "Is this clickable" is decided by the raycast, and a full-rect frame
overlay with `raycastTarget` left on swallows every click beneath it while looking perfectly correct.
That exact defect sat on a screen's tab bar here and could only be reasoned about. So:

- name a `target` and it is aimed at that node's centre, answering `targetReceivesInput` and, when
  something is in the way, `blockedBy`
- the full raycast `hits` stack comes back, topmost first
- with `click`, the tap goes **through** the raycast as down/up/click on whatever is actually on top —
  deliberately not by invoking the button's own event, which would bypass the thing worth testing.
  `handledBy` names the node whose handler ran, or null if it landed somewhere that ignores it

`x`/`y` default to a **top-left** origin, because that is how a captured image reads; Unity's own screen
space is bottom-left, so a coordinate taken off a screenshot would otherwise land mirrored.

Two things worth knowing about `screenshot_game`. It is started and collected in separate bridge calls,
because handlers run on the editor's main thread and the capture only completes once a frame has
**rendered** — a handler that waits for the file blocks the very thread that would produce it. The first
version deadlocked itself and blamed the Game View. And a Game View that is closed, or on a hidden tab,
renders no frames at all, so nothing will ever arrive.

Entering Play Mode reloads the domain, which stops and restarts the bridge. It comes back within a
couple of seconds and answers from inside Play Mode; a call in between simply fails, which is normal.

Note what this does **not** buy you: Play Mode starts the app at its first scene, so for a real game
that means splash, login and network, and driving from there to a particular screen is usually not
feasible. The productive pattern is the other way round — ask the human to bring the app to the state in
question, then capture and probe it.

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
