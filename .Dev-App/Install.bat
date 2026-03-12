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
  
  REM ::::::::::::::::::::::::::::
  REM NON-ADMIN SECTION (before elevation)
  REM This section runs as the normal (non-admin) user and handles the gh-pages
  REM working copy. It must run BEFORE elevation because symlinks created by an
  REM elevated process are owned by the admin account, which causes Git to treat
  REM the cloned repo as an "unsafe directory" for the normal user.
  REM ::::::::::::::::::::::::::::

  REM Check that git is available before doing anything git-related.
  where git >nul 2>nul
  if errorlevel 1 (
    ECHO ERROR: 'git' was not found in PATH. Please install Git and re-run this script.
    pause
    exit /B 1
  )

  REM Resolve repo root (parent of .Dev-App, i.e. the package root).
  pushd "%batchDir%.."
  set "REPO_ROOT=%CD%"
  popd

  REM The gh-pages working copy is placed NEXT TO the package repo (not inside it)
  REM so it does not appear as an untracked subfolder in the main repo's git status.
  pushd "%REPO_ROOT%\.."
  set "REPO_PARENT=%CD%"
  popd

  set "GHPAGES_DIR=%REPO_PARENT%\unity-gui-toolkit-gh-pages"

  if exist "%GHPAGES_DIR%\.git" (
    ECHO gh-pages repo already exists at "%GHPAGES_DIR%"
  ) else (
    ECHO Creating gh-pages working copy at "%GHPAGES_DIR%" ^(as current user^)
    mkdir "%GHPAGES_DIR%" 2>nul
    REM Copy the .git metadata from the main repo so the new working copy shares
    REM the same remote URL and stored credentials without needing a fresh clone.
    ECHO Copying .git directory to share remote and credentials...
    xcopy /E /I /H /Y "%REPO_ROOT%\.git" "%GHPAGES_DIR%\.git" >nul
    if errorlevel 1 (
      ECHO Failed to copy .git directory. Skipping gh-pages setup.
    ) else (
      pushd "%GHPAGES_DIR%"
      ECHO Checking out gh-pages branch...
      git checkout -f gh-pages 2>nul
      if errorlevel 1 git checkout -b gh-pages
      popd
    )
  )
  
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
 ::START (Admin section - symlinks only)
 ::::::::::::::::::::::::::::
 REM Run shell as admin (example) - put here code as you like
 ECHO Dir: "%batchDir%"

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
 exit
