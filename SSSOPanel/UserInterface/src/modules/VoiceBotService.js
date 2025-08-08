import { reactive } from 'vue';

const state = reactive({
  isConnected: false,
  botUsername: '',
  voiceMembers: [],
});

let websocket = null;
let reconnectAttempts = 0;
const maxReconnectAttempts = 5;
const reconnectDelay = 3000;

function connect(serverUrl, channelId) {
  if (websocket && websocket.readyState === WebSocket.OPEN) return;

  try {
    websocket = new WebSocket(serverUrl);

    websocket.onopen = () => {
      reconnectAttempts = 0;
      websocket.send(JSON.stringify({
        type: 'monitor_channel',
        channel_id: channelId,
      }));
    };

    websocket.onmessage = (event) => {
      // Basic data validation
      if (!event.data || typeof event.data !== 'string') {
        console.error('Received invalid message format:', event.data);
        return;
      }

      try {
        const data = JSON.parse(event.data);

        // Validate parsed data structure
        if (!data || typeof data !== 'object') {
          console.error('Received malformed data:', data);
          return;
        }

        // Ensure required fields exist (adjust according to your protocol)
        if (!data.type) {
          console.error('Received message missing type field:', data);
          return;
        }

        // Add a message ID for tracking if not present
        if (!data.id) {
          data.id = `msg-${Date.now()}-${Math.random().toString(36).substr(2, 4)}`;
        }

        console.log('Received WebSocket message:', data);
        handleServerMessage(data, channelId);

      } catch (err) {
        console.error('Message processing error:', err, 'Raw data:', event.data);
        // Consider sending an error acknowledgement back to the server
        sendToWs(JSON.stringify({
          type: 'error',
          error: 'message_processing_failed',
          details: err.message,
          originalData: event.data
        }));
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
    };
  } catch (err) {
    console.error('Connection failed:', err);
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
  reconnectAttempts = maxReconnectAttempts;
}

function handleServerMessage(data, channelId) {
  switch (data.type) {
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

    case 'error':
      console.error('Server error:', data.message);
      break;
  }
}

function updateMemberSpeaking(userId, isSpeaking) {
  const member = state.voiceMembers.find(m => m.id === userId);
  if (member) {
    member.is_speaking = isSpeaking;
  }
}

export default {
  state,
  connect,
  disconnect,
};