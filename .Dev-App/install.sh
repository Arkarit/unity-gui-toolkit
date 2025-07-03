#/usr/bin/env bash

# Setup symlinks for the Unity project
ln -s -v "$(pwd)/../Runtime" "$(pwd)/Unity/Assets/External/unity-gui-toolkit"
ln -s -v "$(pwd)/../Editor" "$(pwd)/Unity/Assets/External/unity-gui-toolkit-editor"
