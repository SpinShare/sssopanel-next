<template>
    <!-- float (match-ingame): no box, new messages pop up over the gameplay for a while -->
    <div
        v-if="variant === 'float'"
        class="screen-ref-den float"
    >
        <TransitionGroup
            tag="div"
            name="toast"
            class="toasts"
        >
            <section
                v-for="message in toastMessages"
                :key="message.messageId"
                class="card"
            >
                <header>
                    <div class="tint themed"></div>
                    <div class="noise"></div>
                    <span class="mdi mdi-forum"></span>
                    <span>REF DEN</span>
                </header>
                <div class="box">
                    <ScreenRefDenMessage :message="message" />
                </div>
            </section>
        </TransitionGroup>
    </div>

    <template v-else>
        <transition name="ref-den">
            <section
                v-if="refDenIsVisible"
                class="screen-ref-den"
                :class="[variant, { short }]"
            >
                <header>
                    <div class="tint themed"></div>
                    <div class="noise"></div>
                    <span class="mdi mdi-forum"></span>
                    <span>REF DEN</span>
                </header>
                <div class="box">
                    <!-- Newest at the bottom like a chat window, older ones run off the top.
                         The viewport's height follows the messages (animated), between the old fixed size and a limit -->
                    <div
                        ref="viewport"
                        class="viewport"
                        :class="{ animated: heightAnimated }"
                        :style="viewportHeight !== null ? { height: `${viewportHeight}px` } : null"
                    >
                        <TransitionGroup
                            tag="div"
                            name="message"
                            class="messages"
                        >
                            <ScreenRefDenMessage
                                v-for="message in shownMessages"
                                :key="message.messageId"
                                :message="message"
                                :data-id="message.messageId"
                            />
                            <div
                                v-if="!shownMessages.length"
                                key="empty"
                                class="empty"
                            >
                                There's nothing here yet!
                            </div>
                        </TransitionGroup>
                    </div>
                </div>
            </section>
        </transition>
    </template>

    <!-- Big in the middle, only when the panel puts a referee's message up (refDenSpotlight) -->
    <transition
        name="spotlight"
        mode="out-in"
    >
        <section
            v-if="spotlightMessage"
            :key="spotlightMessage.messageId"
            class="screen-ref-den spotlight"
        >
            <header>
                <div class="tint themed"></div>
                <div class="noise"></div>
                <span class="mdi mdi-forum"></span>
                <span>REF DEN</span>
            </header>
            <div class="box">
                <ScreenRefDenMessage :message="spotlightMessage" />
            </div>
        </section>
    </transition>
</template>

<script setup>
import { computed, inject, onMounted, onUnmounted, ref, watch } from 'vue';
import ScreenRefDenMessage from './ScreenRefDenMessage.vue';
import { highlightVersion } from '@/modules/useHighlighter';
import { isJumbo, replyText, toLines } from '@/modules/refDenMarkup';

const props = defineProps({
    // docked: bottom right above the footer. column: full height on the right, the screen makes room (match-overview).
    // float: over the gameplay, only while messages are new (match-ingame)
    variant: {
        type: String,
        default: 'docked',
    },
    // Docked but lower, for screens that fill up the whole area (brackets, match results)
    short: Boolean,
});

// How long a floating message stays on match-ingame, longer messages need longer to read
const readTime = (message) => Math.min(20000, Math.max(6000, 3000 + (message.content ?? '').length * 60));
// Float shows this many at once, the oldest goes first
const MAX_TOASTS = 3;

const emitter = inject('emitter');

const refDenIsVisible = ref(false);
// Newest first, written by the panel (useRefDenRelay)
const messages = ref([]);

// Oldest at the top like a chat, the newest one at the bottom. All of them, the box cuts off what doesn't fit
const shownMessages = computed(() => {
    // Re-runs once a code block's grammar is in
    void highlightVersion.value;
    return [...messages.value].reverse().map((message) => ({
        ...message,
        // Images and stickers come without text
        lines: (message.content ?? '').trim() ? toLines(message.content.trim(), message.mentions) : [{ kind: '', parts: [{ text: '(attachment)' }] }],
        reply: message.reply ? { ...message.reply, text: replyText(message.reply.content ?? ''), color: message.reply.color && message.reply.color !== '#000000' ? message.reply.color : null } : null,
        jumbo: isJumbo(message.content ?? ''),
        // No colored role is #000000, which would vanish on the dark box
        color: message.color && message.color !== '#000000' ? message.color : null,
        role: message.role === '@everyone' ? '' : message.role,
    }));
});

const byId = (id) => shownMessages.value.find((message) => message.messageId === id) ?? null;

