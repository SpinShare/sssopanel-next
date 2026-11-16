<template>
    <!-- Chat's pick: the Twitch prediction on the followed spulti match, split by Speen Coins. Each player's half of the
         dark box (avatar, name, share) with a strip along the bottom like the tug of war: a muted track, each side's
         share fills its half with the theme tint from its outer edge, 100% reaches the middle line.
         Once Twitch paid out, the players swap for the verdict: the winner's avatar with a badge, "Chat called it!" or
         "Upset!" and how much of chat backed them. The strip stays.
         card: like the ref den, a tinted label over the box (match-overview's column). banner: in place of the
         spinsha.re banner on match-ingame, same size -->
    <div
        class="chat-pick"
        :class="[`is-${variant}`, { split: shares }]"
    >
        <header v-if="variant === 'card'">
            <div class="tint themed"></div>
            <div class="noise"></div>
            <span class="mdi mdi-twitch"></span>
            <span>CHAT'S PICK</span>
        </header>
        <div class="box">
            <template v-if="!shares && variant === 'banner'">
                <div class="tint themed"></div>
                <div class="noise"></div>
            </template>
            <transition
                name="swap"
                mode="out-in"
            >
                <div
                    v-if="verdict"
                    key="verdict"
                    class="main verdict"
                >
                    <span
                        class="avatar"
                        :style="{ backgroundImage: verdict.avatar ? `url(${verdict.avatar})` : null }"
                    >
                        <span
                            class="badge mdi"
                            :class="verdict.icon"
                        ></span>
                    </span>
                    <div class="text">
                        <span class="headline">{{ verdict.status }}</span>
                        <ScreenMarquee
                            class="sub"
                            overlap-start
                            >{{ verdict.text }}</ScreenMarquee
                        >
                    </div>
                </div>
                <div
                    v-else-if="shares"
                    key="players"
                    class="main players"
                >
                    <div
                        v-for="part in shares"
                        :key="part.side"
                        class="side"
                        :class="part.side"
                    >
                        <span
                            class="avatar"
                            :style="{ backgroundImage: part.avatar ? `url(${part.avatar})` : null }"
                        ></span>
                        <div class="text">
                            <!-- Long names scroll like the song in the footer -->
                            <ScreenMarquee class="name">{{ part.name }}</ScreenMarquee>
                            <span class="share">{{ part.share }}%</span>
                        </div>
                    </div>
                </div>
            </transition>
            <div
                v-if="shares"
                class="strip"
            >
                <div class="tint muted"></div>
                <div class="noise"></div>
                <div
                    v-for="part in shares"
                    :key="part.side"
                    class="part"
                    :class="part.side"
                    :style="{ width: `${part.share / 2}%` }"
                >
                    <div class="tint themed"></div>
                    <div class="noise"></div>
                </div>
            </div>
            <!-- banner: no header, the label on a line of its own -->
            <div
                v-if="variant === 'banner'"
                class="top"
            >
                <span><span class="mdi mdi-twitch"></span> Chat's pick</span>
            </div>
            <!-- Without votes there are no sides, the status gets the box -->
            <span
                v-if="!shares && status"
                class="status"
                >{{ status }}</span
            >
        </div>
    </div>
</template>

<script setup>
import { computed } from 'vue';
import { predictionShares, predictionVerdict, sideMap } from '@/modules/useSpulti';
import ScreenMarquee from '@/components/ScreenMarquee/ScreenMarquee.vue';

const props = defineProps({
    // spulti-update snapshot, with its prediction
    spulti: {
        type: Object,
        required: true,
    },
    // state currentMatch.players, for which side is on the left
    players: {
        type: Object,
        default: null,
    },
    variant: {
        type: String,
        default: 'card',
    },
});

const prediction = computed(() => props.spulti.prediction);

// Left to right like the cards, null until Twitch reported any coins. Our player's avatar and name, Twitch's
// outcome name when they aren't on screen
const shares = computed(() => {
    const parts = predictionShares(prediction.value);
    if (!parts) return null;
    const sides = sideMap(props.spulti.match, props.players);
    return parts
        .map((outcome) => {
            const player = props.players?.[sides[outcome.side]];
            return {
                side: sides[outcome.side],
                share: outcome.share,
                spultiSide: outcome.side,
                avatar: player?.spinshareProfile?.avatar ?? null,
                name: player?.spinshareProfile?.username ?? player?.name ?? outcome.name,
            };
        })
        .sort((a, b) => a.side.localeCompare(b.side));
});

// Same call as match results (predictionVerdict), an even split has no favorite
const call = computed(() => predictionVerdict(prediction.value));

const status = computed(() => {
    const p = prediction.value;
    if (!p) return '';
    // no call to vote, the viewers are on Twitch already
    if (p.status === 'open') return shares.value ? '' : 'No votes yet';
    if (p.status === 'locked') return shares.value ? '' : "Voting's closed";
    if (call.value?.called == null) return '';
    return call.value.called ? 'Chat called it!' : 'Upset!';
});

// The full card once it paid out, nothing on an even split (no favorite to call)
const verdict = computed(() => {
    if (call.value?.called == null || !shares.value) return null;
    const winner = shares.value.find((part) => part.spultiSide === call.value.side);
    if (!winner) return null;
    const called = call.value.called;
    return {
        status: status.value,
        icon: called ? 'mdi-check-bold' : 'mdi-lightning-bolt',
        avatar: winner.avatar,
        text: `${called ? '' : 'Only '}${winner.share}% of chat backed ${winner.name}`,
    };
});
</script>

<style lang="scss" scoped>
.chat-pick {
    display: flex;
    flex-direction: column;

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

    // left side, right side. The header keeps the ref den's font
    & .box {
        flex-grow: 1;
        position: relative;
        overflow: hidden;
        display: grid;
        grid-template-columns: minmax(0, 1fr);
        align-content: center;
        align-items: center;
        background: #111;
        color: #fff;
        font-family: 'JetBrains Mono', 'Noto Sans JP', monospace;
    }

    // Muted version of the grainy background, same as the tug of war's track
    & .tint.muted {
        background-image: url('../../assets/background.svg');
        filter: contrast(1) brightness(0.22) grayscale(2);
    }

    // The strip runs along the bottom edge, the box has no padding for it. The players' row takes the rest, centered
    &.split .box {
        align-content: stretch;
        grid-template-rows: minmax(0, 1fr) auto;
    }

    & .strip {
        grid-column: 1 / -1;
        position: relative;
        overflow: hidden;
        height: 1.4vw;

        // Middle line, where a side with every coin ends up
        &::after {
            content: '';
            position: absolute;
            left: calc(50% - 0.0625vw);
            top: 0;
            bottom: 0;
            width: 0.125vw;
            background: rgba(255, 255, 255, 0.4);
            z-index: 2;
        }

        & .part {
            position: absolute;
            top: 0;
            bottom: 0;
            overflow: hidden;
            transition: width 0.6s cubic-bezier(0.65, 0, 0.35, 1);

            &.player1 {
                left: 0;
            }

            &.player2 {
                right: 0;
            }
        }
    }

    & .main {
        position: relative;
        z-index: 10;
    }

    & .players {
        display: grid;
        grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
    }

    & .verdict {
        display: flex;
        align-items: center;
        gap: 1vw;
        padding: 0.6vw 1.2vw;
        min-width: 0;
        color: #ededed;

        & .avatar {
            position: relative;
            flex-shrink: 0;
            border-radius: 50%;
            background: #333 center / cover;
        }

        // Theme tinted like the header, sits on the avatar's corner
        & .badge {
            position: absolute;
            right: -0.2vw;
            bottom: -0.2vw;
            display: grid;
            place-items: center;
            width: 1.4vw;
            height: 1.4vw;
            border-radius: 50%;
            background: var(--themeBackground) center / cover;
            box-shadow: 0 0 0 0.15vw #111;
            color: #111;
            font-size: 0.85vw;
        }

        & .text {
            display: flex;
            flex-direction: column;
            min-width: 0;
            gap: 0.15vw;
        }

        & .headline {
            font-weight: 900;
            text-transform: uppercase;
            letter-spacing: 0.05vw;
            line-height: 1;
        }

        & .sub {
            --fade: 0.6vw;
            opacity: 0.8;
        }
    }

    // Out quick and accelerating, the verdict pops in decelerating
    .swap-enter-active {
        transition: opacity 0.35s cubic-bezier(0.22, 1, 0.36, 1), transform 0.45s cubic-bezier(0.34, 1.4, 0.64, 1);
    }
    .swap-leave-active {
        transition: opacity 0.2s cubic-bezier(0.55, 0, 1, 0.45);
    }
    .swap-enter-from {
        opacity: 0;
        transform: scale(0.95);
    }
    .swap-leave-to {
        opacity: 0;
    }

    & .side {
        position: relative;
        z-index: 10;
        display: flex;
        align-items: center;
        gap: 0.6vw;
        min-width: 0;
        padding: 0.6vw 1vw;
        color: #ededed;

        &.player1 {
            grid-column: 1;
        }

        &.player2 {
            grid-column: 2;
            flex-direction: row-reverse;
            text-align: right;
        }

        & .avatar {
            flex-shrink: 0;
            border-radius: 50%;
            background: #333 center / cover;
        }

        & .text {
            display: flex;
            flex-direction: column;
            min-width: 0;
        }

        & .name {
            --fade: 0.6vw;
            font-weight: 700;
        }

        // A name that fits stays against the avatar on the right side too
        &.player2 .name:not(.scrolling) :deep(.track) {
            margin-left: auto;
        }

        & .share {
            font-weight: 900;
            line-height: 1;
        }
    }

    & .top,
    & .status {
        grid-column: 1 / -1;
        position: relative;
        z-index: 10;
        font-weight: 700;
    }

    & .top {
        display: flex;
        justify-content: space-between;
        gap: 1vw;
        white-space: nowrap;
    }

    & .status {
        text-align: center;
    }

    // Same pieces as the ref den next to it
    &.is-card {
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
        }

        & .box {
            border-radius: 0 0 0.5vw 0.5vw;
        }

        & .avatar {
            width: 2.6vw;
            height: 2.6vw;
        }

        & .name {
            font-size: 0.85vw;
        }

        & .share {
            font-size: 1.5vw;
        }

        & .verdict {
            .avatar {
                width: 3.2vw;
                height: 3.2vw;
            }

            .headline {
                font-size: 1.5vw;
            }

            .sub {
                font-size: 0.85vw;
            }
        }

        & .status {
            font-size: 0.9vw;
        }
    }

    // Same box as the spinsha.re banner it takes the place of
    &.is-banner {
        width: 25vw;

        & .box {
            border-radius: 0.5vw;
        }

        &:not(.split) .box {
            color: #111;
        }

        & .avatar {
            width: 2.6vw;
            height: 2.6vw;
        }

        & .name {
            font-size: 0.9vw;
        }

        & .share {
            font-size: 1.6vw;
        }

        & .top {
            font-size: 0.8vw;
            letter-spacing: 0.1vw;
            padding: 0.7vw 1vw 0;
        }

        & .status {
            padding: 1vw;
        }

        // label, then the players, then the strip
        &.split .box {
            grid-template-rows: auto minmax(0, 1fr) auto;
        }

        & .top {
            grid-row: 1;
        }

        & .main {
            grid-row: 2;
        }

        & .verdict {
            .avatar {
                width: 3vw;
                height: 3vw;
            }

            .headline {
                font-size: 1.5vw;
            }

            .sub {
                font-size: 0.9vw;
            }
        }

        & .strip {
            grid-row: 3;
        }

        & .status {
            font-size: 0.9vw;
        }
    }
}
</style>
