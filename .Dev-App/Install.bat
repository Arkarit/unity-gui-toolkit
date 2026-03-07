::::::::::::::::::::::::::::::::::::::::
:: Elevate.cmd - Version 4
:: Automatically check & get admin rights
:: see "https://stackoverflow.com/a/12264592/1016343" for description
::::::::::::::::::::::::::::::::::::::::
rem @echo off
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
 ECHO Dir: "%batchDir%"

 REM Resolve repo root (parent of .Dev-App)
 pushd "%batchDir%.."
 set "REPO_ROOT=%CD%"
 popd
 
 REM Resolve parent directory
 pushd "%REPO_ROOT%\.."
 set "REPO_PARENT=%CD%"
 popd
 
 set "GHPAGES_DIR=%REPO_PARENT%\unity-gui-toolkit-gh-pages"
 
 if exist "%GHPAGES_DIR%\.git" (
   ECHO gh-pages repo already exists at "%GHPAGES_DIR%"
 ) else (
   ECHO Creating gh-pages working copy at "%GHPAGES_DIR%"
    git clone "%REPO_ROOT%" "%GHPAGES_DIR%"
    if errorlevel 1 (
      ECHO Git clone failed. Skipping gh-pages setup.
    ) else (
      pushd "%GHPAGES_DIR%"
      REM try to copy origin URL from the source repo so credentials/remote match
      set "SRC_ORIGIN="
      REM write origin URL to a temp file to avoid FOR/backquote parsing inside parenthesis
      git -C "%REPO_ROOT%" remote get-url origin 2>nul > "%TEMP%\src_origin.txt" || (echo)
      if exist "%TEMP%\src_origin.txt" (
        set /p SRC_ORIGIN=<"%TEMP%\src_origin.txt"
        del "%TEMP%\src_origin.txt"
      )
      if "%SRC_ORIGIN%"=="" (
        set "SRC_ORIGIN=git@github.com:Arkarit/unity-gui-toolkit-gh-pages.git"
        ECHO No origin found in source repo, using fallback: %SRC_ORIGIN%
      ) else (
        ECHO Using source repo origin: %SRC_ORIGIN%
      )
      git remote | findstr /R /C:"^origin$" >nul
      if errorlevel 1 (
        git remote add origin %SRC_ORIGIN%
        ECHO Added origin -> %SRC_ORIGIN%
      ) else (
        git remote set-url origin %SRC_ORIGIN%
        ECHO Set origin URL -> %SRC_ORIGIN%
      )
      git checkout gh-pages 2>nul || git checkout -b gh-pages
      popd
    )
 )

 ECHO Creating symlinks...
 if not exist "%batchDir%\Unity\Assets\External\unity-gui-toolkit" (
   mklink /D "%batchDir%\Unity\Assets\External\unity-gui-toolkit" "%batchDir%..\Runtime"
 ) else (
   ECHO Symlink already exists: unity-gui-toolkit
 )
 if not exist "%batchDir%\Unity\Assets\External\unity-gui-toolkit-editor" (
   mklink /D "%batchDir%\Unity\Assets\External\unity-gui-toolkit-editor" "%batchDir%..\Editor"
 ) else (
   ECHO Symlink already exists: unity-gui-toolkit-editor
 )
 
 ECHO done
 REM pause
 rem exit
