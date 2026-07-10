# SSSOPanel Next — Agent Guide

**IMPORTANT**: When a phase is completed, commit the changes and mark the phase as done (✅) in this file.

## Next Actions for This Agent

This project is migrating from **Photino.NET** to **Electron.NET**. Follow these steps in order:

1. **Plan (read-only)** — Read this file fully, explore the codebase, understand the current Photino.NET structure
2. ✅ **Phase 1: C# Project Setup** — Edit `SSSOPanel.csproj`: swap SDK to `Microsoft.NET.Sdk.Web`, remove Photino packages, add `ElectronNET.API`, add Electron config properties
3. ✅ **Phase 2: New Files** — Create `preload.js` and `custom_main.js` in `SSSOPanel/`
4. ✅ **Phase 3: C# Core** — Rewrite `Program.cs`, `ScreenManager.cs`, `MessageHandler.cs`, `ICommand.cs`
5. ✅ **Phase 4: C# Commands** — Update all 17 `Command*.cs` files (`PhotinoWindow?` → `BrowserWindow?`)
6. ✅ **Phase 5: Vue Frontend** — Update all 22 files using `window.external.*` → `window.electronAPI.*`
7. ✅ **Phase 6: Build & CI** — Create `electron.manifest.json`, update GitHub Actions, update dev script
8. ✅ **ElectronNET.Core Migration** — Upgrade from `ElectronNET.API` v23.6.1 to `ElectronNET.Core` v0.5.1. New console app startup via `ElectronNetRuntime.RuntimeController`. Dev command: `./dev.sh`
9. **Verify** — `npm run lint` (0 errors), `dotnet build` (0 errors), `./dev.sh` launches both windows

Detailed instructions for each phase are in the **Electron.NET Migration Plan** section below. If the project is already migrated, skip to the **Coding Standards** and **Commands** sections for daily development.

## Project Overview

Desktop streaming overlay application for SpinShare SpeenOpen (Spin Rhythm XD tournaments). Two-window desktop app: **Panel** (operator controls) and **Screen** (livestream overlay).

## Tech Stack

- **Backend**: C# .NET 10.0 desktop app via Electron.NET (Chromium-based windowing)
- **Frontend**: Vue 3 + Vite + Vue Router (hash-based history)
- **UI Library**: Custom "Spin" component library (`SpinButton`, `SpinInput`, etc.)
- **State/Events**: `mitt` event bus for IPC bridge, JSON command/response protocol
- **Styling**: SCSS with scoped styles, `@mdi/font` icons
- **Language**: Plain JavaScript (no TypeScript)

## Commands

### UI (run from `SSSOPanel/UserInterface/`)

| Command | Description |
|---------|-------------|
| `npm run dev` | Start Vite dev server on `:5173` |
| `npm run build` | Production build to `dist/` |
| `npm run lint` | ESLint (`.vue,.js,.jsx,.cjs,.mjs`) with `--fix` |
| `npm run prettier` | Prettier binary (no args, use IDE integration) |

### Backend (run from `SSSOPanel/`)

| Command | Description |
|---------|-------------|
| `dotnet build --configuration Release` | Build the full app |
| `./dev.sh` | Start Electron in dev mode (ElectronNET.Core, .NET-first launch) |

### Tests

**No test framework is configured.** There are zero test projects, zero test files, and no test runner in the repo. `dotnet test` exists in CI but passes vacuously. To add tests, you'd need to add a test project (xUnit/NUnit) for C# and vitest for Vue.

### Dev Workflow

1. Terminal 1: `cd SSSOPanel/UserInterface && npm run dev` (Vite)
2. Terminal 2: `cd SSSOPanel && ./dev.sh` (Electron)
3. In debug mode, Electron loads `http://localhost:5173`

---

## Project Structure

