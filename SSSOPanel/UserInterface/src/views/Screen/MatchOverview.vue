<template>
    <ScreenLayout>
        <section
            class="screen-match-overview"
            :class="{ column: layoutColumn, 'pick-ban': layoutPickBan, 'chat-pick-up': layoutChatPick }"
            :style="{ '--move': `${MOVE}ms`, '--chatPickHeight': `${CHAT_PICK_HEIGHT}vw`, '--refDenTop': layoutChatPick ? `${2.5 + CHAT_PICK_HEIGHT + 1}vw` : '2.5vw' }"
        >
            <div class="tint themed"></div>
            <div class="noise"></div>
            <!-- Tall column on the right, the content moves over while it's up: chat's pick on top, the ref den under it.
                 Without the ref den chat's pick doesn't move the cards: above player 2, or above VERSUS (pushed down)
                 with the pick/ban board, it glides between the spots with the cards -->
            <transition name="column-pick">
                <ScreenChatPick
                    v-if="showChatPick"
                    class="column-pick"
                    :spulti="spulti"
                    :players="{ player1, player2 }"
                />
            </transition>
            <ScreenRefDen variant="column" />

            <div class="content">
                <section
                    class="player-info"
                    v-if="player1 && player2"
                >
                    <div class="player">
                        <header>SPEENOPEN PLAYER</header>
                        <div class="box">
                            <div class="avatar-box">
                                <div
                                    class="avatar"
                                    :style="`background-image: url(${player1.spinshareProfile?.avatar ?? ''})`"
                                ></div>
                            </div>
                            <div class="meta">
                                <div
                                    v-if="player1.spinshareProfile?.pronouns"
                                    class="pronouns"
                                >
                                    {{ player1.spinshareProfile?.pronouns }}
                                </div>
                                <div
                                    class="username"
                                    :style="{ fontSize: fitFontSize(player1.spinshareProfile?.username ?? player1.name, 1, 20, 0.6, 'em') }"
                                >
                                    {{ player1.spinshareProfile?.username ?? player1.name }}
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="middle">
                        <div class="versus"><span>V</span><span>E</span><span>R</span><span>S</span><span>U</span><span>S</span></div>
                        <div class="below">
                            <span
                                v-if="roundLabel"
                                class="round"
                                >{{ roundLabel }}</span
                            >
                        </div>
                    </div>
                    <div class="player">
                        <header>SPEENOPEN PLAYER</header>
                        <div class="box">
                            <div class="avatar-box">
                                <div
                                    class="avatar"
                                    :style="`background-image: url(${player2.spinshareProfile?.avatar ?? ''})`"
                                ></div>
                            </div>
                            <div class="meta">
                                <div
                                    v-if="player2.spinshareProfile?.pronouns"
                                    class="pronouns"
                                >
                                    {{ player2.spinshareProfile?.pronouns }}
                                </div>
                                <div
                                    class="username"
                                    :style="{ fontSize: fitFontSize(player2.spinshareProfile?.username ?? player2.name, 1, 20, 0.6, 'em') }"
                                >
                                    {{ player2.spinshareProfile?.username ?? player2.name }}
                                </div>
                            </div>
                        </div>
                    </div>
                </section>
                <!-- spulti's coin toss and draft under the cards, which move up for it -->
                <transition
                    name="pick-ban"
                    @after-leave="layoutPickBan = showPickBan"
                >
                    <div
                        v-if="showPickBan"
                        class="pick-ban-area"
                    >
                        <ScreenPickBan
                            variant="panel"
                            :height="PICK_BAN_HEIGHT"
                            :width="layoutColumn ? 65.5 : 93"
                            :spulti="spulti"
                            :players="{ player1, player2 }"
                            :coin-flip-key="coinFlipKey"
                            :coin-flip-at="boardFlipAt"
                        />
                    </div>
                </transition>
            </div>

            <div class="marquees">
                <div class="marquee top">
                    <template
                        v-for="n in 10"
                        :key="n"
                    >
                        <span>COMING UP</span>
                    </template>
                </div>
                <div class="marquee bottom">
                    <template
                        v-for="n in 10"
                        :key="n"
                    >
                        <span>COMING UP</span>
                    </template>
                </div>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import { computed, onMounted, onUnmounted, ref, inject, watch } from 'vue';
