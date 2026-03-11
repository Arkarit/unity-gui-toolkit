# Align gettext / pgettext signatures with GNU gettext and deprecate non‑standard _() overload

## Summary

The codebase currently provides a non‑standard pgettext/_() API shape (context and msgid parameter order differs from GNU gettext and there is an extra `_()` overload that accepts a context). This breaks parity with standard gettext conventions and complicates POT extraction and contributor expectations. This document proposes a compatibility‑first refactor to align the API and update extraction and tests.

## Background / Current behavior

- Key files: `Runtime/Code/Components/LocaMonoBehaviour.cs`, `Runtime/Code/Loca/LocaManager.cs` (and related implementations).
- Current helpers:
  - `_(msgid)` — standard single‑arg form (OK).
  - `_(msgid, context, group)` — an overload that accepts context after the msgid (non‑standard).
  - `pgettext(msgid, context, group)` — implemented with `(msgid, context, group)` ordering (non‑standard).
- GNU gettext convention: `pgettext(context, msgid)` (context first); tools like `xgettext` expect that signature (or must be specially configured).

## Why this matters

- Extraction tools (xgettext, custom scanners) and experienced contributors expect the standard `pgettext(context,msgid)` signature; the current ordering risks missed extractions and developer confusion.
- Keeping backward compatibility with minimal disruption is important — many callsites and tests rely on the current helpers.

## Proposed solution (compatibility‑first)

1. Add the standard signature:
   - Add `protected static string pgettext(string context, string msgid, string group = null)` that delegates to `LocaManager.Instance.Translate(msgid, context, group)`.
2. Provide obsolete wrappers for the existing non‑standard overloads:
   - Mark the existing `pgettext(msgid, context, group)` and the underscore overload `_(msgid, context, group)` as `[Obsolete("Use pgettext(context, msgid, group) or gettext/_(msgid)")]` and implement them to forward to the new, standard `pgettext(context, msgid, group)` to preserve runtime behavior.
3. Keep the single‑arg `_()` (gettext shortcut) unchanged.
4. Update extraction tooling and documentation:
   - Update any extraction scripts or developer docs to look for `pgettext(context,msgid)`.
   - For `xgettext`, document example keyword mapping: `--keyword=pgettext:1c,2` or equivalent scanner configuration that extracts context correctly.
   - Consider a scanner configuration that recognizes both legacy and new signatures while migration progresses.
5. Tests and docs:
   - Add unit tests asserting `pgettext(context,msgid)` equals `Translate(msgid, context)` and that obsolete wrappers still return identical translations.
   - Update Localization docs (EN and add `-de.md` copy) describing the new standard API and migration guidance.
6. Migration plan:
   - Phase A (non‑breaking): add the standard `pgettext(context,msgid)` and the obsolete forwarding wrappers; update docs and extraction config.
   - Phase B (opt‑in removal): after a deprecation window, remove the old overloads in a later major bump.

## Implementation notes / files to change

- `Runtime/Code/Components/LocaMonoBehaviour.cs`: add standard `pgettext(context,msgid)`, mark old overloads `[Obsolete]`, update XML docs.
- `Runtime/Code/Loca/LocaManager.cs` and `LocaManagerDefaultImpl.cs`: ensure `Translate` overloads remain available; add unit tests asserting equivalence.
- Tests: add `TestPgettextSignatures` in `Tests/Editor` to validate new signature and wrapper behavior.
- Extraction tooling: update any xgettext/scan scripts with `--keyword=pgettext:1c,2` (or equivalent) and document the change.
- Documentation: update `Documentation/Localization` (and add `-de.md` copy).

## Acceptance criteria

- New `pgettext(context,msgid[,group])` API present and documented.
- Existing non‑standard overloads still work but are marked `[Obsolete]` and forward to the new API.
- Extraction instructions updated (xgettext or scanner) and documented.
- Unit tests verify equivalence between new and old behavior and are green in CI.
- A migration note is present in the Localization docs.

## Suggested labels

`area:localization`, `type:refactor`, `priority:medium`, `breaking-change:no` (phase A)

## Checklist (for the PR)

- [ ] Add `pgettext(context,msgid[,group])` implementation
- [ ] Add `[Obsolete]` wrappers for legacy overloads
- [ ] Update `LocaMonoBehaviour` XML docs
- [ ] Update extraction script(s) and add example xgettext invocation
- [ ] Add unit tests (`TestPgettextSignatures`)
- [ ] Update Localization docs (EN and `-de.md`)
- [ ] Run editmode tests; ensure green
- [ ] Leave a follow‑up TODO for removal of deprecated wrappers after a deprecation window

---

Notes

This approach preserves runtime compatibility while moving the API toward standard gettext semantics and making extraction robust. Phase A can be implemented with no breaking change and will give consumers time to adopt the canonical signature before removing legacy wrappers in a later major release.
