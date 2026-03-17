---
layout: default
title: Contributing
---

# Contributing

This page explains how to set up a local development environment to work on the toolkit itself.

## Prerequisites

- Git
- Unity Hub with Unity 2022.3 LTS (or newer)
- Windows, macOS, or Linux

## Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/Arkarit/unity-gui-toolkit.git
   ```

2. Run the install script **as a normal user** (do not run as administrator):
   - **Windows:** `.Dev-App/Install.bat`
   - **macOS / Linux:** `.Dev-App/install.sh`

   The script creates symlinks that link `Runtime/` and `Editor/` from the repository root into the dev Unity project's `Assets/External/unity-gui-toolkit/` folder, so changes to the source are reflected immediately without a copy step.

3. Open `.Dev-App/Unity` in Unity Hub.

> **Important (Windows):** Do not run `Install.bat` with administrator privileges manually. The script requests UAC elevation only for the symlink step. Running the whole script as admin causes the companion gh-pages repository to be cloned with incorrect ownership, which prevents Git operations inside it.

## Repository Structure

```
unity-gui-toolkit/
├── Runtime/          ← Runtime C# source (symlinked into dev project)
├── Editor/           ← Editor-only C# source (symlinked into dev project)
├── .Dev-App/
│   ├── Unity/        ← Dev Unity project (open this in Unity Hub)
│   ├── Install.bat   ← Windows setup script
│   └── install.sh    ← macOS/Linux setup script
├── package.json      ← UPM package manifest
└── CHANGELOG.md
```

Documentation lives in the `gh-pages` branch (a separate working copy created by `Install.bat` at `.Dev-App/../unity-gui-toolkit-gh-pages/`).

## Running the Tests

Open the dev project in Unity, then:

**Window → General → Test Runner → Editor** — run all tests in the `de.phoenixgrafik.ui-toolkit.Test.Editor` assembly.
