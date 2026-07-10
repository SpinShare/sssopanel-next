const { contextBridge, ipcRenderer } = require('electron');

// Retrieve this window's webContents id from the main process so the
// .NET backend can map incoming messages back to the correct BrowserWindow.
let windowId;
try {
    windowId = ipcRenderer.sendSync('get-window-id');
} catch {
    windowId = null;
}

contextBridge.exposeInMainWorld('electronAPI', {
    windowId: windowId,
    send: (channel, data) => {
        let parsed;
        try { parsed = JSON.parse(data); } catch { parsed = {}; }
        const hash = window.location.hash || '';
        parsed.__sender = hash.includes('/panel') ? 'panel' : 'screen';
        if (windowId != null) parsed.__windowId = windowId;
        ipcRenderer.send(channel, JSON.stringify(parsed));
    },
    on: (channel, callback) => ipcRenderer.on(channel, (event, ...args) => callback(...args)),
});