import ScreenRefDen from '@/components/ScreenRefDen/ScreenRefDen.vue';
import ScreenPickBan from '@/components/ScreenSpulti/ScreenPickBan.vue';
import { fitFontSize } from '@/modules/fitText';
import ScreenChatPick from '@/components/ScreenSpulti/ScreenChatPick.vue';
import useSpulti from '@/modules/useSpulti';
const emitter = inject('emitter');

const player1 = ref(null);
const player2 = ref(null);
const refDenIsVisible = ref(false);

// vw, the board under the cards, see the pick-ban styles
const PICK_BAN_HEIGHT = 20.5;
const spulti = useSpulti();
const pickBanVisible = ref(false);
const coinFlipKey = ref(null);
const coinFlipAt = ref(0);
const showPickBan = computed(() => pickBanVisible.value && !!spulti.value.match && !!player1.value && !!player2.value);
// The match's Twitch prediction, unless the operator turned it off (Overlays menu)
const chatPickVisible = ref(true);
const showChatPick = computed(() => chatPickVisible.value && !!spulti.value.prediction && !!spulti.value.match && spulti.value.match.id === spultiMatchId.value);
// The start.gg round of the spulti match these players are from, the bracket on its own line
const spultiMatchId = ref(null);
const roundLabel = computed(() => {
    const match = spulti.value.match;
    if (!match || match.id !== spultiMatchId.value) return '';
    return [match.startgg?.round, match.startgg?.bracket].filter(Boolean).join('\n');
});

// vw, chat's pick box at the top of the column, the ref den starts under it
const CHAT_PICK_HEIGHT = 7.5;

// The layout classes, they lag behind on the way back: the cards only move once the board or the column (ref den,
// chat's pick) is gone (and coming in, the board waits for the cards, its enter delay is MOVE)
const MOVE = 500;
// ScreenRefDen's column leave, chat's pick leaves as fast
const COLUMN_LEAVE = 250;
const layoutPickBan = ref(false);
const layoutColumn = ref(false);
// The ref den only moves up into chat's pick spot once it faded out
const layoutChatPick = ref(false);
// When the board last came in
const boardShownAt = ref(0);
watch(showPickBan, (show) => {
    // Off happens in the board's after-leave
    if (!show) return;
    layoutPickBan.value = true;
    boardShownAt.value = Date.now();
});
// A flip that opened the board (Auto Spulti) starts once the board shows, MOVE later, not while it's still hidden
const boardFlipAt = computed(() => {
    const opened = boardShownAt.value - coinFlipAt.value < 1000 && coinFlipAt.value < boardShownAt.value + MOVE;
    return opened ? boardShownAt.value + MOVE : coinFlipAt.value;
});
let chatPickTimer = null;
watch(
    showChatPick,
    (show) => {
        clearTimeout(chatPickTimer);
        if (show) layoutChatPick.value = true;
        else chatPickTimer = setTimeout(() => (layoutChatPick.value = false), COLUMN_LEAVE);
    },
    { immediate: true },
);
let columnTimer = null;
watch(
    () => refDenIsVisible.value,
    (visible) => {
        clearTimeout(columnTimer);
        if (visible) layoutColumn.value = true;
        else columnTimer = setTimeout(() => (layoutColumn.value = false), COLUMN_LEAVE);
    },
);

// Toggles come in through state-set, they don't touch the players
const applyRefDen = (state) => {
    refDenIsVisible.value = state?.refDenIsVisible ?? false;
    pickBanVisible.value = state?.pickBanVisible ?? pickBanVisible.value;
    coinFlipKey.value = state?.coinFlipKey ?? coinFlipKey.value;
    coinFlipAt.value = state?.coinFlipAt ?? coinFlipAt.value;
    spultiMatchId.value = state?.currentMatch?.spultiMatchId ?? spultiMatchId.value;
    chatPickVisible.value = state?.chatPickVisible ?? chatPickVisible.value;
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        player1.value = state?.currentMatch?.players?.player1 ?? player1.value;
        player2.value = state?.currentMatch?.players?.player2 ?? player2.value;
        applyRefDen(state);
    });
    emitter.on('state-set-response', applyRefDen);

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-get',
        }),
    );
});
onUnmounted(() => {
    clearTimeout(columnTimer);
    clearTimeout(chatPickTimer);
    emitter.off('state-get-response');
    emitter.off('state-set-response', applyRefDen);
});
</script>

