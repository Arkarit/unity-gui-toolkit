# unity-gui-toolkit
Generisches Unity GUI Toolkit

Bitte beachten: Dieses GUI Toolkit für Unity ist noch stark in Entwicklung.
Insbesondere gibt es noch keine Dokumentation, außer Doxygen (aber auch das ist noch sehr unvollständig).
Es ist jedoch zumindest teilweise verwendbar.

## Installation

### Als Unity Package:
- In dem Projekt, in dem du das unity-gui-toolkit verwenden möchtest, editiere die Datei Packages/manifest.json
- Füge die Zeile "de.phoenixgrafik.ui-toolkit": "https://github.com/Arkarit/unity-gui-toolkit.git#v-00-01-01" ein (wobei #v-00-01-01 das Release-Tag markiert, das du verwenden möchtest).

### Als Sub Repo:
- In dem Projekt, in dem du das unity-gui-toolkit verwenden möchtest, füge das unity-gui-toolkit Repo (https://github.com/Arkarit/unity-gui-toolkit.git) als Sub-Repo in einem Ordner deiner Wahl innerhalb des Unity Assets-Ordners hinzu.
- Wähle den Branch oder Tag deiner Wahl. Beachte: Master kann manchmal defekt sein.

### Arbeit im Repo selbst:
- Klone das Repo (https://github.com/Arkarit/unity-gui-toolkit.git)
- Führe die Batch-Datei .Dev-App/Install.bat **als normaler Benutzer** aus (nicht als Administrator ausführen — das Skript fordert automatisch Berechtigungen an, wenn nötig)
- Öffne in Unity Hub den Ordner .Dev-App\Unity

**Wichtig:** Führe Install.bat nicht manuell mit Administrator-Rechten aus. Das Skript behandelt die Rechte-Erhöhung automatisch und eine Ausführung als Admin würde dazu führen, dass das gh-pages Dokumentations-Repository mit falschen Besitzrechten erstellt wird, was Git-Operationen verhindert.
