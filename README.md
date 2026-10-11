# SSSOPanel Next
A modern desktop streaming overlay for SpinShare SpeenOpen

## Overview
- [Setup](#setup)
- [OBS](#obs)
- [Building](#building)

### Setup
#### Installing all dependencies
```sh
cd SSSOPanel/UserInterface && npm install
```

#### Running a dev server
```sh
cd SSSOPanel/UserInterface && npm run dev
```

#### Starting the client
In a seperate terminal
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

### Building
#### Building the UI
```sh
cd SSSOPanel/UserInterface && npm run build
```

#### Building the application
```sh
cd SSSOPanel && dotnet build --configuration Release
```

 