// Only messages that come in while this screen is open pop up, not the ones it loads with
let seen = null;
const timers = new Set();
const later = (callback, delay) => {
    const timer = setTimeout(() => {
        timers.delete(timer);
        callback();
    }, delay);
    timers.add(timer);
};

const toastIds = ref([]);
// Deleted ones drop out on their own, edits show up
const toastMessages = computed(() => toastIds.value.map(byId).filter(Boolean));
const showToast = (message) => {
    toastIds.value = [...toastIds.value, message.messageId].slice(-MAX_TOASTS);
    later(() => {
        toastIds.value = toastIds.value.filter((id) => id !== message.messageId);
    }, readTime(message));
};

// Up until the panel hides it (refDenSpotlight), a new one replaces it
const spotlightId = ref(null);
const spotlightMessage = computed(() => (spotlightId.value ? byId(spotlightId.value) : null));

// Auto height can't transition, so the viewport gets the messages' height in px, clamped to its min/max-height.
// The column fills its space instead and skips this
const viewport = ref(null);
const viewportHeight = ref(null);
// Off for the first measurement, it would grow from nothing
const heightAnimated = ref(false);
let heightObserver = null;
const measure = () => {
    const box = viewport.value;
    const list = box?.firstElementChild;
    if (!box || !list) return;
    const style = getComputedStyle(box);
    const min = parseFloat(style.minHeight) || 0;
    const max = parseFloat(style.maxHeight) || Infinity;
    viewportHeight.value = Math.min(max, Math.max(min, list.offsetHeight));
};
watch(
    viewport,
    (box) => {
        heightObserver?.disconnect();
        heightObserver = null;
        heightAnimated.value = false;
        viewportHeight.value = null;
        if (!box || props.variant === 'column') return;

        // The list changes size with new messages and their transitions, the viewport with the screen size (vw)
        heightObserver = new ResizeObserver(measure);
        heightObserver.observe(box.firstElementChild);
        heightObserver.observe(document.documentElement);
        measure();
        requestAnimationFrame(() => requestAnimationFrame(() => (heightAnimated.value = true)));
    },
    { flush: 'post' },
);

const applyState = (state) => {
    refDenIsVisible.value = state?.refDenIsVisible ?? false;
    messages.value = Array.isArray(state?.refDenMessages) ? state.refDenMessages : [];
    spotlightId.value = state?.refDenSpotlight?.messageId ?? null;

    const ids = messages.value.map((message) => message.messageId);
    if (seen === null) {
        seen = new Set(ids);
        return;
    }
    // Oldest first, so they pop up in the order they were sent
    const fresh = ids.filter((id) => !seen.has(id)).reverse();
    fresh.forEach((id) => seen.add(id));
    if (!refDenIsVisible.value) {
        toastIds.value = [];
        return;
    }
    if (props.variant === 'float') fresh.map(byId).filter(Boolean).forEach(showToast);
};

onMounted(() => {
    emitter.on('state-get-response', applyState);
    // state-set broadcasts the merged state to every screen, so a toggle doesn't need a state-get round trip
    emitter.on('state-set-response', applyState);
    window.electronAPI.send('message', JSON.stringify({ command: 'state-get' }));
});

onUnmounted(() => {
    emitter.off('state-get-response', applyState);
    emitter.off('state-set-response', applyState);
    timers.forEach(clearTimeout);
    heightObserver?.disconnect();
});
</script>

