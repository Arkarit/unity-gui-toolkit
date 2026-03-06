---
layout: default
title: Setup
---

# Setup

## Installation

### As a Unity Package (UPM)

In your project's `Packages/manifest.json`, add the following line to the `dependencies` block:

```json
"de.phoenixgrafik.ui-toolkit": "https://github.com/Arkarit/unity-gui-toolkit.git#v-00-01-01"
```

Replace `#v-00-01-01` with the [release tag](https://github.com/Arkarit/unity-gui-toolkit/releases) you want to use.

### As a Sub-Repository

Clone the repository into a folder inside your Unity project's `Assets/` directory:

```
git clone https://github.com/Arkarit/unity-gui-toolkit.git
```

Choose a branch or tag. Note that `master` may occasionally be unstable.

### Working in the Repository

If you want to contribute or work on the toolkit itself:

1. Clone the repository
2. Run `.Dev-App/Install.bat` (Windows) or `.Dev-App/install.sh` (macOS/Linux)
   — this creates symlinks linking `Runtime/` and `Editor/` into the dev Unity project
3. Open `.Dev-App/Unity` in Unity Hub
