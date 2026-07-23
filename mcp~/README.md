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
   approve the `ui-toolkit` server. Verify with `/mcp` — you should see 7 tools.

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
| `get_catalog` | Locate the screen-authoring catalog: returns a small summary (`{ path, absolutePath, counts, … }`), NOT the body — read the file yourself, it's large. |
| `regenerate_catalog` | Re-run the generator in Unity, then return the same summary envelope as `get_catalog`. |
| `bake_screen` | Bake a screen description (`{ name, root }`) into a real `.prefab`; returns its project path. |
| `screenshot_view` | Render a baked prefab to a PNG (Edit-Mode) and return the image — the AI preview loop. |

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
`text` (`@loca:` key or `@text:` literal), `rect` (layout), `children`.

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
