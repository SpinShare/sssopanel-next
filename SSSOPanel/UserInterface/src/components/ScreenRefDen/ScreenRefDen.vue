<template>
    <transition name="ref-den">
        <section
            v-if="refDenIsVisible"
            class="screen-ref-den"
            :class="[variant, { compact }]"
        >
            <header>
                <div class="tint themed"></div>
                <div class="noise"></div>
                <span class="mdi mdi-forum"></span>
                <span>REF DEN</span>
            </header>
            <div class="box">
                <!-- Fixed height like a chat window, pinned to the newest message, older ones run off the top -->
                <TransitionGroup
                    tag="div"
                    name="message"
                    class="messages"
                >
                    <div
                        v-for="message in shownMessages"
                        :key="message.messageId"
                        class="message"
                    >
                        <div class="meta">
                            <span
                                class="author"
                                :style="message.color ? { color: message.color } : null"
                                >{{ message.author }}</span
                            >
                            <span
                                v-if="message.role"
                                class="role"
                                >{{ message.role }}</span
                            >
                        </div>
                        <!-- Emoji only messages show big, like in discord -->
                        <div
                            class="content"
                            :class="{ jumbo: message.jumbo }"
                        >
                            <template
                                v-for="(part, index) in message.parts"
                                :key="index"
                            >
                                <img
                                    v-if="part.emoji && !failedEmoji.has(part.emoji)"
                                    class="emoji"
                                    :src="part.emoji"
                                    alt=""
                                    @error="emojiFailed(part.emoji)"
                                />
                                <template v-else-if="part.emoji">:{{ part.text }}:</template>
                                <!-- Spoilers stay hidden on stream too -->
                                <span
                                    v-else-if="part.spoiler"
                                    class="spoiler"
                                    >{{ part.text }}</span
                                >
                                <template v-else>{{ part.text }}</template>
                            </template>
                        </div>
                    </div>
                    <div
                        v-if="!shownMessages.length"
                        key="empty"
                        class="empty"
                    >
                        There's nothing here yet!
                    </div>
                </TransitionGroup>
            </div>
        </section>
    </transition>
</template>

<script setup>
import { computed, inject, onMounted, onUnmounted, ref } from 'vue';

defineProps({
    // docked: floats above the footer on screens with room. inline: sits in a layout slot (match-ingame's banner)
    variant: {
        type: String,
        default: 'docked',
    },
    // Same height as inline, for screens with less room
    compact: Boolean,
});

const emitter = inject('emitter');

const refDenIsVisible = ref(false);
// Newest first, written by the panel (useRefDenRelay)
const messages = ref([]);

// The rest of discord's markup only makes sense inside discord
const cleanText = (text) =>
    text
        .replace(/\[([^\]]+)\]\(<?https?:\/\/[^\s)>]+>?\)/g, '$1')
        .replace(/\*\*|__|~~|`/g, '')
        .replace(/<@!?\d+>/g, '@user')
        .replace(/<@&\d+>/g, '@role')
        .replace(/<#\d+>/g, '#channel')
        .replace(/<t:(\d+)(:\w)?>/g, (_, seconds) => new Date(seconds * 1000).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }));

// Custom emoji (<:name:id>, <a:name:id> when animated) come from discord's cdn, which serves any server's emoji by id
// Without nitro, an emoji from another server is sent as its cdn link instead, sometimes as a [name](<link>) masked link
const CUSTOM_EMOJI = /<(a?):(\w+):(\d+)>|(?:\[([^\]]*)\]\(<?)?https:\/\/(?:cdn|media)\.discordapp\.(?:com|net)\/emojis\/(\d+)\.(\w+)(\?[^\s)>]*)?(?:>?\))?/g;
const emojiPart = (match) => {
    if (match[3]) return { text: match[2], emoji: `https://cdn.discordapp.com/emojis/${match[3]}.${match[1] ? 'gif' : 'webp'}?size=96` };

    const query = new URLSearchParams(match[7] ?? '');
    const animated = match[6] === 'gif' || query.get('animated') === 'true';
    const name = query.get('name') || (match[4] ?? '').replace(/:/g, '') || 'emoji';
    return { text: name, emoji: `https://cdn.discordapp.com/emojis/${match[5]}.${animated ? 'gif' : 'webp'}?size=96` };
};
// Odd pieces of a split on ||...|| are the spoilers
const textParts = (text) => text.split(/\|\|([\s\S]+?)\|\|/).map((piece, index) => ({ text: cleanText(piece), spoiler: index % 2 === 1 }));
const toParts = (content) => {
    const parts = [];
    let last = 0;
    for (const match of content.matchAll(CUSTOM_EMOJI)) {
        if (match.index > last) parts.push(...textParts(content.slice(last, match.index)));
        parts.push(emojiPart(match));
        last = match.index + match[0].length;
    }
    if (last < content.length) parts.push(...textParts(content.slice(last)));
    return parts.filter((part) => part.emoji || part.text !== '');
};

