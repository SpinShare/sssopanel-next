import { reactive } from 'vue';

const state = reactive({
    isConnected: false,
    botUsername: '',
    voiceMembers: [],
    messageHandlers: new Set(),
    recentMessages: [],
    lastError: null,
});

let websocket = null;
let reconnectAttempts = 0;
const maxReconnectAttempts = 5;
const reconnectDelay = 3000;
const maxStoredMessages = 20;

function connect(serverUrl, channelId) {
    if (websocket && websocket.readyState === WebSocket.OPEN) return;

    try {
        websocket = new WebSocket(serverUrl);

        websocket.onopen = () => {
            reconnectAttempts = 0;
            state.lastError = null;
            websocket.send(
                JSON.stringify({
                    type: 'monitor_channel',
                    channel_id: channelId,
                }),
            );
        };

        websocket.onmessage = (event) => {
            if (!event.data || typeof event.data !== 'string') {
                console.error('Received invalid message format:', event.data);
                return;
            }

            try {
                const data = JSON.parse(event.data);
                const messageType = data.type || data.command; // Handle both 'type' and 'command'

                if (!data || typeof data !== 'object') {
                    console.error('Received malformed data:', data);
                    return;
                }

                if (!messageType) {
                    console.error('Received message missing type/command field:', data);
                    state.lastError = 'Missing message type/command';
                    return;
                }

                if (!data.id) {
                    data.id = `msg-${Date.now()}-${Math.random().toString(36).substr(2, 4)}`;
                }

                console.log('Received WebSocket message:', data);
                handleServerMessage(data, messageType, channelId);
            } catch (err) {
                console.error('Message processing error:', err, 'Raw data:', event.data);
                sendToWs({
                    type: 'error',
                    error: 'message_processing_failed',
                    details: err.message,
                    originalData: event.data,
                });
            }
        };

        websocket.onclose = () => {
            state.isConnected = false;
            state.botUsername = '';
            if (reconnectAttempts < maxReconnectAttempts) {
                reconnectAttempts++;
                setTimeout(() => connect(serverUrl, channelId), reconnectDelay);
            }
        };

        websocket.onerror = (err) => {
            console.error('WebSocket error:', err);
            state.lastError = `WebSocket error: ${err.message}`;
        };
    } catch (err) {
        console.error('Connection failed:', err);
        state.lastError = `Connection failed: ${err.message}`;
    }
}

function handleServerMessage(data, messageType, channelId) {
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

        case 'message-received': // Handle Discord messages
            handleChatMessage(data);
            break;

        case 'error':
            console.error('Server error:', data.message);
            state.lastError = data.message;
            break;

        default:
            console.warn('Received unknown message type:', messageType);
            state.lastError = `Unknown message type: ${messageType}`;
    }
}

function handleChatMessage(message) {
    // Validate message structure
    if (!message.data || typeof message.data !== 'object') {
        console.error('Invalid chat message format:', message);
        return;
    }

    // Add to recent messages
    state.recentMessages.unshift(message.data);

    // Trim messages if over limit
    if (state.recentMessages.length > maxStoredMessages) {
        state.recentMessages.pop();
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
    }
}

function disconnect() {
    if (websocket) {
        websocket.send(JSON.stringify({ type: 'stop_monitoring' }));
        websocket.close();
        websocket = null;
    }
    state.isConnected = false;
    state.botUsername = '';
    state.voiceMembers = [];
    state.recentMessages = [];
    reconnectAttempts = maxReconnectAttempts;
}

function updateMemberSpeaking(userId, isSpeaking) {
    const member = state.voiceMembers.find((m) => m.id === userId);
    if (member) {
        member.is_speaking = isSpeaking;
    }
}

export default {
    state,
    connect,
    disconnect,
    addMessageHandler,
    removeMessageHandler,
    sendToWs,
};
