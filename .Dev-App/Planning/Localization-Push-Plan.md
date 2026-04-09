# Implementation Plan: Localization Push & Gettext↔Spreadsheet Integration

## Goal
Add push/export capabilities to the localization toolchain so translations can be pushed back to local Excel files and Google Sheets, and integrate this with the gettext extraction/merge workflow such that POT→PO→(optional)Spreadsheet push becomes a single, auditable pipeline.

Scope covers Editor-only features (UI and tooling), security for Service Account handling, tests and CI integration, and documentation updates. Runtime behavior of `ILocaProvider` remains unchanged; this work focuses on Editor and build-time tools.

## Summary of the proposed user workflow (your idea)
1. Scan source for `_()`, `pgettext()`, `_n()`, etc. and update POT templates.
2. Merge POT changes into existing PO files (editor merge step; creates or updates msgid entries and preserves msgstr where possible).
3. If configured, forward the updated PO data to one or more configured `LocaExcelBridge` instances and push the data back to the bridge's target (local .xlsx file or Google Sheets).

This closes the loop so translators can work in spreadsheets (local or cloud) and code changes are reflected back in the translation assets.

## Quick evaluation of the idea
- Strongly recommended: it improves translator workflows and reduces manual copy/paste.  
- Key risks: mapping fidelity (context, plurals), merge conflicts, accidental overwrites, and secure handling of service account keys for Google.  
- Mitigations: require explicit opt-in per bridge, dry-run previews, and conservative merge (add new keys and update empty msgstrs by default; require manual review for overwrites of existing msgstrs).

## High-level implementation phases

Phase 0 — Design & Decisions (no code changes)
- Decide XLSX writer library (ClosedXML vs EPPlus vs CSV fallback) and verify Unity Editor compatibility.  
- Confirm merge strategy (overwrite vs merge-empty-only vs interactive review).  
- Decide UI flow: automatic push after extraction vs explicit "Push to Bridges" action.  
- Decide Google scopes required (write vs drive.file) and CI secret handling.

Phase 1 — Google Sheets Push (Editor)
- Add push APIs to `LocaExcelBridge` or a new `LocaPushService` that can: (a) accept a ProcessedLoca/PO representation, (b) map to bridge column layout, and (c) perform a `spreadsheets.values.batchUpdate` (or create/replace sheet).  
- Extend `GoogleServiceAccountAuth` to optionally request write scopes and token caching; add helper to validate service account email and permissions.  
- Add an Editor button (LocaExcelBridge inspector): `Process` -> `Push to Google` with options (dry-run, overwrite policy, target sheet id).  
- Add logging and undo-safe behavior (push only on confirmation).  
- Acceptance criteria: Able to push a small translation table to an existing sheet; dry-run shows exactly what would change.

Phase 2 — Local Excel Export
- Implement `.xlsx` writing using the selected library, producing columns for key/context and per-language plural columns as configured.  
- Add `Push to .xlsx` inspector action; support writing to an existing file (merge) or produce a new file.  
- Fallback: CSV export for basic scenarios and CI usage.  
- Acceptance criteria: Exported .xlsx opens in Excel/LibreOffice and contains expected columns and plural forms.

Phase 3 — Gettext ↔ Spreadsheet Pipeline Integration
- Extend `LocaProcessor` to optionally run the merge step after POT extraction.  
- Implement a merge engine that: (a) loads existing PO files, (b) applies POT diffs, (c) merges safely (preserve translations; flag conflicts), and (d) emits updated PO files.  
- Provide UI to select which bridges to push to and which languages to include; supply dry-run and review screens.  
- Acceptance criteria: Running "Process Keys -> Merge -> Push" updates POT/PO locally and, when confirmed, writes to selected bridges.

Phase 4 — Tests & CI
- Add Editor tests covering: PO merge logic, mapping to Bridge schema, push dry-run, and successful push flows (mock network for Google).  
- Add CI job templates for headless import/export (secret injection for service account keys).  
- Acceptance criteria: Unit/Editor tests cover critical logic and run in CI with secrets injected securely.

Phase 5 — Docs, UX polish, and release
- Update in-repo documentation pages (overview, workflow, Google Sheets/Excel pages) with step-by-step push instructions and security guidance.  
- Add screenshots and decision notes; include migration notes for existing users.  
- Acceptance criteria: Docs updated, UI labelled, and a release note explaining opt-in behavior and admin steps.

## Detailed task breakdown (high level)
- Design: choose XLSX writer and merge behavior; list required Google scopes.  
- Implement Google Push API + Editor UI (+ tests).  
- Implement XLSX export (+ tests).  
- Implement PO merge engine and integrate into LocaProcessor with UI.  
- Add Editor tests, CI scripts, and secret handling docs.  
- Update documentation and gh-pages pages; add image assets.

## Security & operational notes
- **Service account keys must never be committed.** Document `.gitignore` and CI secret usage.  
- Use the narrowest Google scopes possible (e.g., `https://www.googleapis.com/auth/spreadsheets` or `drive.file` if creating files) and document required sharing settings.  
- Implement token caching and expiry handling; provide clear error messages on permission failures.  
- Provide a dry-run mode and require explicit confirmation before writing to remote sheets.

## Merge and conflict strategy (recommended default)
- **Default behavior**: Merge-only — add new msgid rows, populate empty `msgstr` with incoming translations, and update `msgstr` only if translator-approved (interactive review).  
- **Overwrite policy**: Optional explicit mode where incoming PO values overwrite existing spreadsheet cells.  
- **Conflict reporting**: Produce a simple diff/preview UI listing keys that would be overwritten and require explicit approval.

## Libraries and dependencies
- **Reading**: ExcelDataReader (already used) — keep as-is.  
- **Writing**: Evaluate ClosedXML (friendly API, MIT), EPPlus (licensing concerns for commercial use), or produce CSV fallback.  
- **Google API**: Use direct HTTP calls (existing UnityWebRequest approach) to Google Sheets REST endpoints; use OAuth via service account JWT flow (existing helper).  

## Open questions for you
1. Preferred XLSX writer (ClosedXML recommended unless you prefer EPPlus)?  
2. Default merge strategy (merge-only vs overwrite-by-default)?  
3. Should pushes be automatic after key extraction or always require explicit user confirmation?  

## Next steps (if you approve the plan)
- Confirm the three open questions above.  
- Pick the first todo to start (I recommend `loca-gettext-spreadsheet-integration` for design, then `loca-google-sheets-push`).  
- After confirmation, I can draft the PRs and implement phase-by-phase, running Editor tests and validating in the dev Unity app.


---

Plan created by Copilot CLI on user's request. Adjust or tell me which decision to make next and I'll proceed to create implementation tasks or begin coding.