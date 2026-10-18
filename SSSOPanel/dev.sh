#!/bin/bash
# Start Electron in dev mode (ElectronNET.Core: .NET-first launch)
# The new architecture runs: dotnet run → .NET launches Electron as child process.

set -e

export DOTNET_ROLL_FORWARD=LatestMajor

cd "$(dirname "$0")"

# Read a property from the project file, so versions only have to be bumped there
csproj_property() {
    sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" SSSOPanel.csproj | head -n 1
}

ELECTRON_VERSION="$(csproj_property ElectronVersion)"
ELECTRON_DIR="bin/Debug/$(csproj_property TargetFramework)/linux-x64/.electron/node_modules/electron"

# Voice bot (pharahp submodule), dev runs it on the system node
[ -f VoiceBot/index.js ] || git -C .. submodule update --init SSSOPanel/VoiceBot
[ -d VoiceBot/node_modules ] || (cd VoiceBot && npm ci --omit=dev --no-fund --no-audit)

dotnet build SSSOPanel.csproj -c Debug

# Electron's installer unpacks its binary with extract-zip, which silently stops
# halfway on recent Node versions. It still downloads the zip, so unpack that instead.
if [ ! -x "$ELECTRON_DIR/dist/electron" ]; then
    ELECTRON_ZIP="$(find "${XDG_CACHE_HOME:-$HOME/.cache}/electron" -name "electron-v${ELECTRON_VERSION}-linux-x64.zip" 2>/dev/null | head -n 1)"
    if [ -z "$ELECTRON_ZIP" ]; then
        echo "Electron binary is missing and no downloaded electron-v${ELECTRON_VERSION}-linux-x64.zip was found." >&2
        exit 1
    fi

    echo "==> Unpacking Electron from $ELECTRON_ZIP"
    rm -rf "$ELECTRON_DIR/dist"
    unzip -q "$ELECTRON_ZIP" -d "$ELECTRON_DIR/dist"
    printf 'electron' > "$ELECTRON_DIR/path.txt"
fi

# The debug build loads the panel from the vite dev server, start it unless one is running already
if ! (exec 3<>/dev/tcp/localhost/5173) 2>/dev/null; then
    echo "==> Starting vite dev server"
    (
        cd UserInterface
        [ -d node_modules ] || npm install
        exec npx vite --strictPort
    ) &
    VITE_PID=$!
    trap 'kill "$VITE_PID" 2>/dev/null' EXIT

    for _ in $(seq 1 60); do
        (exec 3<>/dev/tcp/localhost/5173) 2>/dev/null && break
        sleep 0.5
    done
fi

dotnet run --no-build -c Debug -- -unpackeddotnet
