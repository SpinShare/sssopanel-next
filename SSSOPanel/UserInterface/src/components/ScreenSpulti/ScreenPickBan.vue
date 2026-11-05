<template>
    <section class="screen-pick-ban">
        <div class="backdrop">
            <div class="tint"></div>
            <div class="noise"></div>
        </div>

        <div class="content">
            <!-- A fresh toss opens the board: big flip, the result, then it shrinks away and the charts come in -->
            <transition
                name="intro"
                mode="out-in"
            >
                <div
                    v-if="introPlaying"
                    key="intro"
                    class="intro"
                >
                    <div class="coin">
                        <ScreenCoin
                            :key="coinFlipAt"
                            :color="coinColor"
                            :elapsed="introElapsed"
                            @landed="introLanded = true"
                        />
                    </div>
                    <div
                        class="caption"
                        :class="{ shown: introLanded }"
                    >
                        <div class="tint themed"></div>
                        <div class="noise"></div>
                        <span>{{ name(coin.side) }} {{ coin.text }}!</span>
                    </div>
                </div>
                <div
                    v-else
                    key="board"
                    class="board"
                >
                    <header>
                        <!-- The toss stays up small during the strike -->
                        <div
                            v-if="coin && striking"
                            class="toss"
                        >
                            <div class="coin">
                                <ScreenCoin :color="coinColor" />
                            </div>
                            <span>{{ name(coin.side) }} {{ coin.text }}</span>
                        </div>
                        <div
                            class="turn"
                            :class="{ themed: !!turn.side }"
                        >
                            <div class="tint themed"></div>
                            <div class="noise"></div>
                            <span
                                class="mdi"
                                :class="turn.icon"
                            ></span>
                            <span>{{ turn.text }}</span>
                        </div>
                    </header>

                    <div
                        v-if="phase?.type === 'roll'"
                        class="idle-coin"
                    >
                        <ScreenCoin color="transparent" />
                    </div>
                    <div
                        v-if="cards.length"
                        class="cards"
                        :style="{ '--card': cardSize }"
                    >
                        <div
                            v-for="(row, i) in rows"
                            :key="i"
                            class="row"
                        >
                            <div
                                v-for="card in row"
                                :key="card.key"
                                class="card"
                                :class="card.status"
                            >
                                <div class="cover">
                                    <!-- Its own layer, so dimming the art leaves the label and tag readable -->
                                    <div
                                        class="art"
                                        :style="card.cover ? `background-image: url('${card.cover}')` : ''"
                                    >
                                        <span
                                            v-if="!card.cover"
                                            class="mdi mdi-music"
                                        ></span>
                                    </div>
                                    <span class="label">{{ card.label }}</span>
                                    <span
                                        v-if="card.difficulty"
                                        class="difficulty"
                                        >{{ card.difficulty }}</span
                                    >
                                    <div
                                        v-if="card.tag"
                                        class="tag"
                                    >
                                        <div
                                            v-if="card.status !== 'banned'"
                                            class="tint themed"
                                        ></div>
                                        <span>{{ card.tag }}</span>
                                        <span
                                            v-if="card.by"
                                            class="by"
                                            >{{ card.by }}</span
                                        >
                                    </div>
                                </div>
                                <div class="title">{{ card.title }}</div>
                                <div class="artist">{{ card.artist }}</div>
                            </div>
                        </div>
                    </div>
                </div>
            </transition>
        </div>
    </section>
</template>

<script setup>
import { computed, onUnmounted, ref, watch } from 'vue';
import { coinResult, sideMap, slotStates, spultiPhase } from '@/modules/useSpulti';
import { COIN_INTRO_DURATION } from '@/modules/useSpultiOverlays';
import ScreenCoin from '@/components/ScreenSpulti/ScreenCoin.vue';

// Matches ScreenCoin's flip
const FLIP_TIME = 3000;

const props = defineProps({
    // spulti-update snapshot
    spulti: {
        type: Object,
        required: true,
    },
    // state currentMatch.players, for names and which side is which
    players: {
        type: Object,
        default: null,
    },
    // state coinFlipKey / coinFlipAt: which toss got its flip and when
    coinFlipKey: {
        type: String,
        default: null,
    },
    coinFlipAt: {
        type: Number,
        default: 0,
    },
});

