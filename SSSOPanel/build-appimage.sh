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

# Read a property from the project file, so versions only have to be bumped there
csproj_property() {
    sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" SSSOPanel.csproj | head -n 1
}

TARGET_FRAMEWORK="$(csproj_property TargetFramework)"
APP_VERSION="$(csproj_property Version)"
APP_ID="$(csproj_property ElectronPackageId)"
ELECTRON_VERSION="$(csproj_property ElectronVersion)"

for value in "$TARGET_FRAMEWORK" "$APP_VERSION" "$APP_ID" "$ELECTRON_VERSION"; do
    if [ -z "$value" ]; then
        echo "Could not read TargetFramework, Version, ElectronPackageId or ElectronVersion from SSSOPanel.csproj" >&2
        exit 1
    fi
done

RID="linux-x64"
DOTNET_IMAGE="mcr.microsoft.com/dotnet/sdk:${TARGET_FRAMEWORK#net}"
# Not set in the project file, ElectronNET.Core picks its own default there
ELECTRON_BUILDER_VERSION="26.0"
PUBTMP="obj/Release/$TARGET_FRAMEWORK/$RID/PubTmp"
PUBLISH_DIR="bin/Release/$TARGET_FRAMEWORK/$RID/publish"

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
# ElectronNET.Core runs npm while building (Electron host dependencies), but the
# SDK image ships without Node, so install it first.
# The ElectronPublishApp target then tries to package with electron-builder, which
# can fail inside the container.  The .NET single-file apphost is emitted BEFORE
# that step and is the only publish output we need, so that failure is harmless.
podman run --rm \
    -v "$SCRIPT_DIR:/work:Z" \
    -w /work \
    -e DOTNET_ROLL_FORWARD=LatestMajor \
    -e RID="$RID" \
    "$DOTNET_IMAGE" \
    sh -c 'apt-get update -qq && apt-get install -y -qq --no-install-recommends nodejs npm > /dev/null && dotnet publish SSSOPanel.csproj -c Release -r "$RID"' || \
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
        -c.electronVersion="$ELECTRON_VERSION" \
        -c.directories.output "$SCRIPT_DIR/$PUBLISH_DIR" \
        -c.appId "$APP_ID" \
        -c.buildVersion "$APP_VERSION" \
        -c.extraResources "bin/**/*"
)

echo ""
echo "==> Done! AppImage:"
ls -lh "$PUBLISH_DIR"/*.AppImage