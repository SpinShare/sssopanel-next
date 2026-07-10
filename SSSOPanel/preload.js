const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('electronAPI', {
    send: (channel, data) => {
        let parsed;
        try { parsed = JSON.parse(data); } catch { parsed = {}; }
        const hash = window.location.hash || '';
        parsed.__sender = hash.includes('/panel') ? 'panel' : 'screen';
        ipcRenderer.send(channel, JSON.stringify(parsed));
    },
    on: (channel, callback) => ipcRenderer.on(channel, (event, ...args) => callback(...args)),
});