```
SSSOPanel/                        # C# .NET backend
├── Program.cs                    # Entry point — creates Panel + Screen windows
├── SSSOPanel.csproj              # Project file (Microsoft.NET.Sdk.Web)
├── preload.js                    # Electron contextBridge IPC shim
├── electron/
│   └── custom_main.js            # Electron main process (permissions)
├── MessageParser/                # IPC command/response pattern
│   ├── ICommand.cs               # Command interface
│   ├── CommandFactory.cs         # Maps command strings → handler classes
│   ├── MessageHandler.cs         # IPC dispatch and response
│   └── Command*.cs               # Individual command handlers
├── ScreenManager/                # Manages screen window lifecycle
├── SettingsManager/              # JSON file-backed settings
├── UpdateManager/                # GitHub release checker
├── Properties/                   # launchSettings.json, electron-builder.json
├── Resources/wwwroot/            # Embedded production UI output
└── UserInterface/                # Vue 3 frontend
    ├── vite.config.js
    ├── .eslintrc.cjs
    ├── .prettierrc.json
    └── src/
        ├── main.js               # App bootstrap, global Spin components
        ├── App.vue               # Root — IPC bridge (mitt)
        ├── router.js             # Hash-based routes: /panel/*, /screen/*
        ├── layouts/              # AppLayout, ScreenLayout, AspectLayout
        ├── components/           # Spin component library + screen components
        ├── modules/              # Composables & utility modules
        └── views/Panel/          # Panel control views (11 files)
        └── views/Screen/         # Stream overlay views (8 files)
```

---

## Coding Standards — C#

- **File-scoped namespaces** everywhere (`namespace SSSOPanel.MessageParser;`)
- **Class naming**: PascalCase (`CommandStateSet`, `MessageHandler`)
- **Interface naming**: PascalCase with `I` prefix (`ICommand`)
- **Method naming**: PascalCase (`Execute`, `GetInstance`, `SendResponse`)
- **Private fields**: `_camelCase` with underscore prefix
- **Static fields**: `_camelCase` with underscore prefix
- **Constants**: PascalCase
- **Nullable reference types**: Enabled (`<Nullable>enable</Nullable>`), use `?` notation on all nullable params
- **Async**: `ICommand.Execute()` returns `Task`; use `await Task.Yield()` at end of async void-less methods
- **Singletons**: Double-check locking pattern (`GetInstance()` with `lock` on private `static readonly object`)
- **Error handling**: Throw `ArgumentNullException`/`InvalidOperationException` for invalid states; silent catch with `// ignored` for non-critical failures
- **Logging**: `Console.WriteLine` only (no logger framework)
- **Using directives**: At top of file before namespace declaration
- **JSON**: `Newtonsoft.Json` (not System.Text.Json) for state/settings serialization

---

## Coding Standards — Vue/JS

- **Composition API** with `<script setup>` (primary). Note: `ScreenVCHeader.vue` and `VoiceMember.vue` use Options API (legacy).
- **Plain JavaScript** — no TypeScript anywhere
- **Naming**:
  - Component files: PascalCase (`SpinButton.vue`, `AppLayout.vue`)
  - JS module files: camelCase (`useCurrentScreen.js`, `useSpinShareApi.js`)
  - View files: `View[Panel|Screen][Name].vue` pattern
  - Constants: UPPER_SNAKE_CASE
  - Component names (global registration): `Spin*` prefix
- **Prettier** (`SSSOPanel/UserInterface/.prettierrc.json`):
  - `tabWidth: 4`, `useTabs: false`, `semi: true`, `singleQuote: true`
  - `trailingComma: "all"`, `printWidth: 1000000` (effectively disabled)
  - `singleAttributePerLine: true`
- **Imports**: ES modules only; prefer `@/` path alias over relative paths (`@/router`, `@/components/Common/SpinButton.vue`)
- **Props/Emits**: `defineProps({ ... })` with `type`, `default`, `required`; `defineEmits(['event-name'])`
- **Event bus**: `mitt` injected via `app.provide('emitter', new mitt())`; consume via `inject('emitter')`; always clean up with `emitter.off()` in `onUnmounted`
- **IPC bridge**: `window.electronAPI.send(channel, data)` sends to C#; `window.electronAPI.on(channel, callback)` receives JSON responses
- **Styling**: `<style lang="scss" scoped>` with nested selectors (`&` parent ref); global vars in `app.scss` (`--colorBase`, `--colorPrimary`, etc.)
- **Error handling**: `try/catch/finally` for async ops; `console.error()` for errors; silent null returns in catch blocks