<style lang="scss" scoped>
.screen-match-overview {
    color: #222;
    position: relative;
    display: flex;
    justify-content: center;
    align-items: center;

    & .tint {
        background: url('../../assets/background.svg');
        background-size: cover;
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        top: 0;
        z-index: 1;

        &.themed {
            background-image: var(--themeBackground);
            filter: none;
        }
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5em;
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        top: 0;
        mix-blend-mode: soft-light;
        z-index: 2;
    }

    & .content {
        z-index: 10;
        position: absolute;
        top: 5vw;
        bottom: 5vw;
        left: 5vw;
        right: 5vw;
        display: grid;
        grid-template-rows: 1fr;
        gap: 5vw;
        // Moves for the ref den and pick/ban, see the end

        & .player-info {
            display: grid;
            // minmax, a long name would stretch its card off screen
            grid-template-columns: minmax(0, 1fr) auto minmax(0, 1fr);
            justify-content: center;
            align-items: center;
            gap: 5vw;

            & .player {
                min-width: 0;
                display: flex;
                flex-direction: column;
                align-items: flex-start;

                & header {
                    background: #111;
                    color: #ededed;
                    padding: 1vw 2vw;
                    border-radius: 0.5vw 0.5vw 0 0;
                    font-weight: 600;
                    font-size: 1vw;
                    letter-spacing: 0.15vw;
                }

                & .box {
                    flex-grow: 1;
                    align-self: stretch;
                    display: flex;
                    flex-direction: column;
                    gap: 2vw;
                    background: #111;
                    color: #ededed;
                    padding: 2vw;
                    border-radius: 0 0.5vw 0.5vw 0.5vw;

                    & .avatar-box {
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        flex-grow: 1;

                        & .avatar {
                            background-position: center;
                            background-size: cover;
                            width: 15vw;
                            height: 15vw;
                            border-radius: 1.5vw;
                        }
                    }

                    // Pronouns + a full size name, so both cards are the same height with a shrunk name or no pronouns
                    & .meta {
                        display: flex;
                        flex-direction: column;
                        justify-content: flex-end;
                        gap: 0.25vw;
                        min-width: 0;
                        min-height: 2.75vw;

                        & .pronouns {
                            color: rgba(255, 255, 255, 0.6);
                            font-size: 1vw;
                            font-weight: 600;
                        }

                        & .username {
                            font-weight: 600;
                            letter-spacing: 0.1vw;
                            white-space: nowrap;
                            overflow: hidden;
                            text-overflow: ellipsis;
                        }
                    }
                }
            }

            // The round hangs under VERSUS without widening the middle column
            & .middle {
                position: relative;

                & .below {
                    position: absolute;
                    top: 100%;
                    left: 50%;
                    transform: translateX(-50%);
                    margin-top: 0.5vw;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    gap: 1vw;
                }

                & .round {
                    // A long round wraps instead of running into the cards
                    width: max-content;
                    max-width: 23vw;
                    text-align: center;
                    font-family: 'JetBrains Mono', 'Noto Sans JP', monospace;
                    font-size: 1vw;
                    font-weight: 700;
                    letter-spacing: 0.1em;
                    text-transform: uppercase;
                    opacity: 0.8;
                    // round and bracket on their own lines
                    white-space: pre-line;
                }
            }

            & .versus {
                font-size: 4vw;
                font-family: 'JetBrains Mono', 'Noto Sans JP', monospace;
                font-weight: 900;
                display: flex;
                gap: -0.5vw;

                & span {
                    display: block;
                    transform: scale(1);
                    animation: versusLoop 1s cubic-bezier(1, 0, 0, 1) infinite;

                    &:nth-child(2) {
                        animation-delay: calc(0.1s * 1);
                    }

                    &:nth-child(3) {
                        animation-delay: calc(0.2s * 2);
                    }

                    &:nth-child(4) {
                        animation-delay: calc(0.1s * 3);
                    }

                    &:nth-child(5) {
                        animation-delay: calc(0.2s * 4);
                    }

                    &:nth-child(6) {
                        animation-delay: calc(0.1s * 5);
                    }
                }
            }
        }
    }

    & .marquees {
        transform: rotate(-4deg) scale(1);
        position: absolute;
        top: 4vw;
        bottom: 4vw;
        left: 0;
        right: 0;
        z-index: 3;

        & .marquee {
            white-space: nowrap;
            display: flex;
            position: absolute;
            will-change: transform;
            font-size: 3vw;
            font-weight: 800;
            color: rgba(0, 0, 0, 0.2);

            & span {
                padding-right: 1em;
            }

            &.top {
                top: 0;
                left: 0;
                animation: marqueeLeft 10s infinite linear;
            }

            &.bottom {
                bottom: 0;
                left: 0;
                animation: marqueeRight 10s infinite linear;
            }
        }
    }
}

