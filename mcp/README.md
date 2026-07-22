# GUI Toolkit — Mini-MCP

A minimal MCP server ("Mini-DAL") that lets an external Claude drive the GUI Toolkit
Unity Editor for AI screen authoring. It is a thin stdio proxy that forwards MCP tool
calls over HTTP to a small listener running inside the Editor.

```
Claude (MCP client) ──stdio──> mcp/server.mjs ──HTTP──> Unity Editor bridge
                                                         (UiScreenMcpBridge.cs, 127.0.0.1:17632)
```

## Setup

1. **Install the proxy deps** (once):
   ```
   cd mcp
   npm install
   ```
2. **Start the Editor bridge** in Unity: menu **`Gui Toolkit → AI → Start MCP Bridge`**
   (stays on across domain reloads until you pick *Stop MCP Bridge*).
3. **Register the server** with your MCP client. For Claude Code, add to `.mcp.json`
   (or run `claude mcp add`):
   ```json
   {
     "mcpServers": {
       "ui-toolkit": {
         "command": "node",
         "args": ["<ABSOLUTE-PATH>/mcp/server.mjs"]
       }
     }
   }
   ```

## Tools (current)

| Tool | Description |
|------|-------------|
| `ping` | Verify the Editor bridge is reachable. |
| `get_catalog` | Return the current screen-authoring catalog JSON (components, styles, skins, palette). |
| `regenerate_catalog` | Re-run the generator in Unity, then return the fresh catalog. |
| `bake_screen` | Bake a screen description (`{ name, root }`) into a real `.prefab`; returns its project path. |

A screenshot tool for the AI preview loop is added next.

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