---

## IPC Protocol

JSON command/response between JS and C# via Electron.NET contextBridge:

**JS → C#**: `window.electronAPI.send("message", JSON.stringify({ command: "some-command", data: {...} }))`

**C# → JS**: Responses broadcast via `MessageHandler.SendResponse` / `SendScreenResponse` with shape `{ Command: "some-command-response", Data: {...} }`

**Preload bridge** (`SSSOPanel/preload.js`):
```js
contextBridge.exposeInMainWorld('electronAPI', {
    send: (channel, data) => ipcRenderer.send(channel, data),
    on: (channel, callback) => ipcRenderer.on(channel, (event, ...args) => callback(...args)),
});
```

**Command naming**: lowercase-kebab-case for command names, lowercase-kebab-case `-response` suffix for responses.

---

## Key Files

| File | Purpose |
|------|---------|
| `SSSOPanel/Program.cs` | App entry — creates Panel + Screen windows via Electron |
| `SSSOPanel/preload.js` | Electron contextBridge: `electronAPI.send`/`electronAPI.on` |
| `SSSOPanel/electron/custom_main.js` | Electron main process — permission handlers (WebRTC, autoplay) |
| `SSSOPanel/electron.manifest.json` | Electron.NET configuration (app metadata, ports) |
| `SSSOPanel/MessageParser/ICommand.cs` | Command interface: `Task Execute(BrowserWindow? sender, object? data)` |
| `SSSOPanel/MessageParser/CommandFactory.cs` | String → command handler mapping |
| `SSSOPanel/MessageParser/CommandStateSet.cs` | Handles `state-set`, merges JSON state |
| `SSSOPanel/ScreenManager/ScreenManager.cs` | Singleton managing screen window lifecycle |
| `SSSOPanel/UserInterface/src/App.vue` | Root — bridges `window.electronAPI.on` → `mitt` |
| `SSSOPanel/UserInterface/src/main.js` | Bootstrap — registers Spin components globally, creates `mitt` emitter |
| `SSSOPanel/UserInterface/src/router.js` | Hash-based routes with `View[Panel/Screen][Name]` component pattern |
| `SSSOPanel/UserInterface/vite.config.js` | Vite config — Vue plugin, `@` alias, markdown plugin |

---

## Electron.NET Migration Plan

### Phase 1: C# Project Setup

**`SSSOPanel.csproj` changes:**
1. Remove `Photino.NET` (2.5.2) and `Photino.NET.Server` (1.0.0) packages
2. Add `ElectronNET.Core` package (v0.5.1 — includes `ElectronNET.Core.API` transitively)
3. Change `OutputType` from `WinExe` to `Exe`
4. Change SDK from `Microsoft.NET.Sdk` to `Microsoft.NET.Sdk.Web`
5. Remove Photino-specific properties (`GenerateEmbeddedFilesManifest`, `EmbedWwwRoot`, `UiRoot`, `WwwRoot`)
6. Remove Photino build targets (BuildUserInterface — Electron.NET handles this via its own pipeline)
7. Add `<ElectronMainJs>custom_main.js</ElectronMainJs>` and `<ElectronPreloadJs>preload.js</ElectronPreloadJs>`
8. Add `<Content Include="wwwroot/**" CopyToOutputDirectory="PreserveNewest" />`

### Phase 2: New Files

**`SSSOPanel/preload.js`:**
```js
const { contextBridge, ipcRenderer } = require('electron');
contextBridge.exposeInMainWorld('electronAPI', {
    send: (channel, data) => ipcRenderer.send(channel, data),
    on: (channel, callback) => ipcRenderer.on(channel, (event, ...args) => callback(...args)),
});
```

**`SSSOPanel/custom_main.js`** — Allow all permissions for WebRTC/media/autoplay:
```js
const { session } = require('electron');
app.whenReady().then(() => {
    session.defaultSession.setPermissionRequestHandler((webContents, permission, callback) => {
        callback(true);
    });
    session.defaultSession.setPermissionCheckHandler(() => true);
});
```

