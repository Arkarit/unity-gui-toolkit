---
layout: default
title: AI Support — Working With It
---

# AI Support — Working With It

Practical habits for sharing a Unity project with an AI assistant. None of this is ceremony: every
item below comes from something that actually went wrong once.

---

## Ask for the loop, not for the file

The useful unit of work is *build → look → adjust*, and the assistant can close that loop itself. So
instead of asking for a prefab, ask for the loop:

> *"Build a settings dialog with a headline, three toggles and an OK/Cancel bar. Show me a screenshot
> when it looks right."*

It will bake, render, notice that the button bar overlaps, fix it, and only then come back. What you
review is a picture and a readable description — not a YAML diff.

Good things to ask for explicitly:

- **"Show me"** — screenshots are cheap, both in Edit Mode and from the running app.
- **"What does the project already use for this?"** — it can group every animation in the project by
  how the motion looks, and tell you the four shapes that cover 90% of your UI. Reusing one of those
  is almost always better than a new invention.
- **"Check the console since that action"** — it can ask for exactly the messages one action produced,
  rather than guessing from a log file.

---

## Keep the file out of its way

The single most common way to lose work: the assistant writes a prefab or scene **that you have open
in the editor**. Unity's in-memory copy wins, and your change or its change quietly disappears.

The bridge now refuses the obvious cases — baking over the prefab currently open in Prefab Mode, or
restoring values into it — and the assistant can ask per file whether it is safe to touch. But the
cheapest rule is human:

> **Close the prefab or scene you asked it to change.**

If you would rather keep it open, say so, and let it hand you the change to apply in the inspector
instead. Three drags in the inspector are more reliable than surgery on prefab YAML — and *far* more
reliable than either of you editing the same asset from two sides.

---

## Tell it what you changed — one sentence is enough

If you hand-edit a screen it authored, say so in passing:

> *"I moved the close button and made the headline bigger."*

It will diff the baked prefab against its own description itself and fold your change back in. What it
cannot do is notice a change nobody mentioned: a re-bake rebuilds from the description, and anything
the description does not know about is gone.

For edits the description cannot express — a font size on a template's internals, a gradient direction
— it has a snapshot mechanism: capture every serialized value **before** re-baking, then review a plan
of what to restore. Ask for the dry run and read it: a difference can equally mean "a human edit worth
keeping" or "a change the assistant deliberately made", and only the two of you together can tell
those apart.

---

## Bring the app to the interesting state

Play Mode starts at your first scene. The assistant cannot log in, cannot pass your splash screen, and
in a project with a real backend it should not be firing requests on its own anyway.

So the division of labour is:

1. **You** press Play and navigate to the screen in question.
2. **You** say so: *"Dashboard is open."*
3. **It** captures the Game View, asks what a tap at that position would hit, and taps.

That last part is worth more than it sounds. A full-screen frame image with raycasting left on swallows
every click beneath it while looking perfectly correct in a screenshot. Asking the running UI what a
tap *actually* hits is the only way to catch that — and it reports the whole hit stack, so "the button
is there but something invisible is on top" becomes a one-line answer.

---

## Review the description, not just the prefab

Every baked screen has a `<Name>.screen.src.json` sidecar next to it. That is the source of truth for
the next re-bake, and it is meant to be read by humans. Skim it in review: if the description says
something you did not intend, the prefab will say it again after the next bake.

Two related habits:

- **Do not hand-add components to a baked prefab.** The next bake rebuilds from the description and
  drops them. Ask for the component to be *authored* instead.
- **Do not hand-tag a baked prefab as a standard element** for the same reason. The marker belongs in
  the description.

---

## Localization

Text the assistant authors goes through the normal gettext pipeline, so the usual rule applies: a
literal in a translate call gets harvested into the POT and lands in front of translators.

Two consequences worth knowing:

- **Unfinished features:** if a screen should stay invisible for now, keep its strings out of the
  translate calls. The assistant can author text as plain literals on purpose.
- **The extractor scans source as text.** A *comment* that spells out the translate call is harvested
  just like the call itself. Describe such decisions in words, not in code shapes.

---

## When something looks broken

**A burst of hundreds of identical "missing script" save errors.**
Unity refuses to save a prefab that contains a component whose script cannot be loaded, and it logs one
error per offending GameObject — a prefab open in Prefab Mode with auto-save retries on every repaint.
Nothing is being written, so nothing is damaged, but the session is degraded: prefab saves will keep
failing until the editor is restarted. Ask the assistant to check the prefab's state; if it reports
zero unloadable scripts on disk, a restart clears it. (The trigger for this was a timing bug in the
bridge's own waiting logic, fixed in `v-00-05-187` — if you still see it, it is worth reporting.)

**Old prefabs that genuinely carry broken components.**
Long-lived projects tend to accumulate prefabs referencing scripts deleted years ago. They sit there
harmlessly until something saves them. `Editor/Tools/RemoveMissingComponentsWindow` cleans them —
but run it only in a healthy editor session, because in a degraded one "missing" is not real and the
tool would delete live components.

**Authored screens look like the raw library.**
The project-state check in **[Setup](ai-setup.html#step-5-check-the-projects-health)** answers this in one
call. Usually the standard-element registry or the prefab-variants folder.

---

## Two things to expect

**It will ask you to reconnect sometimes.** The Node proxy is only reloaded when the MCP client
reconnects, so a change to the proxy needs `/mcp`. Changes inside Unity do not.

**It will refuse things.** Heavy operations are declined while the editor compiles or imports, and
writes are declined for a file the editor holds open. Both are deliberate: the failure mode they
prevent is a request that times out, which takes away the assistant's view of the editor at exactly
the moment it needs to look.

---

## Related

- **[AI Support — Overview](ai-overview.html)** — architecture, strengths, limits
- **[AI Support — Setup](ai-setup.html)** — install and project health