<style lang="scss" scoped>
// Same pieces as the match cards: a tinted label across the top of a dark box
.screen-ref-den {
    display: flex;
    flex-direction: column;
    color: #ededed;
    width: 26vw;
    // How many lines a message gets before it's cut off (and goes to the spotlight)
    --clamp: 6;

    &.docked {
        position: fixed;
        right: 2vw;
        // Above the 3vw footer
        bottom: 4vw;
        z-index: 99;
    }

    // match-overview moves its cards over for it, always the full height
    &.column {
        position: absolute;
        top: 5vw;
        bottom: 5vw;
        right: 3vw;
        width: 25vw;
        z-index: 20;
        --clamp: 10;

        & .box {
            flex-grow: 1;
            min-height: 0;
            display: flex;
            flex-direction: column;
        }

        & .viewport {
            flex-grow: 1;
            min-height: 0;
            max-height: none;
        }
    }

    &.short {
        --clamp: 3;
    }

    & header {
        color: #111;
        padding: 0.3vw 1vw;
        border-radius: 0.5vw 0.5vw 0 0;
        font-size: 0.6vw;
        font-weight: 700;
        letter-spacing: 0.12vw;
        position: relative;
        overflow: hidden;
        display: flex;
        align-items: center;
        gap: 0.4vw;

        & span {
            position: relative;
            z-index: 10;
        }

        & .mdi {
            font-size: 0.75vw;
            letter-spacing: 0;
        }

        & .tint {
            background-image: var(--themeBackground);
            background-size: cover;
            position: absolute;
            inset: 0;
        }

        & .noise {
            background: url('../../assets/noise.png');
            background-size: 5em;
            position: absolute;
            inset: 0;
            z-index: 1;
            opacity: 0.4;
            mix-blend-mode: soft-light;
        }
    }

    & .box {
        background: #111;
        border-radius: 0 0 0.5vw 0.5vw;
        padding: 0.7vw 1vw;
    }

    // Grows with the messages up to a limit, the newest sits at the bottom and older ones fade out at the top
    // At least the old fixed size, grows with new messages up to the limit, anything above runs off the top
    & .viewport {
        min-height: 9vw;
        max-height: 16vw;
        display: flex;
        flex-direction: column;
        justify-content: flex-end;
        overflow: hidden;
        -webkit-mask-image: linear-gradient(to bottom, transparent, #000 1.5vw);
        mask-image: linear-gradient(to bottom, transparent, #000 1.5vw);

        // Same timing as a message sliding in, so the box and the message move together
        &.animated {
            transition: height 0.4s ease;
        }
    }

    & .messages {
        flex-shrink: 0;
        display: flex;
        flex-direction: column;
        gap: 0.4vw;
    }

    &.short .viewport {
        max-height: 9vw;
    }

    & .empty {
        color: rgba(255, 255, 255, 0.5);
        font-size: 0.8vw;
        font-style: italic;
    }

    // Big in the middle, reads the whole thing out
    &.spotlight {
        position: fixed;
        left: 50%;
        top: 50%;
        transform: translate(-50%, -50%);
        width: 56vw;
        z-index: 120;
        box-shadow: 0 1vw 3vw rgba(0, 0, 0, 0.6);
        border-radius: 0.5vw;
        --message-size: 1.5vw;
        --clamp: 14;

        & header {
            font-size: 0.8vw;
            padding: 0.5vw 1.2vw;
        }

        & .box {
            padding: 1.2vw 1.5vw;
        }
    }

    // Over the top right of the gameplay cutout (28.125vw tall, centered), the right side like everywhere else
    &.float {
        position: fixed;
        right: 2vw;
        top: calc(50% - 14.0625vw + 1vw);
        width: 24vw;
        z-index: 99;
        --clamp: 8;

        & .toasts {
            display: flex;
            flex-direction: column;
            align-items: flex-end;
            gap: 0.6vw;
        }

        & .card {
            display: flex;
            flex-direction: column;
            width: 100%;
        }

        & .box {
            // See through enough to not hide the game completely
            background: rgba(17, 17, 17, 0.85);
        }
    }
}

// New messages slide in at the bottom and push the older ones up
.message-enter-active {
    transition: opacity 0.4s ease, transform 0.4s ease;
}
.message-leave-active {
    transition: opacity 0.3s ease;
}
.message-enter-from {
    opacity: 0;
    transform: translateY(0.8vw);
}
.message-leave-to {
    opacity: 0;
}
.message-move {
    transition: transform 0.4s ease;
}

.ref-den-enter-active,
.ref-den-leave-active {
    transition: opacity 0.5s ease;
}
.ref-den-enter-from,
.ref-den-leave-to {
    opacity: 0;
}
// The overview's column comes in once the cards have mostly moved over, and leaves before they grow back
.column.ref-den-enter-active {
    transition: opacity 0.4s ease-out 0.25s, transform 0.4s ease-out 0.25s;
}
.column.ref-den-leave-active {
    transition: opacity 0.25s ease-in, transform 0.25s ease-in;
}
.column.ref-den-enter-from,
.column.ref-den-leave-to {
    transform: translateX(4vw);
}

// Out of the flow while leaving, so the ones below move up smoothly
.toast-leave-active {
    position: absolute;
    width: 100%;
}

// Pops up, then shrinks off towards where the box is
.spotlight-enter-active {
    transition: opacity 0.4s ease, transform 0.4s cubic-bezier(0.2, 1.2, 0.4, 1);
}
.spotlight-leave-active {
    transition: opacity 0.4s ease, transform 0.4s ease-in;
}
.spotlight-enter-from {
    opacity: 0;
    transform: translate(-50%, -50%) scale(0.9);
}
.spotlight-leave-to {
    opacity: 0;
    transform: translate(-50%, -50%) translate(15vw, 10vw) scale(0.4);
}

.toast-enter-active,
.toast-leave-active {
    transition: opacity 0.4s ease, transform 0.4s ease;
}
.toast-enter-from,
.toast-leave-to {
    opacity: 0;
    transform: translateX(1.5vw);
}
.toast-move {
    transition: transform 0.4s ease;
}
</style>