const match = computed(() => props.spulti.match);
const sides = computed(() => sideMap(match.value, props.players));

// Our player's name when they're linked, spulti's otherwise
const name = (spultiSide) => {
    if (!spultiSide) return '';
    const player = props.players?.[sides.value[spultiSide]];
    return player?.spinshareProfile?.username ?? player?.name ?? match.value?.[spultiSide]?.name ?? '';
};

const phase = computed(() => spultiPhase(match.value, props.spulti.mappool));
const striking = computed(() => (match.value?.sets?.length ?? 0) === 0);

const coin = computed(() => coinResult(match.value));
// spulti's tap color per spulti side, like its own coin toss
const coinColor = computed(() => (coin.value?.side === 'player1' ? '#25a7fb' : '#fb1357'));

// The intro runs for COIN_INTRO_DURATION from the flip, a screen that loads in partway skips ahead
const introPlaying = ref(false);
const introLanded = ref(false);
const introElapsed = ref(0);
let introTimer = null;
watch(
    () => [props.coinFlipAt, props.coinFlipKey, coin.value?.key],
    () => {
        clearTimeout(introTimer);
        const elapsed = Date.now() - props.coinFlipAt;
        introPlaying.value = !!coin.value && coin.value.key === props.coinFlipKey && elapsed < COIN_INTRO_DURATION;
        if (!introPlaying.value) return;
        introElapsed.value = elapsed;
        introLanded.value = elapsed >= FLIP_TIME;
        introTimer = setTimeout(() => (introPlaying.value = false), COIN_INTRO_DURATION - elapsed);
    },
    { immediate: true },
);
onUnmounted(() => clearTimeout(introTimer));

const turn = computed(() => {
    const set = (match.value?.sets?.length ?? 0) + 1;
    switch (phase.value?.type) {
        case 'build':
            return { icon: 'mdi-playlist-plus', text: 'Building the pool' };
        case 'roll':
            return { icon: 'mdi-circle-multiple-outline', text: 'Waiting for the coin toss' };
        case 'ban':
            return { icon: 'mdi-cancel', text: `${name(phase.value.side)} is banning`, side: phase.value.side };
        case 'pick':
            return { icon: 'mdi-cursor-default-click', text: `${name(phase.value.side)} picks set ${set}`, side: phase.value.side };
        case 'tiebreak':
            return { icon: 'mdi-dice-multiple', text: `Tie! Rolling a tiebreak for set ${set}` };
        case 'play':
            return { icon: 'mdi-play', text: `Set ${set}: ${phase.value.chart?.title ?? ''}` };
        case 'finished':
            return { icon: 'mdi-trophy', text: match.value?.winner ? `${name(match.value.winner)} wins the match!` : 'Match over' };
        default:
            return { icon: 'mdi-timer-sand', text: 'Waiting for the match' };
    }
});

const rating = (chart, difficulty) => {
    if (!difficulty) return null;
    const value = chart?.ratings?.[difficulty];
    return value ? `${difficulty} ${value}` : difficulty;
};

const cards = computed(() => {
    // Nothing before the coin toss, tournament pools are secret until then (spulti won't send them either)
    if (!match.value || phase.value?.type === 'roll') return [];

    const states = slotStates(match.value, props.spulti.mappool);
    const upNext = phase.value?.type === 'play' ? phase.value.slot?.id : null;

    const cards = (props.spulti.mappool?.slots ?? []).map((slot) => {
        const state = states[slot.id] ?? {};
        let status = 'open';
        let tag = null;
        let by = null;

        if (state.played !== undefined) {
            status = 'played';
            tag = `Set ${state.played + 1}`;
            by = state.winner ? `${name(state.winner)} won` : 'Tie';
        } else if (slot.id === upNext) {
            status = 'next';
            tag = `Set ${(match.value.sets?.length ?? 0) + 1}`;
            by = state.picked ? `Picked by ${name(state.picked)}` : null;
        } else if (striking.value && state.banned !== undefined) {
            // Bans only count for the first chart, after that struck charts can be picked again
            status = 'banned';
            tag = 'Banned';
            by = state.banned ? name(state.banned) : null;
        }

        return {
            key: slot.id,
            label: slot.label,
            title: slot.chart.title,
            artist: slot.chart.artist,
            cover: slot.chart.coverUrl,
            difficulty: rating(slot.chart, slot.difficulty),
            status,
            tag,
            by,
        };
    });

    // Tiebreaks come from outside the pool
    (match.value.draft?.actions ?? []).forEach((action, i) => {
        if (action.type !== 'tiebreak') return;
        const set = match.value.sets?.[action.forSet];
        cards.push({
            key: `tiebreak-${i}`,
            label: 'TB',
            title: action.chart.title,
            artist: action.chart.artist,
            cover: action.chart.coverUrl,
            difficulty: rating(action.chart, action.difficulty),
            status: set ? 'played' : 'next',
            tag: `Set ${action.forSet + 1}`,
            by: set ? (set.winner ? `${name(set.winner)} won` : 'Tie') : 'Tiebreak',
        });
    });

    return cards;
});

