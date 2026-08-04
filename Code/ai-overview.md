---
layout: default
title: AI Support — Overview
---

# AI Support — Overview

The toolkit ships a small bridge that lets an AI assistant drive the Unity Editor: build screens,
look at what it built, read the console, click around in the running app. This page explains what it
is and where its limits are. For the five-minute install, go to **[Setup](ai-setup.html)**; for day-to-day
habits, see **[Working With It](ai-workflow.html)**.

---

## What it is

Three pieces, on your own machine only:

```
AI assistant  ──stdio──>  mcp~/server.mjs  ──HTTP──>  Unity Editor bridge
(MCP client)              (Node proxy)                (127.0.0.1, loopback only)
```

- **The Editor bridge** (`UiScreenMcpBridge`) is a tiny HTTP listener inside Unity, started from the
  menu. It answers a handful of JSON commands and does all real work on Unity's main thread.
- **The proxy** (`mcp~/server.mjs`) is a Node script that translates MCP tool calls into those HTTP
  calls. It ships with the package so both halves are always the same version.
- **The assistant** connects to the proxy. Nothing leaves your machine: the listener binds to
  loopback, and there is no cloud component.

> **Why `mcp~` and not `Editor/`?** Unity ignores folders ending in `~`. The Node tool travels with
> the package but is never imported as an asset.

---

## What it is good at

| Task | How |
|---|---|
| Building a screen | The assistant writes a **screen description** (JSON) and bakes it into a real `.prefab` |
| Judging the result | Renders the prefab to a PNG and looks at it, in Edit Mode |
| Judging an animation | Samples the timeline into a contact sheet — several frames in one image |
| Matching your project's look | Reads a generated **catalog** of your components, styles and standard elements |
| Checking the running app | Enters Play Mode, captures the Game View, asks what a tap would hit — and taps |
| Diagnosing | Reads this editor session's console, with a "what did that last action produce" filter |

The important part is the **description**, not the prefab. Every baked screen keeps its description
next to it as `<Name>.screen.src.json`. That file is the thing to review, and re-baking from it is
cheap — so "move the button bar up and make the panel narrower" is an edit to a readable document,
not a hunt through YAML.

---

## What it is not good at

Worth knowing up front, so nobody is surprised:

- **It cannot drive your game.** Play Mode starts at your first scene — splash, login, network. It
  cannot log in for you. The productive pattern is: *you* bring the app to the interesting state,
  then the assistant looks and taps.
- **It cannot invent your art direction.** It can only reuse what the catalog already names. If a
  look is not a style, a standard element or a prefab in the project, it will approximate — and
  approximation is exactly what looks off.
- **Hand-tuned curves are beyond it.** It can author animations, but the shapes a human eyeballed
  (custom tangents) are not something it can reproduce from scratch. It can, however, *harvest* the
  animations your project already uses and reuse those.
- **It is not a substitute for review.** It bakes assets in your project. Read the diff.

---

## Vocabulary: the catalog

Everything the assistant can name comes from one generated file,
`Assets/AiSupport/screen-catalog.json`, produced by **Gui Toolkit → AI → Generate Screen Catalog**:

- **Components** — every toolkit and project component it may put on a node, with their serialized
  fields, harvested from `/// <summary>` docs and `UiComment` annotations.
- **Palette** — prefabs it may instantiate as building blocks, with their descriptions.
- **Styles** — the named looks per component type (`Buttons/Standard/BackgroundFrame` and friends).
- **Standard elements** — the identities the toolkit resolves at runtime: which prefab *is* this
  project's OK button, requester, close button, panel background.

If the catalog is stale or the standard-element registry is unset, authored screens come out looking
like the raw library instead of like your project. That failure has a one-call diagnosis —
`setup_status` — described in **[Setup](ai-setup.html)**.

---

## Several projects at once

The bridge does not own a fixed port. It probes upward from **17632** and announces what it found
twice: once inside its own project (`Library/UiToolkit/mcp-bridge.json`) and once machine-wide
(`%LOCALAPPDATA%\UiToolkit\bridges\`). Consequences:

- **One editor per project, all at the same time.** Two Unity instances need no configuration to keep
  apart. Ports genuinely move — two editors restarting together have swapped 17632 and 17633 — and
  nothing needs adjusting.
- **One assistant session can reach all of them.** It lists them and names the one it wants per call.
- **A call cannot land in the wrong project.** Every answer carries the project path, and the proxy
  refuses a bridge that serves a different project than the one it was started for. This matters: an
  MCP client shows which project a server was *registered* for, but not which editor it actually
  reached — so before this check, a session could have written into the wrong project silently.

There is deliberately **no** "switch to project X" command, because a remembered target makes *which
project does this write to* invisible again.

---

## Next steps

- **[Setup](ai-setup.html)** — install, register, verify, and check the project's health
- **[Working With It](ai-workflow.html)** — what to ask for, what to keep out of its way, how to review
