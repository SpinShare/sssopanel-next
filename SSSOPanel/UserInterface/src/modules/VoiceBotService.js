import { reactive } from 'vue';

const state = reactive({
    isConnected: false,
    botUsername: '',
    voiceMembers: [],
    messageHandlers: new Set(),
    recentMessages: [],
    lastError: null,
    url: '',
});

let websocket = null;
// Only the panel gets one, for the local bot
let commandKey = '';
let reconnectTimer = null;
const reconnectDelay = 5000;
const requestTimeout = 15000;
const maxStoredMessages = 20;

// request_id -> { resolve, reject, timer }
const pendingRequests = new Map();

function connect(serverUrl) {
    if (!serverUrl) return;

    // Already on this bot, the reconnect loop takes care of the rest
    if (state.url === serverUrl && (websocket || reconnectTimer)) return;
    if (state.url !== serverUrl) disconnect();

    state.url = serverUrl;
    open();
}

function open() {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;

    const url = state.url;
    let socket;
    try {
        socket = new WebSocket(url);
    } catch (err) {
        console.error('Connection failed:', err);
        state.lastError = `Connection failed: ${err.message}`;
        scheduleReconnect();
        return;
    }
    websocket = socket;

    socket.onopen = () => {
        state.lastError = null;
        // Older bots wait for this before they count as connected
        socket.send(JSON.stringify({ type: 'monitor_channel' }));
    };

    socket.onmessage = (event) => {
        if (!event.data || typeof event.data !== 'string') {
            console.error('Received invalid message format:', event.data);
            return;
        }

        try {
            const data = JSON.parse(event.data);
            if (!data || typeof data !== 'object') {
                console.error('Received malformed data:', data);
                return;
            }

            const messageType = data.type || data.command; // Handle both 'type' and 'command'
            if (!messageType) {
                console.error('Received message missing type/command field:', data);
                return;
            }

            handleServerMessage(data, messageType);
        } catch (err) {
            console.error('Message processing error:', err, 'Raw data:', event.data);
        }
    };

    socket.onclose = () => {
        // Replaced by a newer socket or disconnected on purpose
        if (websocket !== socket) return;

        websocket = null;
        state.isConnected = false;
        state.botUsername = '';
        state.voiceMembers = [];
        rejectPending('Voice bot disconnected');
        scheduleReconnect();
    };

    socket.onerror = () => {
        state.lastError = `Could not reach the voice bot at ${url}`;
    };
}

function scheduleReconnect() {
    if (!state.url || reconnectTimer) return;
    reconnectTimer = setTimeout(open, reconnectDelay);
}

function handleServerMessage(data, messageType) {
    if (messageType.endsWith('_result')) {
        const pending = pendingRequests.get(data.request_id);
        if (!pending) return;

        pendingRequests.delete(data.request_id);
        clearTimeout(pending.timer);
        if (data.success) {
            pending.resolve(data);
        } else {
            pending.reject(new Error(data.error || 'Request failed'));
        }
        return;
    }

    switch (messageType) {
        case 'bot_ready':
            state.botUsername = data.username;
            break;

        case 'channel_monitored':
            if (data.success) {
                state.isConnected = true;
            }
            break;

        case 'members_update':
            state.voiceMembers = data.members;
            break;

        case 'speaking_update':
            updateMemberSpeaking(data.user_id, data.is_speaking);
            break;

        case 'message-received':
        case 'message-updated':
        case 'message-deleted':
            handleChatMessage(data);
            break;

        case 'error':
            console.error('Server error:', data.message);
            state.lastError = data.message;
            break;

        default:
            console.warn('Received unknown message type:', messageType);
    }
}

function handleChatMessage(message) {
    if (!message.data || typeof message.data !== 'object') {
        console.error('Invalid chat message format:', message);
        return;
    }

    if (message.command === 'message-received') {
        state.recentMessages.unshift(message.data);
        if (state.recentMessages.length > maxStoredMessages) {
            state.recentMessages.pop();
        }
    }

    // Notify all subscribed components
    state.messageHandlers.forEach((handler) => handler(message));
}

function addMessageHandler(handler) {
    state.messageHandlers.add(handler);
}

function removeMessageHandler(handler) {
    state.messageHandlers.delete(handler);
}

function sendToWs(data) {
    if (websocket && websocket.readyState === WebSocket.OPEN) {
        websocket.send(JSON.stringify(data));
        return true;
    }
    return false;
}

/**
 * Sends a command to the bot and resolves with its `<type>_result` answer.
 */
function request(type, payload = {}) {
    return new Promise((resolve, reject) => {
        const requestId = `req-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
        if (!sendToWs({ type, key: commandKey, request_id: requestId, ...payload })) {
            reject(new Error('Voice bot is not connected'));
            return;
        }

        // Bots from before these commands never answer
        const timer = setTimeout(() => {
            pendingRequests.delete(requestId);
            reject(new Error('Voice bot did not answer, is it up to date?'));
        }, requestTimeout);
        pendingRequests.set(requestId, { resolve, reject, timer });
    });
}

function rejectPending(reason) {
    pendingRequests.forEach((pending) => {
        clearTimeout(pending.timer);
        pending.reject(new Error(reason));
    });
    pendingRequests.clear();
}

function disconnect() {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;

    const socket = websocket;
    websocket = null;
    if (socket) {
        if (socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify({ type: 'stop_monitoring' }));
        socket.close();
    }

    rejectPending('Voice bot disconnected');
    state.url = '';
    state.isConnected = false;
    state.botUsername = '';
    state.voiceMembers = [];
    state.recentMessages = [];
}

function updateMemberSpeaking(userId, isSpeaking) {
    const member = state.voiceMembers.find((m) => m.id === userId);
    if (member) {
        member.is_speaking = isSpeaking;
    }
}

function setKey(key) {
    commandKey = key || '';
}

export default {
    state,
    setKey,
    connect,
    disconnect,
    addMessageHandler,
    removeMessageHandler,
    sendToWs,
    request,
    status: () => request('status'),
    listChannels: () => request('list_channels'),
    join: (channelId) => request('join', { channel_id: channelId }),
    leave: (guildId) => request('leave', { guild_id: guildId }),
    setRefChannel: (guildId, channelId) => request('set_ref_channel', { guild_id: guildId, channel_id: channelId }),
    setSpotlightRole: (guildId, roleId) => request('set_spotlight_role', { guild_id: guildId, role_id: roleId }),
};
