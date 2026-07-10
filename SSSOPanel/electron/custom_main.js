const { app, session } = require('electron');

module.exports.onStartup = function (host) {
    app.whenReady().then(() => {
        session.defaultSession.setPermissionRequestHandler((webContents, permission, callback) => {
            callback(true);
        });
        session.defaultSession.setPermissionCheckHandler(() => true);
    });
};
