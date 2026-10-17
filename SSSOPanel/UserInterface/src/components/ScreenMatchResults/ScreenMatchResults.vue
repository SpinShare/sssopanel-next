<template>
    <section
        class="screen-match-results"
        :class="{ recap: !!winnerName }"
    >
        <div class="backdrop">
            <div class="tint"></div>
            <div class="noise"></div>
        </div>

        <div class="content">
            <header
                v-if="winnerName"
                class="match-title"
            >
                <div class="tint themed"></div>
                <div class="noise"></div>
                <span class="mdi mdi-trophy"></span>
                <span>{{ winnerName }} takes the match!</span>
            </header>

            <!-- One slide per set once the match is won. Both slides share a grid cell, so they cross over in place -->
            <div class="slides">
                <transition :name="`slide-${direction}`">
                    <div
                        :key="shownSlide"
                        class="slide"
                    >
                        <div
                            v-for="side in sides"
                            :key="side.key"
                            class="player"
                            :class="[side.key, { won: side.won }]"
                        >
                            <header>
                                <template v-if="side.won">
                                    <div class="tint themed"></div>
                                    <div class="noise"></div>
                                    <span class="mdi mdi-trophy"></span>
                                    <span>WINNER &middot;</span>
                                </template>
                                <span>{{ side.player?.spinshareProfile?.username ?? side.entry?.name ?? 'Unknown' }}</span>
                            </header>
                            <div class="box">
                                <!-- Winner only, a tinted frame that joins up with the tinted header tab -->
                                <template v-if="side.won">
                                    <div class="frame">
                                        <div class="tint themed"></div>
                                        <div class="noise"></div>
                                    </div>
                                    <div class="fill"></div>
                                </template>
                                <template v-if="side.entry">
                                    <div class="headline">
                                        <span class="grade">{{ side.entry.grade }}</span>
                                        <div class="numbers">
                                            <span class="score">{{ side.entry.score.toLocaleString() }}</span>
                                            <span class="percent">{{ side.entry.percent.toFixed(2) }}%</span>
                                        </div>
                                    </div>
                                    <div class="tags">
                                        <span>{{ side.entry.difficulty }} {{ side.entry.difficultyRating }}</span>
                                        <span>{{ side.entry.maxCombo }}x Max Combo</span>
                                        <span v-if="fullComboLabel(side.entry.fullCombo)">{{ fullComboLabel(side.entry.fullCombo) }}</span>
                                        <span v-if="side.entry.failed">Failed</span>
                                        <span v-if="side.entry.forfeited">Forfeited</span>
                                    </div>
                                    <div class="judgements">
                                        <div
                                            v-for="judgement in judgements"
                                            :key="judgement.key"
                                            class="judgement"
                                        >
                                            <span class="label">{{ judgement.label }}</span>
                                            <!-- Kept for misses too so the counts line up -->
                                            <span class="early">{{ judgement.early && !judgement.noSplit ? `${count(side.entry, judgement.early)} early · ${count(side.entry, judgement.key)} late` : '' }}</span>
                                            <span class="count">{{ count(side.entry, judgement.key) + (judgement.early ? count(side.entry, judgement.early) : 0) }}</span>
                                        </div>
                                    </div>
                                </template>
                                <div
                                    v-else
                                    class="missing"
                                >
                                    Did not finish
                                </div>
                            </div>
                        </div>
                    </div>
                </transition>
            </div>

            <footer
                v-if="winnerName && history.length > 1"
                class="carousel"
            >
                <span class="label"
                    >Set {{ shownSlide + 1 }}<template v-if="history[shownSlide]?.chart?.title"> &middot; {{ history[shownSlide].chart.title }}</template></span
                >
                <div class="dots">
                    <span
                        v-for="(_, index) in history"
                        :key="index"
                        :class="{ active: index === shownSlide }"
                    ></span>
                </div>
            </footer>
        </div>
    </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue';
import { player1Won as isPlayer1Winner } from '@/modules/useSrxd';

const props = defineProps({
    results: {
        type: Object,
        required: true,
    },
    player1: {
        type: Object,
        default: null,
    },
    player2: {
        type: Object,
        default: null,
    },
    // Set when the match is won, turns the results into a recap carousel of history
    winnerName: {
        type: String,
        default: null,
    },
    // [{ chart: { title, artist }, results }] per counted set
    history: {
        type: Array,
        default: () => [],
    },
    slide: {
        type: Number,
        default: 0,
    },
});