// 10 copies, so -10% is exactly one copy and the loop is seamless
@keyframes marqueeLeft {
    from {
        transform: translateX(-10%);
    }
    to {
        transform: translateX(0);
    }
}

@keyframes marqueeRight {
    from {
        transform: translateX(0);
    }
    to {
        transform: translateX(-10%);
    }
}

@keyframes versusLoop {
    from {
        transform: scale(1);
    }
    50% {
        transform: scale(1, 0.25);
    }
    to {
        transform: scale(1);
    }
}

// Ref den and pick/ban move the content with its insets, avatar size and the cards' margin, all on one transition
// (--move, no delays, the lagging layout classes do the ordering). align-items stays center: with the margin the
// cards' row is as tall as the cards, so they end up on the top edge too.
// Ref den: the content ends 2.5vw before the column (72.5vw). Pick/ban: 2.5vw on every edge, the cards keep
// their size under the top bar with smaller avatars, the board takes the bottom PICK_BAN_HEIGHT
.screen-match-overview .content {
    transition-property: top, bottom, left, right;
    transition-duration: var(--move);
    transition-timing-function: ease-in-out;

    & .player-info {
        transition: margin-bottom var(--move) ease-in-out;
    }

    & .avatar {
        transition: width var(--move) ease-in-out, height var(--move) ease-in-out;
    }

    & .pick-ban-area {
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        height: 20.5vw;
    }
}
.screen-match-overview.column .content {
    left: 2.5vw;
    right: 30vw;
}
.screen-match-overview.pick-ban .content {
    top: 2.5vw;
    bottom: 2.5vw;
    left: 2.5vw;

    & .player-info {
        // Board + gap
        margin-bottom: 23vw;
    }

    & .player-info .player .box .avatar-box .avatar {
        width: 10.5vw;
        height: 10.5vw;
    }
}
.screen-match-overview.pick-ban:not(.column) .content {
    right: 2.5vw;
}

// Waits for the cards to move, leaves before they move back (after-leave)
.pick-ban-enter-active {
    transition: opacity 0.35s ease-out var(--move), transform 0.35s ease-out var(--move);
}
.pick-ban-leave-active {
    transition: opacity 0.3s ease-in, transform 0.3s ease-in;
}
.pick-ban-enter-from,
.pick-ban-leave-to {
    opacity: 0;
    transform: translateY(2vw);
}
// Above player 2's card (content right 5vw, the card starts ~11.6vw down)
.column-pick {
    position: absolute;
    top: 3vw;
    right: 5vw;
    width: 25vw;
    height: var(--chatPickHeight);
    z-index: 20;
    transition-property: top, right, width, height;
    transition-duration: var(--move);
    transition-timing-function: ease-in-out;
}
// Centered over VERSUS, between the cards at the top
.pick-ban .column-pick {
    top: 2.5vw;
    right: 39vw;
    width: 22vw;
    height: 6.5vw;
}
// Top of the ref den column (CHAT_PICK_HEIGHT, --refDenTop)
.column .column-pick {
    top: 2.5vw;
    right: 2.5vw;
    width: 25vw;
    height: var(--chatPickHeight);
}
// VERSUS makes room for it with the pick/ban board, same move as the cards
.screen-match-overview .content .player-info .middle {
    transition: transform var(--move) ease-in-out;
}
.screen-match-overview.pick-ban.chat-pick-up:not(.column) .content .player-info .middle {
    transform: translateY(3.5vw);
}
// Comes in once the content moved over, leaves before it grows back (COLUMN_LEAVE), keeps gliding meanwhile
.column-pick-enter-active {
    transition: opacity 0.4s cubic-bezier(0.22, 1, 0.36, 1) var(--move), transform 0.4s cubic-bezier(0.22, 1, 0.36, 1) var(--move), top var(--move) ease-in-out, right var(--move) ease-in-out, width var(--move) ease-in-out, height var(--move) ease-in-out;
}
.column-pick-leave-active {
    transition: opacity 0.25s cubic-bezier(0.55, 0, 1, 0.45), transform 0.25s cubic-bezier(0.55, 0, 1, 0.45);
}
.column-pick-enter-from,
.column-pick-leave-to {
    opacity: 0;
    transform: translateX(4vw);
}
</style>
