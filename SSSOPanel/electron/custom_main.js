const { app, session, ipcMain, BrowserWindow } = require('electron');

module.exports.onStartup = function (host) {
    app.whenReady().then(() => {
        session.defaultSession.setPermissionRequestHandler((webContents, permission, callback) => {
            callback(true);
        });
        session.defaultSession.setPermissionCheckHandler(() => true);

// Sync IPC: let the preload script retrieve this window's BrowserWindow id
        // so the .NET backend can map incoming messages back to the correct BrowserWindow.
        ipcMain.on('get-window-id', (event) => {
            const win = BrowserWindow.fromWebContents(event.sender);
            event.returnValue = win ? win.id : null;
        });
    });
};