const judgements = [
    // Never early or late so far, the total still includes the early key in case the game ever sends one
    { key: 'perfectPlus', early: 'earlyPerfectPlus', label: 'Perfect+', noSplit: true },
    { key: 'perfect', early: 'earlyPerfect', label: 'Perfect' },
    { key: 'great', early: 'earlyGreat', label: 'Great' },
    { key: 'good', early: 'earlyGood', label: 'Good' },
    { key: 'okay', early: 'earlyOkay', label: 'Okay' },
    { key: 'failed', early: null, label: 'Miss' },
];

// The plain counts are the late hits only, early ones are counted separately (early can be higher than the plain count)
const count = (entry, key) => entry.judgements?.[key] ?? 0;

// Named after the lowest judgement the player got, none means no full combo
const fullComboLabels = {
    okay: 'Full Combo (Okay)',
    good: 'Full Combo (Good)',
    great: 'Full Combo (Great)',
    perfect: 'Full Combo (Perfect)',
    perfectPlus: 'Full Combo (Perfect+)',
};
const fullComboLabel = (fullCombo) => (fullCombo && fullCombo !== 'none' ? fullComboLabels[fullCombo] ?? fullCombo : null);

const shownSlide = computed(() => (props.winnerName && props.history.length ? Math.min(props.history.length - 1, Math.max(0, props.slide)) : 0));
const shownResults = computed(() => (props.winnerName && props.history.length ? props.history[shownSlide.value].results : props.results));

// Swipes the way the carousel moves, stepping back slides the other way
const direction = ref('next');
watch(shownSlide, (value, oldValue) => {
    direction.value = value < oldValue ? 'prev' : 'next';
});

const sides = computed(() => {
    const entry1 = shownResults.value?.player1;
    const entry2 = shownResults.value?.player2;
    const player1Won = isPlayer1Winner(entry1, entry2);
    const player2Won = !!entry2 && !player1Won;

    return [
        { key: 'left', player: props.player1, entry: entry1, won: player1Won },
        { key: 'right', player: props.player2, entry: entry2, won: player2Won },
    ];
});
</script>

