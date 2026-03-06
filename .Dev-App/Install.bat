::::::::::::::::::::::::::::::::::::::::
:: Elevate.cmd - Version 4
:: Automatically check & get admin rights
:: see "https://stackoverflow.com/a/12264592/1016343" for description
::::::::::::::::::::::::::::::::::::::::
 @echo off
 CLS
 ECHO.
 ECHO =============================
 ECHO Running Admin shell
 ECHO =============================

:init
 setlocal DisableDelayedExpansion
 set cmdInvoke=1
 set winSysFolder=System32
 set "batchPath=%~dpnx0"
 set "batchDir=%~dp0"
 rem this works also from cmd shell, other than %~0
 for %%k in (%0) do set batchName=%%~nk
 set "vbsGetPrivileges=%temp%\OEgetPriv_%batchName%.vbs"
 setlocal EnableDelayedExpansion

:checkPrivileges
  NET FILE 1>NUL 2>NUL
  if '%errorlevel%' == '0' ( goto gotPrivileges ) else ( goto getPrivileges )

:getPrivileges
  if '%1'=='ELEV' (echo ELEV & shift /1 & goto gotPrivileges)
  ECHO.
  ECHO **************************************
  ECHO Invoking UAC for Privilege Escalation
  ECHO **************************************

  ECHO Set UAC = CreateObject^("Shell.Application"^) > "%vbsGetPrivileges%"
  ECHO args = "ELEV " >> "%vbsGetPrivileges%"
  ECHO For Each strArg in WScript.Arguments >> "%vbsGetPrivileges%"
  ECHO args = args ^& strArg ^& " "  >> "%vbsGetPrivileges%"
  ECHO Next >> "%vbsGetPrivileges%"
  
  if '%cmdInvoke%'=='1' goto InvokeCmd 

  ECHO UAC.ShellExecute "!batchPath!", args, "", "runas", 1 >> "%vbsGetPrivileges%"
  goto ExecElevation

:InvokeCmd
  ECHO args = "/c """ + "!batchPath!" + """ " + args >> "%vbsGetPrivileges%"
  ECHO UAC.ShellExecute "%SystemRoot%\%winSysFolder%\cmd.exe", args, "", "runas", 1 >> "%vbsGetPrivileges%"

:ExecElevation
 "%SystemRoot%\%winSysFolder%\WScript.exe" "%vbsGetPrivileges%" %*
 exit /B

:gotPrivileges
 setlocal & cd /d %~dp0
 if '%1'=='ELEV' (del "%vbsGetPrivileges%" 1>nul 2>nul  &  shift /1)

 ::::::::::::::::::::::::::::
 ::START
 ::::::::::::::::::::::::::::
 REM Run shell as admin (example) - put here code as you like
 ECHO Creating symlinks...
 ECHO Dir: "%batchDir%"

 mklink /D "%batchDir%\Unity\Assets\External\unity-gui-toolkit" "%batchDir%..\Runtime"
 mklink /D "%batchDir%\Unity\Assets\External\unity-gui-toolkit-editor" "%batchDir%..\Editor"

 ECHO done
 REM Setup optional gh-pages working copy
 set "GHPAGES_DIR=%batchDir%..\unity-gui-toolkit-gh-pages"
 if exist "%GHPAGES_DIR%\.git" (
   ECHO gh-pages repo already exists at "%GHPAGES_DIR%"
 ) else (
   ECHO Creating gh-pages working copy at "%GHPAGES_DIR%"
   pushd "%batchDir%.."
   git clone "%batchDir%.." "%GHPAGES_DIR%"
   if errorlevel 1 (
     ECHO Git clone failed (is git installed?). Skipping gh-pages setup.
   ) else (
     pushd "%GHPAGES_DIR%"
     git checkout gh-pages 2>nul || git checkout -b gh-pages
     popd
   )
   popd
 )
 pause
 exit
