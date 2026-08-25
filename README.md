# unity-gui-toolkit
Generic Unity GUI toolkit

Please note that this gui toolkit for Unity is heavy work in progress. The API reference (doxygen)
is still incomplete, but the parts you need to get going are written up:

| Read this | For |
| --- | --- |
| [BEST-PRACTICES.md](BEST-PRACTICES.md) | **Start here when setting the toolkit up in a project.** The three things a project should take ownership of on day one — prefab variants, the style config, and what `IsApplicable` decides — all cheap at setup and expensive to retrofit. |
| [mcp~/README.md](mcp~/README.md) | AI screen authoring: the MCP server, its setup, the full screen-description vocabulary, and the tool reference. |
| [CHANGELOG.md](CHANGELOG.md) | What changed, and why — the reasoning, not just the list. |
| [CLAUDE.md](CLAUDE.md) / [AGENTS.md](AGENTS.md) | Architecture overview for agents working in this repo. Useful to humans too, and kept in sync with each other. |

## Installation

### As a Unity package:
- In the project you'd like to use the unity-gui-toolkit in, edit the file Packages/manifest.json
- Enter the line "de.phoenixgrafik.ui-toolkit": "https://github.com/Arkarit/unity-gui-toolkit.git#v-00-01-01" (where #v-00-01-01 marks the release tag you'd like to use).

### As a Sub repo:
- In the project you'd like to use the unity-gui-toolkit in, add the unity-gui-toolkit repo (https://github.com/Arkarit/unity-gui-toolkit.git) as a sub repo in a folder of your choice within the Unity Assets folder. 
- Choose the branch or tag of your choice. Keep in mind: Master may be sometimes broken.

### Work in the repo itself:
- Pull the repo (https://github.com/Arkarit/unity-gui-toolkit.git)
- Execute the batch file .Dev-App/Install.bat **as a normal user** (do not run as administrator — the script will request elevation only when needed)
- In Unity hub, open the folder .Dev-App\Unity

**Important:** Do not run Install.bat with administrator privileges manually. The script handles privilege elevation automatically and running it as admin will cause the gh-pages documentation repository to be created with incorrect ownership, preventing Git operations.