<style lang="scss" scoped>
// Dims the gameplay cutout with the same grainy background as the bands around it,
// the game stays faintly visible behind the cards
.screen-match-results {
    position: absolute;
    inset: 0;
    overflow: hidden;

    & .backdrop {
        position: absolute;
        inset: 0;
        opacity: 0.75;
    }

    & .tint {
        background: url('../../assets/background.svg');
        background-size: cover;
        position: absolute;
        inset: 0;
        filter: contrast(1) brightness(0.125) grayscale(2);
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
        gap: 0.8vw;
        color: #eee;

        // Same tinted look as the winner's header tab, just bigger
        & .match-title {
            color: #111;
            padding: 0.6vw 1.8vw;
            border-radius: 0.5vw;
            font-size: 1.6vw;
            font-weight: 800;
            letter-spacing: 0.1vw;
            position: relative;
            overflow: hidden;
            display: flex;
            align-items: center;
            gap: 0.75vw;

            & .tint {
                filter: var(--themeFilter);
            }

            & span {
                position: relative;
                z-index: 10;
            }
        }

        & .slides {
            display: grid;
        }

        & .slide {
            grid-area: 1 / 1;
            display: flex;
            justify-content: center;
            gap: 6vw;
        }

        & .carousel {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 0.5vw;

            & .label {
                font-size: 0.8vw;
                font-weight: 600;
                letter-spacing: 0.1vw;
                color: rgba(255, 255, 255, 0.6);
            }

            & .dots {
                display: flex;
                gap: 0.4vw;

                & span {
                    width: 0.5vw;
                    height: 0.5vw;
                    border-radius: 0.25vw;
                    background: rgba(255, 255, 255, 0.3);
                    transition: width 0.4s ease, background 0.4s ease;

                    &.active {
                        width: 1.5vw;
                        background: #ededed;
                    }
                }
            }
        }

        & .player {
            width: 32vw;
            display: flex;
            flex-direction: column;
            align-items: flex-start;

            &.right {
                align-items: flex-end;

                & .box {
                    border-radius: 0.5vw 0 0.5vw 0.5vw;
                }
            }

            & header {
                background: #111;
                color: #ededed;
                padding: 0.8vw 1.5vw;
                border-radius: 0.5vw 0.5vw 0 0;
                font-weight: 600;
                font-size: 0.9vw;
                letter-spacing: 0.15vw;
                position: relative;
                overflow: hidden;
                display: flex;
                align-items: center;
                gap: 0.5vw;

                & span {
                    position: relative;
                    z-index: 10;
                }

                & .mdi {
                    font-size: 1.1vw;
                    letter-spacing: 0;
                }
            }

            // Same tint as the sets box and the spinshare banner, dark text on top
            &.won header {
                background: transparent;
                color: #111;
            }

            & .tint {
                background: url('../../assets/background.svg');
                background-size: cover;
                position: absolute;
                inset: 0;
                filter: var(--themeFilter);
            }

            & .noise {
                z-index: 1;
            }

            & .box {
                align-self: stretch;
                display: flex;
                flex-direction: column;
                gap: 1vw;
                background: #111;
                color: #ededed;
                padding: 1.5vw;
                border-radius: 0 0.5vw 0.5vw 0.5vw;
                position: relative;
                overflow: hidden;

                & > *:not(.frame):not(.fill) {
                    position: relative;
                    z-index: 10;
                }

                & .frame {
                    position: absolute;
                    inset: 0;
                }

                & .fill {
                    position: absolute;
                    inset: 0.25vw;
                    background: #111;
                    border-radius: 0.3vw;
                    z-index: 5;
                }

                & .headline {
                    display: flex;
                    align-items: center;
                    gap: 1.5vw;

                    & .grade {
                        font-size: 3.5vw;
                        font-weight: 900;
                        min-width: 5vw;
                    }

                    & .numbers {
                        display: flex;
                        flex-direction: column;
                        gap: 0.25vw;

                        & .score {
                            font-size: 1.75vw;
                            font-weight: 600;
                            letter-spacing: 0.1vw;
                        }

                        & .percent {
                            color: rgba(255, 255, 255, 0.6);
                            font-size: 1vw;
                            letter-spacing: 0.1vw;
                        }
                    }
                }

                & .tags {
                    display: flex;
                    flex-wrap: wrap;
                    gap: 0.5vw;

                    & span {
                        font-size: 0.75vw;
                        font-weight: 600;
                        letter-spacing: 0.1vw;
                        background: rgba(255, 255, 255, 0.1);
                        padding: 0.4vw 0.6vw;
                        border-radius: 0.25vw;
                    }
                }

                & .judgements {
                    display: grid;
                    gap: 0.4vw;

                    & .judgement {
                        display: grid;
                        grid-template-columns: 1fr auto 4vw;
                        gap: 1vw;
                        align-items: baseline;
                        font-size: 0.9vw;

                        & .label {
                            font-weight: 600;
                            letter-spacing: 0.1vw;
                        }

                        & .early {
                            color: rgba(255, 255, 255, 0.4);
                            font-size: 0.75vw;
                        }

                        & .count {
                            text-align: right;
                            font-weight: 600;
                        }
                    }
                }

                & .missing {
                    color: rgba(255, 255, 255, 0.6);
                    font-size: 1.15vw;
                    padding: 2vw 0;
                }
            }
        }
    }

    // Title and dots sit halfway between the cards and the band edges
    &.recap .content {
        justify-content: space-evenly;
        gap: 0;
    }

    // The title and dots need room in the 540px band, so the recap cards are a bit tighter
    &.recap .content .player .box {
        padding: 1.1vw 1.5vw;
        gap: 0.6vw;

        & .headline .grade {
            font-size: 2.6vw;
        }

        & .judgements {
            gap: 0.15vw;
        }
    }
}

// Swipe and fade to the side, the old slide leaves the way the new one came from
.slide-next-enter-active,
.slide-next-leave-active,
.slide-prev-enter-active,
.slide-prev-leave-active {
    transition: transform 0.6s ease, opacity 0.6s ease;
}
.slide-next-enter-from,
.slide-prev-leave-to {
    opacity: 0;
    transform: translateX(8vw);
}
.slide-next-leave-to,
.slide-prev-enter-from {
    opacity: 0;
    transform: translateX(-8vw);
}
</style>
