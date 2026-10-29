<template>
    <div class="message">
        <!-- Replies show who they answer above the name, like discord -->
        <div
            v-if="message.reply"
            class="reply"
        >
            <span class="mdi mdi-reply"></span>
            <template v-if="message.reply.author">
                <span
                    class="reply-author"
                    :style="message.reply.color ? { color: message.reply.color } : null"
                    >{{ message.reply.author }}</span
                >
                <span class="reply-text">{{ message.reply.text }}</span>
            </template>
            <span
                v-else
                class="reply-text"
                >Original message was deleted</span
            >
        </div>
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
                v-for="(line, lineIndex) in message.lines"
                :key="lineIndex"
            >
                <!-- Code block and quote lines are blocks, they break on their own -->
                <br v-if="lineIndex && !line.block && !message.lines[lineIndex - 1].block" />
                <span
                    class="line"
                    :class="line.kind"
                >
                    <template
                        v-for="(part, index) in line.parts"
                        :key="index"
                    >
                        <img
                            v-if="part.emoji && !failedEmoji.has(part.emoji)"
                            class="emoji"
                            :class="part.classes"
                            :src="part.emoji"
                            alt=""
                            @error="emojiFailed(part.emoji)"
                        />
                        <span
                            v-else-if="part.emoji"
                            :class="part.classes"
                            >:{{ part.text }}:</span
                        >
                        <span
                            v-else-if="part.mention"
                            class="mention"
                            :class="part.classes"
                            :style="part.color ? { color: part.color, background: `${part.color}33` } : null"
                            >{{ part.text }}</span
                        >
                        <!-- Spoilers stay hidden on stream too -->
                        <span
                            v-else
                            :class="part.classes"
                            :style="part.style"
                            >{{ part.text }}</span
                        >
                    </template>
                </span>
            </template>
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue';

// One ref den message, already shaped by ScreenRefDen (lines, reply, colors)
defineProps({
    message: {
        type: Object,
        required: true,
    },
});

// Deleted emoji or no internet, the name is better than a broken image
const failedEmoji = ref(new Set());
const emojiFailed = (url) => {
    failedEmoji.value = new Set([...failedEmoji.value, url]);
};
</script>

<style lang="scss" scoped>
// Name and role on top, the message below, like discord
.message {
    flex-shrink: 0;
    display: flex;
    flex-direction: column;
    gap: 0.15vw;
    // Set per layout by the ref den, the spotlight goes bigger. Everything inside scales with it
    font-size: var(--message-size, 0.8vw);
    line-height: 1.35;
    // Discord's unicode emoji in color, wherever the system has no emoji font. Before the JP font, it has some symbols (like hearts) in black and white
    font-family: 'Orbitron', 'Noto Color Emoji', 'Noto Sans JP', sans-serif;

    // One line, the rest of the replied message gets cut off
    & .reply {
        display: flex;
        align-items: center;
        gap: 0.3vw;
        min-width: 0;
        font-size: 0.75em;
        color: rgba(255, 255, 255, 0.55);
        white-space: nowrap;

        & .mdi {
            flex-shrink: 0;
            font-size: 1.25em;
        }

        & .reply-author {
            flex-shrink: 0;
            font-weight: 700;
            color: rgba(255, 255, 255, 0.75);
        }

        & .reply-text {
            overflow: hidden;
            text-overflow: ellipsis;
        }
    }

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
        font-size: 0.7em;
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
        -webkit-line-clamp: var(--clamp, 3);
        overflow: hidden;

        & .emoji {
            height: 1.35em;
            width: auto;
            vertical-align: bottom;
        }

        & .bold {
            font-weight: 700;
        }
        & .italic {
            font-style: italic;
        }
        & .underline {
            text-decoration: underline;
        }
        & .strike {
            text-decoration: line-through;
        }
        & .underline.strike {
            text-decoration: underline line-through;
        }
        & .code {
            font-family: monospace;
            background: rgba(255, 255, 255, 0.1);
            border-radius: 0.2vw;
            padding: 0 0.2vw;
        }

        & .mention {
            color: #c9cdfb;
            background: rgba(88, 101, 242, 0.3);
            border-radius: 0.2vw;
            padding: 0 0.15vw;
            font-weight: 600;
        }

        & .spoiler {
            background: rgba(255, 255, 255, 0.15);
            color: transparent;
            border-radius: 0.2vw;
            padding: 0 0.2vw;
        }
        & img.spoiler {
            filter: brightness(0) invert(1) opacity(0.15);
        }

        & .line {
            &.heading {
                font-weight: 700;
            }
            &.h1 {
                font-size: 1.3em;
            }
            &.h2 {
                font-size: 1.15em;
            }
            &.subtext {
                font-size: 0.8em;
                color: rgba(255, 255, 255, 0.55);
            }
            // Stacked blocks, so the background runs on without gaps between the lines
            &.codeblock {
                display: block;
                font-family: monospace;
                font-size: 0.9em;
                white-space: pre-wrap;
                background: rgba(255, 255, 255, 0.08);
                padding: 0 0.5vw;

                &.first {
                    border-radius: 0.3vw 0.3vw 0 0;
                    padding-top: 0.25vw;
                    margin-top: 0.15vw;
                }
                &.last {
                    border-radius: 0 0 0.3vw 0.3vw;
                    padding-bottom: 0.25vw;
                    margin-bottom: 0.15vw;
                }
                &.first.last {
                    border-radius: 0.3vw;
                }
            }
            // A block, so the bar covers wrapped lines and runs on into the next quote line
            &.quote {
                display: block;
                border-left: 0.15vw solid rgba(255, 255, 255, 0.35);
                padding-left: 0.5vw;
            }
        }

        &.jumbo {
            font-size: 2.25em;
            line-height: 1.2;
        }
    }
}
</style>
