@echo off
title SDK E-Invoicing System Installer Builder
echo ===================================================
echo   Building SDK E-Invoicing System Release Build...
echo ===================================================
echo.

:: 1. Build the project in Release configuration
dotnet build -c Release "c:\Users\PC\source\repos\SDK-E-INVOICING-SYSTEM\SDK-E-INVOICING-SYSTEM.sln"
if errorlevel 1 (
    echo.
    echo [ERROR] Visual Studio / dotnet Build failed! Please fix compiler errors.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================
echo   Obfuscating Code to Protect from Reverse Engineering...
echo ===================================================
echo.

:: Run Obfuscar to secure the code
obfuscar.console "c:\Users\PC\source\repos\SDK-E-INVOICING-SYSTEM\SDK-E-INVOICING-SYSTEM\obfuscar.xml"
if errorlevel 1 (
    echo.
    echo [ERROR] Code Obfuscation failed!
    pause
    exit /b %ERRORLEVEL%
)

:: Replace the original executable with the obfuscated one so Inno Setup picks it up
copy /Y "c:\Users\PC\source\repos\SDK-E-INVOICING-SYSTEM\SDK-E-INVOICING-SYSTEM\bin\Release\Obfuscated\SDK-E-INVOICING-SYSTEM.exe" "c:\Users\PC\source\repos\SDK-E-INVOICING-SYSTEM\SDK-E-INVOICING-SYSTEM\bin\Release\SDK-E-INVOICING-SYSTEM.exe"
if errorlevel 1 (
    echo.
    echo [ERROR] Failed to copy obfuscated executable!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===================================================
echo   Compiling Inno Setup Installer Package...
echo ===================================================
echo.

:: 2. Compile the installer using Inno Setup Compiler
:: We search in standard installation locations
set "ISCC_PATH="
if exist "C:\Users\PC\AppData\Local\Programs\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Users\PC\AppData\Local\Programs\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set "ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe"

if "%ISCC_PATH%"=="" (
    echo [ERROR] Inno Setup compiler ISCC.exe not found!
    echo Please make sure Inno Setup is installed properly.
    pause
    exit /b 1
)

"%ISCC_PATH%" "c:\Users\PC\source\repos\SDK-E-INVOICING-SYSTEM\installer_setup.iss"
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
echo Location: c:\Users\PC\source\repos\SDK-E-INVOICING-SYSTEM\InstallerOutput\SDK_E_Invoicing_System_Setup.exe
echo.
pause