// Unicode emoji too, so a message of only emoji (custom or not) gets the big ones
const EMOJI_ONLY = /^(?:\p{Extended_Pictographic}|\p{Emoji_Component}|\u200d|\ufe0f|\s)+$/u;

// Deleted emoji or no internet, the name is better than a broken image
const failedEmoji = ref(new Set());
const emojiFailed = (url) => {
    failedEmoji.value = new Set([...failedEmoji.value, url]);
};

const isJumbo = (content) => {
    const rest = content.replace(CUSTOM_EMOJI, '').trim();
    const hasEmoji = rest !== content.trim() || /\p{Extended_Pictographic}/u.test(rest);
    return hasEmoji && (rest === '' || EMOJI_ONLY.test(rest));
};

// Oldest at the top like a chat, the newest one at the bottom. All of them, the box cuts off what doesn't fit
const shownMessages = computed(() =>
    [...messages.value].reverse().map((message) => ({
        ...message,
        // Images and stickers come without text
        parts: (message.content ?? '').trim() ? toParts(message.content.trim()) : [{ text: '(attachment)' }],
        jumbo: isJumbo(message.content ?? ''),
        // No colored role is #000000, which would vanish on the dark box
        color: message.color && message.color !== '#000000' ? message.color : null,
        role: message.role === '@everyone' ? '' : message.role,
    })),
);

const applyState = (state) => {
    refDenIsVisible.value = state?.refDenIsVisible ?? false;
    messages.value = Array.isArray(state?.refDenMessages) ? state.refDenMessages : [];
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
});
</script>

<style lang="scss" scoped>
// Same pieces as the match cards: a tinted label across the top of a dark box
.screen-ref-den {
    display: flex;
    flex-direction: column;
    color: #ededed;
    width: 26vw;

    &.docked {
        position: fixed;
        right: 2vw;
        // Above the 3vw footer
        bottom: 4vw;
        z-index: 99;
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

    // Always the same size, the newest message sits at the bottom and older ones fade out at the top
    & .messages {
        height: 9vw;
        display: flex;
        flex-direction: column;
        justify-content: flex-end;
        gap: 0.4vw;
        overflow: hidden;
        -webkit-mask-image: linear-gradient(to bottom, transparent, #000 1.5vw);
        mask-image: linear-gradient(to bottom, transparent, #000 1.5vw);
    }

    // The band below the gameplay only has room for a couple of lines
    &.inline .messages,
    &.compact .messages {
        height: 6.5vw;
    }

    // Name and role on top, the message below, like discord
    & .message {
        flex-shrink: 0;
        display: flex;
        flex-direction: column;
        gap: 0.15vw;
        font-size: 0.8vw;
        line-height: 1.35;
        // Discord's unicode emoji in color, wherever the system has no emoji font. Before the JP font, it has some symbols (like hearts) in black and white
        font-family: 'Orbitron', 'Noto Color Emoji', 'Noto Sans JP', sans-serif;

        & .meta {
            display: flex;
            align-items: center;
            gap: 0.5vw;
            min-width: 0;
        }

        & .author {
            font-weight: 700;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }

        & .role {
            flex-shrink: 0;
            font-size: 0.55vw;
            font-weight: 600;
            letter-spacing: 0.05vw;
            background: rgba(255, 255, 255, 0.1);
            padding: 0.1vw 0.35vw;
            border-radius: 0.2vw;
        }

        // A wall of text still can't push everything else out
        & .content {
            color: rgba(255, 255, 255, 0.85);
            word-break: break-word;
            display: -webkit-box;
            -webkit-box-orient: vertical;
            -webkit-line-clamp: 3;
            overflow: hidden;

            & .emoji {
                height: 1.35em;
                width: auto;
                vertical-align: bottom;
            }

            & .spoiler {
                background: rgba(255, 255, 255, 0.15);
                color: transparent;
                border-radius: 0.2vw;
                padding: 0 0.2vw;
            }

            &.jumbo {
                font-size: 1.8vw;
                line-height: 1.2;
            }
        }
    }

    & .empty {
        color: rgba(255, 255, 255, 0.5);
        font-size: 0.8vw;
        font-style: italic;
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
</style>
