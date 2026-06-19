@echo off
title HCR E-Invoicing System Installer Builder
echo ===================================================
echo   Building HCR E-Invoicing System Release Build...
echo ===================================================
echo.

:: Get the directory where this batch file lives (repo root)
set "REPO_ROOT=%~dp0"
:: Remove trailing backslash
if "%REPO_ROOT:~-1%"=="\" set "REPO_ROOT=%REPO_ROOT:~0,-1%"

set "SLN_FILE=%REPO_ROOT%\HCR-E-INVOICING-SYSTEM.sln"
set "PROJ_DIR=%REPO_ROOT%\HCR-E-INVOICING-SYSTEM"
set "ISS_FILE=%REPO_ROOT%\installer_setup.iss"

echo Repo Root: %REPO_ROOT%
echo Solution:  %SLN_FILE%
echo.

:: 1. Restore NuGet packages first
echo Restoring NuGet packages...
nuget restore "%SLN_FILE%"
if errorlevel 1 (
    :: Try msbuild restore as fallback
    msbuild "%SLN_FILE%" /t:Restore /p:Configuration=Release
)

:: 2. Build the project in Release configuration
echo.
echo Building Release...
msbuild "%SLN_FILE%" /p:Configuration=Release /p:Platform="Any CPU" /m
if errorlevel 1 (
    echo.
    echo [ERROR] Visual Studio / MSBuild failed! Please fix compiler errors.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================
echo   Obfuscating Code to Protect from Reverse Engineering...
echo ===================================================
echo.

:: Run Obfuscar to secure the code (skip if not found)
where obfuscar.console >nul 2>&1
if %ERRORLEVEL%==0 (
    obfuscar.console "%PROJ_DIR%\obfuscar.xml"
    if errorlevel 1 (
        echo.
        echo [WARNING] Code Obfuscation failed - continuing without obfuscation.
    ) else (
        :: Replace the original executable with the obfuscated one so Inno Setup picks it up
        copy /Y "%PROJ_DIR%\bin\Release\Obfuscated\HCR-E-INVOICING-SYSTEM.exe" "%PROJ_DIR%\bin\Release\HCR-E-INVOICING-SYSTEM.exe"
    )
) else (
    echo [WARNING] Obfuscar not found - skipping obfuscation step.
)

echo.
echo ===================================================
echo   Compiling Inno Setup Installer Package...
echo ===================================================
echo.

:: 3. Compile the installer using Inno Setup Compiler
set "ISCC_PATH="
if exist "%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" set "ISCC_PATH=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"
if exist "%LOCALAPPDATA%\Programs\Antigravity IDE\resources\app\node_modules\innosetup\bin\ISCC.exe" set "ISCC_PATH=%LOCALAPPDATA%\Programs\Antigravity IDE\resources\app\node_modules\innosetup\bin\ISCC.exe"

if "%ISCC_PATH%"=="" (
    echo [ERROR] Inno Setup compiler ISCC.exe not found!
    echo Please install Inno Setup 6 from: https://jrsoftware.org/issetup.php
    pause
    exit /b 1
)

:: Pass the REPO_ROOT as a define to the ISS script
"%ISCC_PATH%" /DProjectDir="%REPO_ROOT%" "%ISS_FILE%"
if errorlevel 1 (
    echo.
    echo [ERROR] Installer compilation failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================
echo   SUCCESS! Installer created successfully!
echo ===================================================
echo.
echo Location: %REPO_ROOT%\InstallerOutput\HCR_E_Invoicing_System_Setup.exe
echo.
pause
