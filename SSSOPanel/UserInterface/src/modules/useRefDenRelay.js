import { inject, onMounted, onUnmounted } from 'vue';
import VoiceBotService from '@/modules/VoiceBotService';

// Kept in the state, the screens show the newest few
const MAX_MESSAGES = 10;

// Hash, not the router, the router may not have resolved the first route yet
const isPanel = () => window.location.hash.startsWith('#/panel');

// Applies a ref channel event to the newest first list, null when it didn't change anything
const applyEvent = (messages, event) => {
    const data = event.data ?? {};
    switch (event.command) {
        case 'message-received':
            if (messages.some((message) => message.messageId === data.messageId)) return null;
            return [data, ...messages].slice(0, MAX_MESSAGES);
        case 'message-updated':
            if (!messages.some((message) => message.messageId === data.messageId)) return null;
            return messages.map((message) => (message.messageId === data.messageId ? { ...message, content: data.content } : message));
        case 'message-deleted': {
            const next = messages.filter((message) => message.messageId !== data.messageId);
            return next.length === messages.length ? null : next;
        }
        default:
            return null;
    }
};

// Used once in App.vue. The panel is the only one writing ref den messages to the state, so several
// screens (OBS sources) never race each other and nothing is lost while no screen is open
export default function useRefDenRelay() {
    const emitter = inject('emitter');

    // Null until the saved state is in, so a panel reload keeps the earlier messages
    let messages = null;
    let queued = [];

    const push = () => {
        window.electronAPI.send(
            'message',
            JSON.stringify({
                command: 'state-set',
                data: { refDenMessages: messages },
            }),
        );
    };

    const onEvent = (event) => {
        if (!isPanel()) return;
        if (messages === null) {
            queued.push(event);
            return;
        }

        const next = applyEvent(messages, event);
        if (!next) return;
        messages = next;
        push();
    };

    // Only seeded once, after that this list is the truth (a late state response could be older than it)
    const onAnyMessage = (type, data) => {
        if (messages !== null || (type !== 'state-get-response' && type !== 'state-set-response') || !isPanel()) return;

        messages = Array.isArray(data?.refDenMessages) ? data.refDenMessages : [];
        const pending = queued;
        queued = [];
        pending.forEach(onEvent);
    };

    onMounted(() => {
        emitter.on('*', onAnyMessage);
        VoiceBotService.addMessageHandler(onEvent);
        if (isPanel()) window.electronAPI.send('message', JSON.stringify({ command: 'state-get' }));
    });

    onUnmounted(() => {
        emitter.off('*', onAnyMessage);
        VoiceBotService.removeMessageHandler(onEvent);
    });
}