### Phase 3: C# Core Files

**`Program.cs`** — Full rewrite:
1. Change to `async Task Main` (Electron.NET requires async)
2. Replace `PhotinoWindow` creation with `Electron.WindowManager.CreateWindowAsync()`
3. Register IPC handler: `Electron.IpcMain.On("message", handler)`
4. Panel window loads `http://localhost:5173/#/panel` (dev) or `file://...wwwroot/index.html#/panel` (prod)
5. Enable DevTools in debug mode via `WebPreferences.DevTools = true`

**`ScreenManager.cs`** — Full rewrite:
1. Change `_screens` from `List<PhotinoWindow>` to `List<BrowserWindow>`
2. `CreateNewScreen()` → `Electron.WindowManager.CreateWindowAsync()`
3. Set `WebPreferences.AutoplayPolicy = NoUserGestureRequired`
4. Remove `SetMediaStreamEnabled`/`SetMediaAutoplayEnabled` (replaced by custom_main.js permissions)

**`MessageHandler.cs`** — Rewrite IPC:
1. Remove `RegisterWebMessageReceivedHandler` (Photino-specific)
2. Register with Electron: `Electron.IpcMain.On("message", handler)`
3. `SendResponse(BrowserWindow? sender, object result)` → `Electron.IpcMain.Send(sender, "message", resultJson)`
4. `SendScreenResponse(object result)` → iterate `_screens`, `Electron.IpcMain.Send()` each

**`ICommand.cs`** — Interface change:
- `using PhotinoNET; Task Execute(PhotinoWindow? sender, object? data)`
- → `using ElectronNET.API; Task Execute(BrowserWindow? sender, object? data)`

### Phase 4: C# Command Files

All 17 `Command*.cs` files updated:
1. `using PhotinoNET` → `using ElectronNET.API;` (`BrowserWindow` is in `ElectronNET.API`)
2. `PhotinoWindow? sender` → `BrowserWindow? sender` in `Execute()` signature

Notable command-specific changes:
- `CommandScreenFullscreenToggle.cs`: Use `Electron.IpcMain.Send(sender, "message", ...)` instead of direct window manipulation
- `CommandOpenScreen.cs`: Await `screenManager.CreateNewScreen()` (it becomes async)

### Phase 5: Vue Frontend

All files using `window.external.*` updated:
1. `App.vue`: `window.external.receiveMessage` → `window.electronAPI.on('message', ...)`
2. All other files: `window.external.sendMessage(JSON.stringify({...}))` → `window.electronAPI.send('message', JSON.stringify({...}))`

The 22 affected files are:
- `App.vue`
- All Panel views (11 files)
- All Screen views (8 files)
- `WhepPlayer.vue`
- `modules/useCurrentScreen.js`

### Phase 6: Build & CI

1. **`electron.manifest.json`**: Create with app name, singleInstance: true, aspCoreBackendPort: 0 (auto)
2. **GitHub Actions** (`.github/workflows/dotnet-desktop.yml`):
   - Remove `dotnet publish` step
   - Replace with `electronize build` (needs Windows runner for Windows builds)
   - Update artifact paths
3. **Dev script** (`dev.sh`): Replace `dotnet run` with `electronize start`

### Cross-Platform Notes

| Aspect | Detail |
|--------|--------|
| **Windows** | Electron bundles Chromium. No system dependencies. |
| **Linux** | Electron bundles Chromium. No WebKitGTK required. |
| **Android/iOS** | Not supported by Electron.NET |
| **Dev mode** | Load from Vite dev server URL (`localhost:5173`) |
| **Production** | Load from file:// URL to wwwroot (built via Electron pipeline) |
| **Binary size** | ~150–250MB (Chromium bundled per platform) |

### What Stays the Same

- All JSON message formats (`{ command, data }`)
- All `mitt` event bus usage in Vue
- All command logic (state management, settings, routing)
- `WhepPlayer.vue` (Chromium has full WebRTC support)
- Vue router, layouts, components
- Settings manager, update manager
- Single instance lock
