import { inject, onMounted, onUnmounted, ref } from 'vue';
import VoiceBotService from '@/modules/VoiceBotService';
import { replyText } from '@/modules/refDenMarkup';

// Kept in the state, the screens show the newest few
const MAX_MESSAGES = 10;

// Hash, not the router, the router may not have resolved the first route yet
const isPanel = () => window.location.hash.startsWith('#/panel');

// Referee messages (the bot's spotlight role) waiting for the broadcaster to show them big or not, oldest first
export const spotlightRequests = ref([]);
// Some stay unanswered, the oldest go once there are more
const MAX_REQUESTS = 5;

const spotlightRequest = (data) => ({ messageId: data.messageId, author: data.author, color: data.color && data.color !== '#000000' ? data.color : null, text: (data.content ?? '').trim() ? replyText(data.content) : '(attachment)' });

export const dismissSpotlight = (messageId) => {
    spotlightRequests.value = spotlightRequests.value.filter((request) => request.messageId !== messageId);
};

// The message up big on stream right now (same shape as a request), null when none. Stays until it's hidden
export const activeSpotlight = ref(null);

const setSpotlightState = (spotlight) => {
    window.electronAPI.send('message', JSON.stringify({ command: 'state-set', data: { refDenSpotlight: spotlight } }));
};

// Replaces whatever is up
export const showSpotlight = (messageId) => {
    activeSpotlight.value = spotlightRequests.value.find((request) => request.messageId === messageId) ?? { messageId, author: '', color: null, text: '' };
    dismissSpotlight(messageId);
    setSpotlightState({ messageId });
};

export const hideSpotlight = () => {
    activeSpotlight.value = null;
    setSpotlightState(null);
};

const trackSpotlight = (event) => {
    const data = event.data ?? {};
    if (event.command === 'message-received' && data.spotlight) {
        spotlightRequests.value = [...spotlightRequests.value, spotlightRequest(data)].slice(-MAX_REQUESTS);
    } else if (event.command === 'message-updated') {
        const text = spotlightRequest(data).text;
        spotlightRequests.value = spotlightRequests.value.map((request) => (request.messageId === data.messageId ? { ...request, text } : request));
        if (activeSpotlight.value?.messageId === data.messageId) activeSpotlight.value = { ...activeSpotlight.value, text };
    } else if (event.command === 'message-deleted') {
        dismissSpotlight(data.messageId);
        // Screens can't show it anymore either
        if (activeSpotlight.value?.messageId === data.messageId) hideSpotlight();
    }
};

// Applies a ref channel event to the newest first list, null when it didn't change anything
const applyEvent = (messages, event) => {
    const data = event.data ?? {};
    switch (event.command) {
        case 'message-received':
            if (messages.some((message) => message.messageId === data.messageId)) return null;
            return [data, ...messages].slice(0, MAX_MESSAGES);
        case 'message-updated':
            if (!messages.some((message) => message.messageId === data.messageId)) return null;
            return messages.map((message) => (message.messageId === data.messageId ? { ...message, content: data.content, mentions: data.mentions ?? message.mentions } : message));
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
        trackSpotlight(event);
        messages = next;
        // Pushed out of the list by newer ones, the screens only know the listed messages
        if (activeSpotlight.value && !messages.some((message) => message.messageId === activeSpotlight.value.messageId)) hideSpotlight();
        push();
    };

    // Only seeded once, after that this list is the truth (a late state response could be older than it)
    const onAnyMessage = (type, data) => {
        if (messages !== null || (type !== 'state-get-response' && type !== 'state-set-response') || !isPanel()) return;

        messages = Array.isArray(data?.refDenMessages) ? data.refDenMessages : [];
        // A panel reload keeps the hide button for what's still up
        const shown = data?.refDenSpotlight?.messageId ? messages.find((message) => message.messageId === data.refDenSpotlight.messageId) : null;
        if (shown) activeSpotlight.value = spotlightRequest(shown);
        else if (data?.refDenSpotlight) hideSpotlight();
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
