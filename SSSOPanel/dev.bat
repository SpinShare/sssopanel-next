@echo off
rem Start Electron in dev mode on Windows (ElectronNET.Core: .NET-first launch), same steps as dev.sh
rem The new architecture runs: dotnet run -> .NET launches Electron as child process.

setlocal
set DOTNET_ROLL_FORWARD=LatestMajor

cd /d "%~dp0"

rem Read properties from the project file, so versions only have to be bumped there
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "(Select-String -Path SSSOPanel.csproj -Pattern '<ElectronVersion>(.*)</ElectronVersion>').Matches[0].Groups[1].Value"`) do set ELECTRON_VERSION=%%v
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "(Select-String -Path SSSOPanel.csproj -Pattern '<TargetFramework>(.*)</TargetFramework>').Matches[0].Groups[1].Value"`) do set TARGET_FRAMEWORK=%%v
set ELECTRON_DIR=bin\Debug\%TARGET_FRAMEWORK%\win-x64\.electron\node_modules\electron

rem Voice bot (pharahp submodule), dev runs it on the system node
if not exist VoiceBot\index.js (
    git -C .. submodule update --init SSSOPanel/VoiceBot || exit /b 1
)
if not exist VoiceBot\node_modules (
    pushd VoiceBot
    call npm ci --omit=dev --no-fund --no-audit || (popd & exit /b 1)
    popd
)

dotnet build SSSOPanel.csproj -c Debug || exit /b 1

rem Electron's installer unpacks its binary with extract-zip, which silently stops
rem halfway on recent Node versions. It still downloads the zip, so unpack that instead.
if exist "%ELECTRON_DIR%\dist\electron.exe" goto electron_ok
set ELECTRON_ZIP=
for /f "delims=" %%z in ('dir /b /s "%LOCALAPPDATA%\electron\Cache\electron-v%ELECTRON_VERSION%-win32-x64.zip" 2^>nul') do set "ELECTRON_ZIP=%%z"
if not defined ELECTRON_ZIP (
    echo Electron binary is missing and no downloaded electron-v%ELECTRON_VERSION%-win32-x64.zip was found. 1>&2
    exit /b 1
)
echo ==^> Unpacking Electron from %ELECTRON_ZIP%
if exist "%ELECTRON_DIR%\dist" rmdir /s /q "%ELECTRON_DIR%\dist"
mkdir "%ELECTRON_DIR%\dist"
tar -xf "%ELECTRON_ZIP%" -C "%ELECTRON_DIR%\dist" || exit /b 1
<nul set /p "=electron.exe" > "%ELECTRON_DIR%\path.txt"
:electron_ok

rem The debug build loads the panel from the vite dev server, start it unless one is running already.
rem It gets its own window, close that to stop it (the next run reuses it if it's still open).
call :port_open
if not errorlevel 1 goto vite_ok
echo ==^> Starting vite dev server
if not exist UserInterface\node_modules (
    pushd UserInterface
    call npm install || (popd & exit /b 1)
    popd
)
start "vite (SSSOPanel)" /d "%~dp0UserInterface" /min cmd /c npx vite --strictPort
for /l %%i in (1,1,60) do (
    call :port_open && goto vite_ok
    timeout /t 1 /nobreak >nul
)
:vite_ok

dotnet run --no-build -c Debug -- -unpackeddotnet
exit /b %errorlevel%

:port_open
powershell -NoProfile -Command "try { (New-Object Net.Sockets.TcpClient).Connect('localhost', 5173); exit 0 } catch { exit 1 }"
exit /b %errorlevel%
