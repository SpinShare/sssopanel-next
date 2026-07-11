#!/bin/bash
# Build a portable Linux AppImage using podman for the .NET publish step.
#
# Why: The host's .NET SDK (Linuxbrew) bakes its own ld.so path into the
# single-file apphost ELF interpreter, making the resulting AppImage
# non-portable.  Building inside the official Microsoft SDK container
# produces a binary with the standard /lib64/ld-linux-x86-64.so.2.
#
# The build is split: .NET publish runs in podman (no npm/FUSE needed),
# then electron-builder runs on the host (needs FUSE for AppImage).
#
# Usage: ./build-appimage.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

DOTNET_IMAGE="mcr.microsoft.com/dotnet/sdk:10.0"
ELECTRON_BUILDER_VERSION="26.0"
PUBTMP="obj/Release/net10.0/linux-x64/PubTmp"
PUBLISH_DIR="bin/Release/net10.0/linux-x64/publish"

echo "==> [1/5] Building Vue UI (host)"
(
    cd UserInterface
    npm install
    npm run build
)

echo "==> [2/5] Staging UI into wwwroot"
rm -rf Resources/wwwroot/assets
rm -f Resources/wwwroot/index.html Resources/wwwroot/favicon.ico
cp -r UserInterface/dist/. Resources/wwwroot/

echo "==> [3/5] Publishing .NET backend in podman ($DOTNET_IMAGE)"
# Clear any stale output so the success check below only passes if *this* run
# actually emitted the apphost binary.
rm -rf "$PUBTMP/bin"
# The ElectronPublishApp target (in ElectronNET.Core) tries to run npm install
# for electron-builder, which is absent inside the container.  However, the .NET
# single-file apphost is emitted BEFORE that step — it's the only output we need
# from the container.  The target failure is therefore harmless; swallow it.
podman run --rm \
    -v "$SCRIPT_DIR:/work:Z" \
    -w /work \
    -e DOTNET_ROLL_FORWARD=LatestMajor \
    "$DOTNET_IMAGE" \
    dotnet publish SSSOPanel.csproj -c Release -r linux-x64 || \
    test -f "$PUBTMP/bin/SSSOPanel"

echo "==> [4/5] Staging wwwroot into publish temp dir"
mkdir -p "$PUBTMP/bin/wwwroot"
cp -r Resources/wwwroot/* "$PUBTMP/bin/wwwroot/"

echo "==> [5/5] Running electron-builder on host (needs FUSE for AppImage)"
(
    cd "$PUBTMP"
    [ -d node_modules ] || npm install "electron-builder@${ELECTRON_BUILDER_VERSION}" --save-dev
    mkdir -p "$SCRIPT_DIR/$PUBLISH_DIR"
    npx electron-builder \
        --config=./electron-builder.json \
        --linux --x64 \
        -c.electronVersion=30.4.0 \
        -c.directories.output "$SCRIPT_DIR/$PUBLISH_DIR" \
        -c.appId "com.spinshare.sssopanel" \
        -c.buildVersion "1.3.0" \
        -c.extraResources "bin/**/*"
)

echo ""
echo "==> Done! AppImage:"
ls -lh "$PUBLISH_DIR/com.spinshare.sssopanel-x86_64-1.3.0.AppImage"