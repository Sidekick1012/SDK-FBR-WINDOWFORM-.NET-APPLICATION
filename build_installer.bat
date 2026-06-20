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

:: 1. Detect MSBuild path
set "MSBUILD_PATH="

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if exist "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

:: Check 2019 if not found yet
if "%MSBUILD_PATH%"=="" (
    for /d %%i in ("C:\Program Files (x86)\Microsoft Visual Studio\2019\*") do (
        if exist "%%i\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=%%i\MSBuild\Current\Bin\MSBuild.exe"
    )
)
if "%MSBUILD_PATH%"=="" (
    for /d %%i in ("C:\Program Files\Microsoft Visual Studio\2019\*") do (
        if exist "%%i\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD_PATH=%%i\MSBuild\Current\Bin\MSBuild.exe"
    )
)

if "%MSBUILD_PATH%"=="" (
    where msbuild >nul 2>&1
    if %ERRORLEVEL%==0 (
        set "MSBUILD_PATH=msbuild"
    )
)

if "%MSBUILD_PATH%"=="" (
    echo [ERROR] MSBuild.exe not found! Please make sure Visual Studio 2022/2019 or Build Tools is installed.
    pause
    exit /b 1
)

echo Found MSBuild: "%MSBUILD_PATH%"

:: 2. Detect/Download NuGet path
set "NUGET_CMD="
where nuget >nul 2>&1
if %ERRORLEVEL%==0 (
    set "NUGET_CMD=nuget"
) else (
    if exist "%REPO_ROOT%\nuget.exe" (
        set "NUGET_CMD=%REPO_ROOT%\nuget.exe"
    ) else (
        echo NuGet.exe not found. Downloading NuGet...
        powershell -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dist.nuget.org/win-x86-commandline/latest/nuget.exe' -OutFile '%REPO_ROOT%\nuget.exe'" >nul 2>&1
        if exist "%REPO_ROOT%\nuget.exe" (
            set "NUGET_CMD=%REPO_ROOT%\nuget.exe"
        )
    )
)

:: 3. Restore NuGet packages
echo Restoring NuGet packages...
if not "%NUGET_CMD%"=="" (
    "%NUGET_CMD%" restore "%SLN_FILE%"
) else (
    "%MSBUILD_PATH%" "%SLN_FILE%" /t:Restore /p:Configuration=Release
)

:: 4. Build the project in Release configuration
echo.
echo Building Release...
"%MSBUILD_PATH%" "%SLN_FILE%" /p:Configuration=Release /p:Platform="Any CPU" /m
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
    pushd "%PROJ_DIR%"
    obfuscar.console "obfuscar.xml"
    if errorlevel 1 (
        echo.
        echo [WARNING] Code Obfuscation failed - continuing without obfuscation.
    ) else (
        :: Replace the original executable with the obfuscated one so Inno Setup picks it up
        copy /Y "bin\Release\Obfuscated\HCR-E-INVOICING-SYSTEM.exe" "bin\Release\HCR-E-INVOICING-SYSTEM.exe"
    )
    popd
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
