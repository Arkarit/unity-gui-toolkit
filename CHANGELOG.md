# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- **Gettext ↔ Google Sheets sync** (`LocaGettextSheetsSyncer`):
  - `[Create by PO]` inspector button — auto-generates column configuration from PO files on disk (language columns + plural forms)
  - `[Push new keys]` inspector button — appends keys from PO files that are missing in the Google Sheet; never overwrites existing cells
  - `[Pull from Sheets]` inspector button — merges translations from the linked Google Sheet into local PO files conservatively (only fills empty translations)
  - `AutoSyncAfterMerge` setting in `UiToolkitConfiguration` — auto-pushes new keys to Google Sheets after every POT→PO merge
- **POT→PO merge pipeline** (`PoMergeEngine`, `LocaPoMerger`):
  - Merge POT template changes into PO files while preserving existing translations
  - Marks removed keys as obsolete (`#~`) rather than deleting them
  - SSoT (Single Source of Truth) header protection — spreadsheet-managed PO files are not overwritten by merge
  - `AutoMergePotToPo` setting in `UiToolkitConfiguration`
  - Manual merge menu: `Gui Toolkit > Localization > Merge POT into PO Files`
- **PO file backup management** (`PoBackupManager`) — keeps up to 10 backup revisions per file; accessible via editor window
- **Google Sheets push** (`LocaExcelBridgePusher`) — write in-memory translations back to the linked Google Sheet
- **CSV export** (`LocaCsvExporter`) — export all PO translations to CSV for offline review
- **UiLocalizedTextMeshProUGUI** — `LocaManager` bootstrap race-condition fix via coroutine retry
