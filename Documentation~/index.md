---
layout: home
title: UI Toolkit
---

**Unity GUI Toolkit** is a runtime UI library for Unity 2022.3+, providing a rich set of components built on top of uGUI and TextMeshPro.

> ⚠️ This toolkit is a work in progress. APIs may change between versions.

## Installation

### As a Unity Package (UPM)

In your project's `Packages/manifest.json`, add:

```json
"de.phoenixgrafik.ui-toolkit": "https://github.com/Arkarit/unity-gui-toolkit.git#v-00-01-01"
```

Replace `#v-00-01-01` with the [release tag](https://github.com/Arkarit/unity-gui-toolkit/releases) you want.

### As a Sub-Repository

Clone the repository into a folder inside your Unity project's `Assets/` directory:

```
https://github.com/Arkarit/unity-gui-toolkit.git
```

### Working in the Repository

1. Pull the repository
2. Run `.Dev-App/Install.bat` (Windows) or `.Dev-App/install.sh` (macOS/Linux) — this sets up symlinks required for the dev app
3. Open `.Dev-App/Unity` in Unity Hub

## API Reference

The full API reference is generated with Doxygen and available [here](api/index.html).
