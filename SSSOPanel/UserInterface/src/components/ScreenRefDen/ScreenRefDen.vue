<template>
    <div
        class="container"
        :class="{ hidden: !showContainer }"
    >
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

<script>
import VoiceBotService from '../../modules/VoiceBotService';

export default {
    name: 'MessageNotification',
    data() {
        return {
            maxMessages: 5,
            localMessages: [],
            showContainer: false,
            hideTimeout: null,
            lastMessageTime: null,
        };
    },
    computed: {
        state() {
            return VoiceBotService.state;
        },
        isConnected() {
            return this.state.isConnected;
        },
        messages() {
            return this.localMessages.slice(0, this.maxMessages);
        },
    },
    methods: {
        formatTimestamp(timestamp) {
            const date = new Date(timestamp);
            return date.toLocaleTimeString();
        },
        handleIncomingMessage(message) {
            if (message.command === 'message-received') {
                this.localMessages.unshift(message.data);

                if (this.localMessages.length > this.maxMessages) {
                    this.localMessages.pop();
                }

                // Show container and reset hide timer
                this.showContainer = true;
                this.resetHideTimer();

                // Auto-remove message after timeout
                setTimeout(() => {
                    this.localMessages = this.localMessages.filter((msg) => msg.messageId !== message.data.messageId);
                    // If no more messages, start hide timer
                    if (this.localMessages.length === 0) {
                        this.resetHideTimer();
                    }
                }, 120000);
            }
        },
        resetHideTimer() {
            // Clear existing timeout
            if (this.hideTimeout) {
                clearTimeout(this.hideTimeout);
            }
            // Set new timeout to hide after 2 minutes
            this.hideTimeout = setTimeout(() => {
                this.showContainer = false;
            }, 120000); // 2 minutes = 120,000ms
        },
    },
    mounted() {
        VoiceBotService.addMessageHandler(this.handleIncomingMessage);
    },
    beforeUnmount() {
        VoiceBotService.removeMessageHandler(this.handleIncomingMessage);
        if (this.hideTimeout) {
            clearTimeout(this.hideTimeout);
        }
    },
};
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

.message {
    margin-bottom: 0.5em;
    padding-bottom: 0.3em;
    border-bottom: 1px solid #333;
    animation: slideIn 0.3s ease-out;
}

.message-header {
    display: flex;
    align-items: center;
    gap: 0.5em;
    font-size: 0.7em;
    margin-bottom: 0.2em;
}

.author {
    font-weight: bold;
}

.role {
    background: #333;
    padding: 0.1em 0.3em;
    border-radius: 0.2em;
    font-size: 0.6em;
}

.timestamp {
    margin-left: auto;
    color: #666;
    font-size: 0.6em;
}

.content {
    font-size: 0.6em;
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
