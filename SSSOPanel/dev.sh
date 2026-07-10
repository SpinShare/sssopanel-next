#!/bin/bash
# Start Electron in dev mode (ElectronNET.Core: .NET-first launch)
# The new architecture runs: dotnet run → .NET launches Electron as child process.

set -e

export DOTNET_ROLL_FORWARD=LatestMajor

cd "$(dirname "$0")"
dotnet run -c Debug -- -unpackeddotnet