// Charts per row by best of (spulti pools have best of + 2 charts)
const POOL_LAYOUTS = {
    3: [5],
    5: [3, 4],
    7: [4, 5],
    9: [5, 6],
};
// Biggest card that fits, one row or two in the 1920x540 cutout (in vw, 1vw gaps)
const MAX_CARD = { 1: 11, 2: 7.6 };

const rows = computed(() => {
    const count = cards.value.length;
    let layout = POOL_LAYOUTS[match.value?.bestOf];
    // Anything else, like an extra tiebreak card: one row up to 6, then two even ones
    if (!layout || layout.reduce((a, b) => a + b, 0) !== count) {
        layout = count <= 6 ? [count] : [Math.floor(count / 2), Math.ceil(count / 2)];
    }
    let start = 0;
    return layout.map((size) => cards.value.slice(start, (start += size)));
});

const cardSize = computed(() => {
    const widest = Math.max(...rows.value.map((row) => row.length));
    return `${Math.min(MAX_CARD[rows.value.length] ?? 7.6, (88 - (widest - 1)) / widest)}vw`;
});
</script>

<style lang="scss" scoped>
// Same dimmed grainy cutout as the set results
.screen-pick-ban {
    position: absolute;
    inset: 0;
    overflow: hidden;

    & .backdrop {
        position: absolute;
        inset: 0;
        opacity: 0.8;
    }

    & .tint {
        background: url('../../assets/background.svg');
        background-size: cover;
        position: absolute;
        inset: 0;
        filter: contrast(1) brightness(0.125) grayscale(2);

        &.themed {
            background-image: var(--themeBackground);
            filter: none;
        }
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5em;
        position: absolute;
        inset: 0;
        opacity: 0.4;
        mix-blend-mode: soft-light;
    }

    & .content {
        position: absolute;
        inset: 0;
        z-index: 10;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        gap: 1.4vw;
        color: #eee;
    }

    & .board,
    & .intro {
        display: flex;
        flex-direction: column;
        align-items: center;
    }
    & .board {
        gap: 1.4vw;
    }

    & .intro {
        gap: 2vw;

        & .coin {
            width: 11vw;
            height: 11vw;
        }

        // Takes its space from the start, nothing jumps when it shows
        & .caption {
            color: #111;
            padding: 0.6vw 1.8vw;
            border-radius: 0.5vw;
            font-size: 1.6vw;
            font-weight: 800;
            letter-spacing: 0.1vw;
            text-transform: uppercase;
            position: relative;
            overflow: hidden;
            opacity: 0;
            transform: translateY(0.5vw);
            transition: opacity 0.4s ease-out, transform 0.4s ease-out;

            &.shown {
                opacity: 1;
                transform: none;
            }

            & span {
                position: relative;
                z-index: 10;
            }
        }
    }

    & .idle-coin {
        width: 9vw;
        height: 9vw;
        animation: idle-bob 2.4s ease-in-out infinite;
    }

    & header {
        display: flex;
        align-items: center;
        gap: 1vw;
    }

    & header .toss {
        display: flex;
        align-items: center;
        gap: 0.6vw;
        padding: 0.4vw 1vw 0.4vw 0.5vw;
        border-radius: 0.5vw;
        background: rgba(0, 0, 0, 0.45);
        font-size: 1.1vw;
        font-weight: 700;

        & .coin {
            width: 2.4vw;
            height: 2.4vw;

            // The ring scales with the badge
            & :deep(.screen-coin) {
                box-shadow: 0 0 0 0.15vw var(--ring);
            }
        }
    }

    & header .turn {
        padding: 0.6vw 1.8vw;
        border-radius: 0.5vw;
        font-size: 1.5vw;
        font-weight: 800;
        letter-spacing: 0.1vw;
        text-transform: uppercase;
        position: relative;
        overflow: hidden;
        display: flex;
        align-items: center;
        gap: 0.75vw;
        background: rgba(255, 255, 255, 0.08);

        // Only tinted while someone has the turn
        & > .tint.themed {
            display: none;
        }
        &.themed {
            color: #111;

            & > .tint.themed {
                display: block;
            }
        }

        & span {
            position: relative;
            z-index: 10;
        }
    }

    & .cards {
        display: flex;
        flex-direction: column;
        gap: 1vw;

        & .row {
            display: flex;
            justify-content: center;
            gap: 1vw;
        }
    }

    & .card {
        width: var(--card);
        display: flex;
        flex-direction: column;
        gap: 0.2vw;
        transition: transform 0.4s ease-out;

        & .cover {
            width: var(--card);
            height: var(--card);
            border-radius: 0.5vw;
            position: relative;
            overflow: hidden;
            margin-bottom: 0.3vw;

            // Solid, so it darkens the same with or without a cover
            & .art {
                position: absolute;
                inset: 0;
                background-color: #333;
                background-size: cover;
                background-position: center;
                display: flex;
                align-items: center;
                justify-content: center;

                & > .mdi {
                    font-size: calc(var(--card) * 0.4);
                    opacity: 0.3;
                }
            }
        }

        & .label,
        & .difficulty {
            position: absolute;
            background: rgba(0, 0, 0, 0.75);
            border-radius: 0.3vw;
            padding: 0.15vw 0.45vw;
            font-size: calc(var(--card) * 0.09);
            font-weight: 800;
        }
        & .label {
            top: 0.4vw;
            left: 0.4vw;
        }
        & .difficulty {
            bottom: 0.4vw;
            right: 0.4vw;
            font-weight: 600;
        }

        & .tag {
            position: absolute;
            left: 0;
            right: 0;
            top: 50%;
            transform: translateY(-50%);
            padding: 0.4vw 0.3vw;
            display: flex;
            flex-direction: column;
            align-items: center;
            text-align: center;
            overflow: hidden;
            color: #111;
            font-weight: 800;
            font-size: calc(var(--card) * 0.11);
            text-transform: uppercase;
            letter-spacing: 0.05vw;

            & span {
                position: relative;
                z-index: 10;
            }
            & .by {
                font-size: 0.65em;
                font-weight: 700;
                text-transform: none;
                letter-spacing: 0;
                max-width: 100%;
                overflow: hidden;
                text-overflow: ellipsis;
                white-space: nowrap;
            }
        }

        & .title,
        & .artist {
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }
        & .title {
            font-weight: 700;
            font-size: calc(var(--card) * 0.1);
        }
        & .artist {
            font-size: calc(var(--card) * 0.085);
            opacity: 0.6;
        }

        // Dimmed, not faded, a see-through card would show the gameplay behind the board
        & .art,
        & .title,
        & .artist {
            transition: filter 0.4s ease-out, color 0.4s ease-out;
        }

        &.banned {
            & .art {
                filter: grayscale(1) brightness(0.35);
            }
            & .title,
            & .artist {
                color: #666;
            }

            & .tag {
                background: rgba(200, 40, 60, 0.95);
                color: #fff;
            }
        }
        &.played {
            & .art {
                filter: brightness(0.45);
            }
            & .title,
            & .artist {
                color: #888;
            }
        }
        &.next {
            transform: scale(1.06);

            & .cover {
                box-shadow: 0 0 0 0.25vw rgba(255, 255, 255, 0.9);
            }
        }
    }
}

// Shrinks up toward the header, where the small coin stays
.intro-leave-active {
    transition: opacity 0.45s ease-in, transform 0.45s ease-in;
}
.intro-leave-to {
    opacity: 0;
    transform: translateY(-8vw) scale(0.3);
}
.intro-enter-active {
    transition: opacity 0.4s ease-out;
}
.intro-enter-from {
    opacity: 0;
}

@keyframes idle-bob {
    0%,
    100% {
        transform: translateY(0);
    }
    50% {
        transform: translateY(-0.6vw);
    }
}
</style>
