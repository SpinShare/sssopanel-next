#!/bin/bash
# Start Electron in dev mode (loads Vite URL)
# Bypasses electronize CLI (incompatible with npm v10+ allowScripts) by running steps manually.

set -e

export DOTNET_ROLL_FORWARD=LatestMajor

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
HOST_DIR="$SCRIPT_DIR/obj/Host"

# Step 1: Publish .NET backend
echo "Publishing .NET backend..."
dotnet publish "$SCRIPT_DIR/SSSOPanel.csproj" \
    -r linux-x64 -c "Debug" \
    --output "$HOST_DIR/bin" \
    /p:PublishReadyToRun=true /p:PublishSingleFile=true --no-self-contained \
    2>&1 | tail -1

# Step 2: Install npm deps (skip scripts to avoid npm v10+ block)
if [ ! -d "$HOST_DIR/node_modules" ]; then
    echo "Installing npm packages..."
    (cd "$HOST_DIR" && npm install --ignore-scripts)
fi

# Step 3: Ensure electron binary is present
ELECTRON_BIN="$HOST_DIR/node_modules/electron/dist/electron"
if [ ! -f "$ELECTRON_BIN" ]; then
    echo "Downloading Electron binary..."
    (cd "$HOST_DIR/node_modules/electron" && node -e "
        const { downloadArtifact } = require('@electron/get');
        const { version } = require('./package.json');
        downloadArtifact({ version, artifactName: 'electron', platform: 'linux', arch: 'x64' })
            .then(p => console.log('Downloaded:', p))
            .catch(e => { console.error(e); process.exit(1); });
    ")
    (cd "$HOST_DIR/node_modules/electron" && \
        ZIP=$(find ~/.cache/electron -name "electron-*-linux-x64.zip" | head -1) && \
        unzip -q -o "$ZIP" -d dist/ && \
        echo "electron" > path.txt && \
        chmod +x dist/electron)
    echo "Electron binary ready"
fi

# Step 4: Launch Electron (same as electronize would)
echo "Starting Electron..."
cd "$HOST_DIR"
exec "$ELECTRON_BIN" "$HOST_DIR/main.js"
