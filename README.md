# SSSOPanel Next
A modern desktop streaming overlay for SpinShare SpeenOpen

## Overview
- [Setup](#setup)
- [OBS](#obs)
- [Themes](#themes)
- [Building](#building)

### Setup
You need the [.NET 10 SDK](https://dotnet.microsoft.com/download), [Node.js](https://nodejs.org) and git.
Clone with `--recursive` (or run `git submodule update --init`) to get the voice bot.

#### Quick start
The dev scripts do everything in one go: fetch the voice bot and install its packages, build the backend, install the UI packages, start the vite dev server and launch the panel.

Linux:
```sh
SSSOPanel/dev.sh
```

Windows:
```bat
SSSOPanel\dev.bat
```

On Windows vite gets its own minimized window, close it to stop the dev server. If it's still running, the next run reuses it.
Both scripts also work around Electron's installer sometimes leaving a half unpacked binary, by unpacking its downloaded zip themselves.

#### Manually
Install the UI packages and start the dev server
```sh
cd SSSOPanel/UserInterface && npm install && npm run dev
```

Then start the client in a seperate terminal
```sh
cd SSSOPanel && dotnet run
```

### OBS
The panel is only the control surface. The overlay itself is rendered in an OBS browser source, served by the panel on the same machine.

1. Start the panel
2. Add a **Browser** source in OBS with the URL `http://localhost:7274/#/screen` (the copy button in the panel header copies it), width `1920` and height `1080`
3. Put the browser source above your gameplay capture

The Match (Ingame) screen has a transparent 1920x540 cutout in the center for the gameplay, all other screens are opaque.
The overlay reconnects on its own when the panel is restarted.

When running a dev server, use `http://localhost:5173/#/screen` instead.

#### Scene switching
The panel can switch OBS scenes together with its screens, using the WebSocket server built into OBS 28 and newer.

1. In OBS, open **Tools > WebSocket Server Settings**, enable the server and note the password
2. In the panel settings, enable **Scene Switching** in the OBS group, enter the address (default `ws://localhost:4455`) and password, then press **Connect**
3. Pick an OBS scene for each screen and save. Screens set to **None** leave the current scene alone

### Themes
Each theme is a folder in `SSSOPanel/UserInterface/src/assets/themes/` with its background, logo and a `theme.json`, and is picked in the panel settings.
See [the themes README](SSSOPanel/UserInterface/src/assets/themes/README.md) for how to make your own.

### Building
#### Building the UI
```sh
cd SSSOPanel/UserInterface && npm run build
```

#### Building the application
```sh
cd SSSOPanel && dotnet build --configuration Release
```

 
