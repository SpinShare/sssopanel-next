<template>
    <section
        class="screen-match-results"
        :class="{ recap: !!winnerName, plain: !backdrop }"
    >
        <div
            v-if="backdrop"
            class="backdrop"
        >
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
                    <!-- Keyed on recap too, so going from the deciding set to set 1 swipes instead of cutting -->
                    <div
                        :key="`${winnerName ? 'recap' : 'set'}-${shownSlide}`"
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
                                <!-- Winner only, the tinted rim around the card -->
                                <div
                                    v-if="side.won"
                                    class="frame"
                                >
                                    <div class="tint themed"></div>
                                    <div class="noise"></div>
                                </div>
                                <div
                                    v-if="side.entry"
                                    class="inner"
                                >
                                    <div class="headline">
                                        <div
                                            v-if="avatars && side.player?.spinshareProfile?.avatar"
                                            class="avatar"
                                            :style="`background-image: url(${side.player.spinshareProfile.avatar})`"
                                        ></div>
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
                                        <span v-if="side.entry.attempts > 1">Best of {{ side.entry.attempts }} attempts</span>
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
                                </div>
                                <div
                                    v-else
                                    class="inner missing"
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
    // Off on a full screen page, the dark backdrop is only there to dim the gameplay cutout
    backdrop: {
        type: Boolean,
        default: true,
    },
    avatars: {
        type: Boolean,
        default: false,
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
                background-image: var(--themeBackground);
                filter: none;
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
            // Rim width of the winner's tinted frame, 0 for the other card
            --rim: 0vw;
            width: 32vw;
            display: flex;
            flex-direction: column;
            align-items: flex-start;
            position: relative;

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

            // Same tint as the sets box and the spinshare banner, dark text on top.
            // The tab and the rim share one tint the size of the whole card, positioned against .player, so the
            // gradient and grain run through where they meet. header and box only cut it out with clip-path,
            // which (unlike overflow) also clips descendants positioned against an ancestor
            &.won {
                --rim: 0.25vw;

                & header,
                & .box {
                    position: static;
                    background: transparent;
                }

                & header {
                    color: #111;
                    // A pixel into the box, so no antialiased seam shows between the two clips
                    clip-path: inset(0 0 -1px 0 round 0.5vw 0.5vw 0 0);
                }

                & .box {
                    clip-path: inset(0 round 0 0.5vw 0.5vw 0.5vw);
                }

                &.right .box {
                    clip-path: inset(0 round 0.5vw 0 0.5vw 0.5vw);
                }
            }

            & .tint {
                background: var(--themeBackground);
                background-size: cover;
                position: absolute;
                inset: 0;
                filter: none;
            }

            & .noise {
                z-index: 1;
            }

            & .box {
                align-self: stretch;
                display: flex;
                flex-direction: column;
                background: #111;
                color: #ededed;
                padding: var(--rim);
                border-radius: 0 0.5vw 0.5vw 0.5vw;
                position: relative;
                overflow: hidden;

                & .frame {
                    position: absolute;
                    inset: 0;
                }

                & .inner {
                    display: flex;
                    flex-direction: column;
                    gap: 1vw;
                    // Content stays put whether the rim is there or not
                    padding: calc(1.5vw - var(--rim));
                    background: #111;
                    border-radius: 0.3vw;
                    position: relative;
                    z-index: 10;
                }

                & .headline {
                    display: flex;
                    align-items: center;
                    gap: 1.5vw;

                    & .avatar {
                        flex-shrink: 0;
                        width: 6vw;
                        height: 6vw;
                        border-radius: 0.75vw;
                        background-position: center;
                        background-size: cover;
                    }

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
                    padding: calc(3.5vw - var(--rim)) calc(1.5vw - var(--rim));
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
    &.recap:not(.plain) .content .player .inner:not(.missing) {
        padding: calc(1.1vw - var(--rim)) calc(1.5vw - var(--rim));
        gap: 0.6vw;

        & .headline .grade {
            font-size: 2.6vw;
        }

        & .judgements {
            gap: 0.15vw;
        }
    }
}

// Straight on the themed background, so the loose text goes dark and the title becomes a dark tab like the cards
.screen-match-results.plain .content {
    & .match-title {
        background: #111;
        color: #ededed;
        font-size: 2vw;
        padding: 0.8vw 2.2vw;

        & .tint,
        & .noise {
            display: none;
        }
    }

    & .carousel {
        & .label {
            color: rgba(0, 0, 0, 0.7);
            font-size: 1vw;
        }

        & .dots span {
            background: rgba(0, 0, 0, 0.25);

            &.active {
                background: #111;
            }
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
