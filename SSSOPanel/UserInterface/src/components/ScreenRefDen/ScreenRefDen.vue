<template>
    <div
        class="container"
        :class="{ hidden: !refDenIsVisible }"
    >
        <h1 class="title">#referee-den</h1>
        <div
            v-for="(message, index) in messages"
            :key="index"
            class="message"
        >
            <div class="message-header">
                <span
                    class="author"
                    :style="{ color: message.color }"
                    >{{ message.author }}</span
                >
                <span class="role">{{ message.role }}</span>
                <span class="timestamp">{{ formatTimestamp(message.timestamp) }}</span>
            </div>
            <div class="content">{{ message.content }}</div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, inject } from 'vue';
import VoiceBotService from '../../modules/VoiceBotService';

// --- State and Services ---
const emitter = inject('emitter');
const refDenIsVisible = ref(false); // Now correctly declared for template access
const maxMessages = ref(5);
const localMessages = ref([]);

console.log('[SCREEN-REFDEN] Component initialized');

// --- Computed Properties ---
const messages = computed(() => {
    return localMessages.value.slice(0, maxMessages.value);
});

// --- Methods ---
const formatTimestamp = (timestamp) => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString();
};

const handleIncomingMessage = (message) => {
    // Only process messages with the correct command
    if (message.command === 'message-received') {
        // Add new message to the top of the list
        localMessages.value.unshift(message.data);
        // If the list is too long, remove the oldest message
        if (localMessages.value.length > maxMessages.value) {
            localMessages.value.pop();
        }
        window.external.sendMessage(
            JSON.stringify({
                command: 'state-set',
                data: {
                    refDenMessages: localMessages.value,
                },
            }),
        );
    }
};

// --- Lifecycle Hooks ---
onMounted(() => {
    // Listen for state updates to toggle visibility
    emitter.on('state-get-response', (state) => {
        refDenIsVisible.value = state?.refDenIsVisible ?? false;
        localMessages.value = state?.refDenMessages ?? [];
    });
    window.external.sendMessage(JSON.stringify({ command: 'state-get' }));
    // Set up the message handler from the VoiceBotService
    VoiceBotService.addMessageHandler(handleIncomingMessage);
});

onUnmounted(() => {
    // Clean up event listeners to prevent memory leaks
    emitter.off('state-get-response');
    VoiceBotService.removeMessageHandler(handleIncomingMessage);
});
</script>

<style scoped>
.container {
    width: 40vw;
    height: 9em;
    position: fixed;
    background: rgba(17, 17, 17, 0.9);
    border-radius: 0.3em;
    align-self: end;
    justify-self: center;
    z-index: 99;
    display: flex;
    flex-direction: column;
    box-sizing: border-box;
    padding: 0.3em 0.4em 0;
    overflow: hidden;
    opacity: 1;
    transition: opacity 0.5s ease;
}

.container.hidden {
    opacity: 0;
    pointer-events: none;
}

.title {
    font-size: 0.65em;
    color: rgba(255, 255, 255, 0.7);
    font-style: italic;
    font-weight: 500;
    width: 100%;
    padding-bottom: 0.2em;
    margin-bottom: 0.2em;
    border-bottom: solid 1px rgba(255, 255, 255, 0.2);
}

.message {
    margin-bottom: 0.3em;
    padding-bottom: 0.2em;
    border-bottom: 1px solid #333;
    animation: slideIn 0.3s ease-out;
}

.message-header {
    display: flex;
    align-items: center;
    gap: 0.4em;
    font-size: 0.65em;
    margin-bottom: 0.1em;
}

.author {
    font-weight: bold;
}

.role {
    background: #333;
    padding: 0.1em 0.2em;
    border-radius: 0.2em;
    font-size: 0.6em;
}

.timestamp {
    margin-left: auto;
    color: #666;
    font-size: 0.6em;
}

.content {
    font-size: 0.55em;
    word-wrap: break-word;
}

@keyframes slideIn {
    from {
        transform: translateY(10px);
        opacity: 0;
    }
    to {
        transform: translateY(0);
        opacity: 1;
    }
}
</style>
