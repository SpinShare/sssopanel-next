// Overlays running outside of Electron (e.g. an OBS browser source) have no preload script.
// For those, window.electronAPI is backed by a WebSocket to the panel's overlay server,
// which speaks the same messages as Electron IPC.

const OVERLAY_PORT = 7274;
const RECONNECT_DELAY = 2000;

export const overlayUrl = import.meta.env.DEV ? `${window.location.origin}/#/screen` : `http://localhost:${OVERLAY_PORT}/#/screen`;

export function installTransport() {
    if (window.electronAPI) return;

    const listeners = [];
    let queue = [];
    let socket = null;
    let hasFailed = false;

    const connect = () => {
        socket = new WebSocket(`ws://${window.location.hostname || 'localhost'}:${OVERLAY_PORT}/ws`);

        socket.onopen = () => {
            // The panel was restarted or started late, reload to resync with its state
            if (hasFailed) {
                window.location.reload();
                return;
            }

            queue.forEach((data) => socket.send(data));
            queue = [];
        };

        socket.onmessage = (event) => {
            listeners.forEach((callback) => callback(event.data));
        };

        socket.onclose = () => {
            hasFailed = true;
            queue = [];
            setTimeout(connect, RECONNECT_DELAY);
        };
    };

    window.electronAPI = {
        windowId: null,
        send: (channel, data) => {
            if (socket.readyState === WebSocket.OPEN) {
                socket.send(data);
            } else if (!hasFailed) {
                queue.push(data);
            }
        },
        on: (channel, callback) => listeners.push(callback),
    };

    connect();
}
