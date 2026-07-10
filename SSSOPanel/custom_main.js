const { app, session } = require('electron');

app.whenReady().then(() => {
    session.defaultSession.setPermissionRequestHandler((webContents, permission, callback) => {
        callback(true);
    });

    session.defaultSession.setPermissionCheckHandler(() => true);
});
