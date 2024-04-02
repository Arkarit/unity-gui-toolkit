 @echo off
 CLS
 ECHO Removing symlinks...
 set "batchDir=%~dp0"
 ECHO Dir: "%batchDir%"

 rmdir "%batchDir%\Unity\Assets\External\unity-gui-toolkit"
 rmdir "%batchDir%\Unity\Assets\External\unity-gui-toolkit-editor"
 del /F /Q "%batchDir%\Unity\Assets\External\*.meta"
 
 ECHO done
 pause
 exit