#!/bin/bash
# Start Electron in dev mode (ElectronNET.Core: .NET-first launch)
# The new architecture runs: dotnet run → .NET launches Electron as child process.

set -e

export DOTNET_ROLL_FORWARD=LatestMajor

cd "$(dirname "$0")"

ELECTRON_VERSION="30.4.0"
ELECTRON_DIR="bin/Debug/net10.0/linux-x64/.electron/node_modules/electron"

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

dotnet run --no-build -c Debug -- -unpackeddotnet
