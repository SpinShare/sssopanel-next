<template>
    <ScreenLayout transparent>
        <section class="screen-match-ingame">
            <div class="tint"></div>
            <div class="noise"></div>

            <div class="content">
                <section class="match-info">
                    <div
                        class="player left"
                        v-show="player1"
                    >
                        <div
                            v-show="!!player1"
                            class="avatar"
                            :style="`background-image: url(${player1?.spinshareProfile?.avatar})`"
                        ></div>
                        <div class="meta">
                            <div
                                v-if="player1?.spinshareProfile?.pronouns"
                                class="pronouns"
                            >
                                {{ player1?.spinshareProfile?.pronouns }}
                            </div>
                            <div class="username">
                                {{ player1?.spinshareProfile?.username ?? player1?.name }}
                            </div>
                            <div
                                v-if="playerDifficulty1"
                                class="difficulty"
                            >
                                {{ playerDifficulty1 }}
                            </div>
                        </div>
                    </div>
                    <transition name="tug">
                        <ScreenStatsBox
                            v-if="tug"
                            class="side-stats left"
                            side="left"
                            :stats="tug.sides[0]"
                            :low-health="TUG_LOW_HEALTH"
                        />
                    </transition>
                    <div class="stats">
                        <div class="tint themed"></div>
                        <div class="noise"></div>

                        <div class="sets">
                            <transition
                                name="promo"
                                mode="out-in"
                            >
                                <span
                                    class="current"
                                    :key="scoresSetsCurrent"
                                >
                                    {{ scoresSetsCurrent }}
                                </span>
                            </transition>
                            <span class="of">OF</span>
                            <span class="max">{{ scoresSetsMax }}</span>
                        </div>
                        <div class="score">
                            <div class="player">
                                <span
                                    v-for="n in Math.ceil(scoresSetsMax / 2)"
                                    :key="'p1-' + n"
                                    class="point"
                                    :class="{ active: n < scoresScorePlayer1 + 1 }"
                                ></span>
                            </div>
                            <div class="player">
                                <span
                                    v-for="n in Math.ceil(scoresSetsMax / 2)"
                                    :key="'p2-' + n"
                                    class="point"
                                    :class="{ active: n < scoresScorePlayer2 + 1 }"
                                ></span>
                            </div>
                        </div>
                    </div>
                    <transition name="tug">
                        <ScreenStatsBox
                            v-if="tug"
                            class="side-stats right"
                            side="right"
                            :stats="tug.sides[1]"
                            :low-health="TUG_LOW_HEALTH"
                        />
                    </transition>
                    <div
                        class="player right"
                        v-show="!!player2"
                    >
                        <div class="meta">
                            <div
                                v-if="player2?.spinshareProfile?.pronouns"
                                class="pronouns"
                            >
                                {{ player2?.spinshareProfile?.pronouns }}
                            </div>
                            <div class="username">
                                {{ player2?.spinshareProfile?.username ?? player2?.name }}
                            </div>
                            <div
                                v-if="playerDifficulty2"
                                class="difficulty"
                            >
                                {{ playerDifficulty2 }}
                            </div>
                        </div>
                        <div
                            class="avatar"
                            :style="`background-image: url(${player2?.spinshareProfile?.avatar})`"
                        ></div>
                    </div>
                    <!-- Grows and shrinks instead of popping in, so the row above slides instead of jumping -->
                    <div
                        class="tug-row"
                        :class="{ shown: !!tug }"
                    >
                        <div
                            class="tug"
                            v-if="shownTug"
                        >
                            <div class="tug-bar">
                                <div class="tint muted"></div>
                                <div class="noise"></div>
                                <div
                                    class="tug-fill left"
                                    :style="{ width: Math.max(0, shownLead) * 50 + '%' }"
                                >
                                    <div class="tint themed"></div>
                                    <div class="noise"></div>
                                </div>
                                <div
                                    class="tug-fill right"
                                    :style="{ width: Math.max(0, -shownLead) * 50 + '%' }"
                                >
                                    <div class="tint themed"></div>
                                    <div class="noise"></div>
                                </div>
                                <!-- Keyed by side, so a lead change fades out on one side and in on the other -->
                                <transition name="lead">
                                    <span
                                        v-if="leadSide"
                                        :key="leadSide"
                                        class="tug-lead"
                                        :class="leadSide"
                                    >
                                        +<ScreenRollingNumber :value="Math.abs(shownTug.diff).toLocaleString()" />
                                    </span>
                                </transition>
                            </div>
                        </div>
                    </div>
                </section>
                <section class="gameplay">
                    <transition name="results">
                        <ScreenMatchResults
                            v-if="resultsVisible && results"
                            :results="results"
                            :player1="player1"
                            :player2="player2"
                        />
                    </transition>
                </section>
                <section
                    class="chart-info"
                    v-if="!!chart || refDenIsVisible"
                >
                    <div
                        v-if="chart"
                        class="chart"
                    >
                        <div
                            v-if="chart.paths?.cover"
                            class="cover"
                            :style="`background-image: url('${chart.paths.cover}')`"
                        ></div>
                        <div
                            v-else
                            class="cover placeholder"
                        >
                            <span class="mdi mdi-music"></span>
                        </div>
                        <div class="meta">
                            <!-- Separate spans, an ellipsis takes the style of the element it cuts off -->
                            <div class="title">
                                <span class="name">{{ chart.title }}</span>
                                <span
                                    v-if="chartMeta.subtitle"
                                    class="subtitle"
                                    >{{ chartMeta.subtitle }}</span
                                >
                            </div>
                            <span class="artist"
                                >{{ chart.artist }}<template v-if="chartMeta.featArtists"> feat. {{ chartMeta.featArtists }}</template></span
                            >
                            <span
                                class="charter"
                                v-if="chart.charter"
                                >Charter: {{ chart.charter }}</span
                            >
                            <div
                                class="tags"
                                v-if="chartMeta.tags.length || chartMeta.length"
                            >
                                <span
                                    v-for="tag in chartMeta.tags"
                                    :key="tag"
                                    >{{ tag }}</span
                                >
                                <span
                                    v-if="chartMeta.length"
                                    class="time"
                                >
                                    <template v-if="chartMeta.time !== null"><ScreenRollingNumber :value="chartMeta.time" /> / </template>{{ chartMeta.length }}
                                </span>
                            </div>
                            <div
                                class="progress"
                                v-if="chartMeta.progress !== null"
                            >
                                <div
                                    class="progress-fill"
                                    :style="{ width: chartMeta.progress * 100 + '%' }"
                                >
                                    <div class="tint themed"></div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- The ref den takes the banner's place while it's toggled on -->
                    <div class="side">
                        <transition name="side">
                            <div
                                class="banner"
                                v-if="chart?.id && !refDenIsVisible"
                            >
                                <div class="tint themed"></div>
                                <div class="noise"></div>

                                <div class="text">
                                    <span>Get this chart on SpinSha.re!</span>
                                    <span>spinsha.re/song/{{ chart.id }}</span>
                                </div>
                                <span class="mdi mdi-archive-music-outline"></span>
                            </div>
                        </transition>
                        <ScreenRefDen variant="inline" />
                    </div>
                </section>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import { computed, reactive, ref, inject, onMounted, onUnmounted, watch } from 'vue';
import useSrxd, { stripRichText } from '@/modules/useSrxd';
import ScreenRollingNumber from '@/components/ScreenRollingNumber/ScreenRollingNumber.vue';
import ScreenMatchResults from '@/components/ScreenMatchResults/ScreenMatchResults.vue';
import ScreenStatsBox from '@/components/ScreenStatsBox/ScreenStatsBox.vue';
import ScreenRefDen from '@/components/ScreenRefDen/ScreenRefDen.vue';
const emitter = inject('emitter');
const game = useSrxd();

// Bars follow the point lead on a log scale, so small early gaps stay small and big leads don't
// flatten out the rest. TUG_SOFTNESS is roughly where the curve bends (higher is calmer).
const TUG_FULL_LEAD = 2000;
const TUG_SOFTNESS = 500;
// Health ring pulses below this
const TUG_LOW_HEALTH = 0.3;

const player1 = ref(null);
const player2 = ref(null);
const chart = ref(null);
const scoresSetsCurrent = ref(1);
const scoresSetsMax = ref(3);
const scoresScorePlayer1 = ref(0);
const scoresScorePlayer2 = ref(0);
const ingame = ref({ player1: '', player2: '' });
const results = ref(null);
const resultsVisible = ref(false);
const refDenIsVisible = ref(false);

// Until the first scores of a new song arrive, the last round's are still around
const liveScores = computed(() => {
    const scores = game.value.scores;
    if (game.value.phase !== 'playing' || !scores || scores.round !== game.value.round) return null;
    return scores;
});

// Live full combo per player name. The game sends no judgements during a song, so a combo drop or any
// health loss (only misses cost health) counts as the FC being gone. The exact tier is only in the results.
const fullCombos = reactive({});
watch(liveScores, (scores) => {
    for (const lane of scores?.lanes ?? []) {
        let tracked = fullCombos[lane.name];
        if (!tracked || tracked.round !== scores.round) {
            tracked = fullCombos[lane.name] = { round: scores.round, combo: 0, health: 1, fullCombo: true };
        }

        if (lane.failed || lane.combo < tracked.combo || lane.health < tracked.health) tracked.fullCombo = false;
        tracked.combo = lane.combo;
        tracked.health = lane.health;
    }
});

const tug = computed(() => {
    if (!liveScores.value || !ingame.value.player1 || !ingame.value.player2) return null;

    const lanes = liveScores.value.lanes ?? [];
    const lane1 = lanes.find((lane) => lane.name === ingame.value.player1);
    const lane2 = lanes.find((lane) => lane.name === ingame.value.player2);
    if (!lane1 || !lane2) return null;

    const diff = lane1.score - lane2.score;
    const lead = Math.min(1, Math.log1p(Math.abs(diff) / TUG_SOFTNESS) / Math.log1p(TUG_FULL_LEAD / TUG_SOFTNESS));

    return {
        score1: lane1.score,
        score2: lane2.score,
        diff,
        // Each bar covers half of the track at most
        // Positive is player 1 ahead, negative player 2
        signedLead: diff > 0 ? lead : -lead,
        sides: [
            { key: 'left', lane: lane1 },
            { key: 'right', lane: lane2 },
        ].map(({ key, lane }) => ({
            key,
            score: lane.score.toLocaleString(),
            combo: lane.combo.toLocaleString(),
            multiplier: lane.multiplier,
            accuracy: lane.accuracy.toFixed(2),
            failed: lane.failed,
            fullCombo: fullCombos[lane.name]?.round === liveScores.value.round && fullCombos[lane.name].fullCombo,
            health: lane.failed ? 0 : Math.min(1, Math.max(0, lane.health)),
        })),
    };
});

// Last bar data, kept on screen while the row slides closed
const shownTug = ref(null);
watch(tug, (value) => {
    if (value) shownTug.value = value;
});

// Side of the lead pill. A tie keeps it up for a bit (rolling to +0) instead of blinking out
const TUG_TIE_LINGER = 3000;
const leadSide = ref(null);
let leadTimer = null;
watch(
    () => tug.value?.diff ?? 0,
    (diff) => {
        if (diff !== 0) {
            clearTimeout(leadTimer);
            leadTimer = null;
            leadSide.value = diff > 0 ? 'left' : 'right';
        } else if (leadSide.value && !leadTimer) {
            leadTimer = setTimeout(() => {
                leadTimer = null;
                leadSide.value = null;
            }, TUG_TIE_LINGER);
        }
    },
);
// A new song starts tied, the old side shouldn't show up as +0
watch(
    () => game.value.round,
    () => {
        clearTimeout(leadTimer);
        leadTimer = null;
        leadSide.value = null;
    },
);
onUnmounted(() => clearTimeout(leadTimer));

// One animated value for both fills. It slides through the middle when the lead changes hands, so the two
// sides never grow and shrink at the same time (which looked like both were ahead for a moment)
const TUG_EASE = 0.1; // seconds, how quickly the bar catches up
const shownLead = ref(0);
let tugFrame = null;
let tugFrameTime = 0;
const animateTug = (time) => {
    const target = tug.value?.signedLead ?? 0;
    const dt = tugFrameTime ? (time - tugFrameTime) / 1000 : 0;
    tugFrameTime = time;

    shownLead.value += (target - shownLead.value) * (1 - Math.exp(-dt / TUG_EASE));
    if (Math.abs(target - shownLead.value) < 0.0005) shownLead.value = target;

    tugFrame = shownLead.value === target ? null : requestAnimationFrame(animateTug);
};
watch(
    () => tug.value?.signedLead ?? 0,
    () => {
        if (tugFrame) return;
        tugFrameTime = 0;
        tugFrame = requestAnimationFrame(animateTug);
    },
);
onUnmounted(() => tugFrame && cancelAnimationFrame(tugFrame));

const formatTime = (seconds) => `${Math.floor(seconds / 60)}:${String(Math.floor(seconds % 60)).padStart(2, '0')}`;

// Only trust the game's track when it is the chart on screen, the chart can also be picked by hand
const track = computed(() => {
    const gameTrack = game.value.track;
    if (!gameTrack || !chart.value?.fileReference || gameTrack.fileReference !== chart.value.fileReference) return null;
    return gameTrack;
});

const chartMeta = computed(() => {
    const tags = [];
    const scores = track.value ? liveScores.value : null;

    // Unlocked rooms show each player's difficulty instead, the track only has the local client's pick
    if (track.value?.difficulty && game.value.room?.lockDifficulty !== false) tags.push(`${track.value.difficulty}${track.value.difficultyRating ? ' ' + track.value.difficultyRating : ''}`);

    const bpmLow = Math.round(track.value?.bpmLow ?? 0);
    const bpmHigh = Math.round(track.value?.bpmHigh ?? 0);
    if (bpmHigh) tags.push(bpmLow && bpmLow !== bpmHigh ? `${bpmLow}-${bpmHigh} BPM` : `${bpmHigh} BPM`);

    const length = scores?.songLength ?? track.value?.duration;

    return {
        subtitle: stripRichText(track.value?.subtitle ?? chart.value?.subtitle),
        featArtists: stripRichText(track.value?.featArtists),
        tags,
        // Separate from the tags so the time can roll like the other live numbers
        length: length ? formatTime(length) : null,
        time: scores && length ? formatTime(scores.songTime) : null,
        progress: scores ? Math.min(1, Math.max(0, scores.progress)) : null,
    };
});

// Only worth showing when players can pick different difficulties
const playerDifficulty = (name) => {
    if (!name || game.value.room?.lockDifficulty !== false) return null;

    const lane = liveScores.value?.lanes?.find((x) => x.name === name);
    const member = lane ?? game.value.room.players?.find((x) => x.name === name);
    if (!member?.difficulty) return null;

    return `${member.difficulty}${member.difficultyRating ? ' ' + member.difficultyRating : ''}`;
};
const playerDifficulty1 = computed(() => playerDifficulty(ingame.value.player1));
const playerDifficulty2 = computed(() => playerDifficulty(ingame.value.player2));

const applyState = (state) => {
    player1.value = state?.currentMatch?.players?.player1 ?? player1.value;
    player2.value = state?.currentMatch?.players?.player2 ?? player2.value;
    chart.value = state?.currentMatch?.chart ?? chart.value;
    ingame.value = state?.currentMatch?.ingame ?? ingame.value;
    results.value = state?.currentMatch?.results ?? results.value;
    resultsVisible.value = state?.currentMatch?.resultsVisible ?? resultsVisible.value;
    refDenIsVisible.value = state?.refDenIsVisible ?? false;

    scoresSetsCurrent.value = state?.currentMatch?.scores?.sets?.current ?? scoresSetsCurrent.value;
    scoresSetsMax.value = state?.currentMatch?.scores?.sets?.max ?? scoresSetsMax.value;
    scoresScorePlayer1.value = state?.currentMatch?.scores?.score?.player1 ?? scoresScorePlayer1.value;
    scoresScorePlayer2.value = state?.currentMatch?.scores?.score?.player2 ?? scoresScorePlayer2.value;
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        applyState(state);
    });
    emitter.on('state-set-response', (state) => {
        applyState(state);
    });

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-get',
        }),
    );
});
onUnmounted(() => {
    emitter.off('state-get-response');
    emitter.off('state-set-response');
});
</script>

<style lang="scss" scoped>
// Gameplay is layered below the overlay in OBS, 1920x540 at 1080p
$gameplayHeight: 28.125vw;
$gameplayMask: linear-gradient(to bottom, #000 calc(50% - #{$gameplayHeight * 0.5}), transparent calc(50% - #{$gameplayHeight * 0.5}), transparent calc(50% + #{$gameplayHeight * 0.5}), #000 calc(50% + #{$gameplayHeight * 0.5}));

.screen-match-ingame {
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
        filter: contrast(1) brightness(0.125) grayscale(2);
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5em;
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        top: 0;
        opacity: 0.4;
        mix-blend-mode: soft-light;
    }

    // Only the full size background gets the cutout, not the nested tints
    & > .tint,
    & > .noise {
        -webkit-mask-image: $gameplayMask;
        mask-image: $gameplayMask;
    }

    & .content {
        display: grid;
        grid-template-rows: 1fr auto 1fr;
        align-items: center;
        position: absolute;
        top: 0;
        bottom: 0;
        left: 0;
        right: 0;
        font-size: 1vw;
        z-index: 10;
        color: #eee;

        // player, stats, sets, stats, player. Columns are fixed so the stats boxes can come and go.
        // Stats columns are as wide as ScreenStatsBox, the two boxes leave on different frames and an auto column would shift the sets box
        & .match-info {
            display: grid;
            grid-template-columns: 1fr 12.5vw auto 12.5vw 1fr;
            // No row gap, the tug row brings its own spacing so it can collapse fully
            column-gap: 2vw;
            padding: 0 4vw;

            & .player {
                display: flex;
                align-items: center;
                gap: 1.5vw;

                & .avatar {
                    background-position: center;
                    background-size: cover;
                    width: 6vw;
                    height: 6vw;
                    border-radius: 100%;
                }

                & .meta {
                    display: flex;
                    flex-direction: column;
                    gap: 0.5vw;

                    & .pronouns {
                        color: rgba(255, 255, 255, 0.6);
                        font-size: 1.25vw;
                        font-weight: 600;
                    }

                    & .username {
                        font-weight: 600;
                        font-size: 1.75vw;
                        letter-spacing: 0.1vw;
                    }

                    & .difficulty {
                        align-self: flex-start;
                        font-size: 0.8vw;
                        font-weight: 600;
                        letter-spacing: 0.1vw;
                        background: rgba(255, 255, 255, 0.1);
                        padding: 0.4vw 0.6vw;
                        border-radius: 0.25vw;
                    }
                }

                &.left {
                    grid-column: 1;
                }

                &.right {
                    grid-column: 5;
                    justify-content: flex-end;

                    & .meta {
                        text-align: right;

                        & .difficulty {
                            align-self: flex-end;
                        }
                    }
                }
            }

            & .side-stats.left {
                grid-column: 2;
            }

            & .side-stats.right {
                grid-column: 4;
            }

            & .stats {
                grid-column: 3;
                background: #111;
                padding: 1.25vw 2vw;
                border-radius: 0.5vw;
                display: flex;
                flex-direction: column;
                gap: 1vw;
                align-items: center;
                position: relative;
                overflow: hidden;

                & .tint {
                    background: url('../../assets/background.svg');
                    background-size: cover;
                    position: absolute;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    top: 0;
                    z-index: 0;

                    &.themed {
                        background-image: var(--themeBackground);
                        filter: none;
                    }
                }

                & .sets {
                    position: relative;
                    z-index: 10;
                    color: #111;
                    display: flex;
                    align-items: center;
                    gap: 0.5vw;

                    & .current,
                    & .max {
                        width: 2.5vw;
                        font-size: 2vw;
                        font-weight: bold;
                        text-align: center;
                    }

                    & .of {
                        font-weight: bold;
                        font-size: 1.15vw;
                        letter-spacing: 0.25vw;
                        background: #111;
                        color: #ededed;
                        padding: 0.5vw 0.5vw;
                        border-radius: 0.25vw;
                    }
                }

                & .score {
                    display: flex;
                    gap: 2vw;
                    justify-items: center;
                    align-items: center;
                    z-index: 10;

                    & .player {
                        display: flex;
                        gap: 0.5vw;

                        & .point {
                            border: 0.125vw solid rgba(0, 0, 0, 0.4);
                            width: 1vw;
                            height: 1vw;
                            border-radius: 0.25vw;

                            &.active {
                                background: #111;
                            }
                        }
                    }
                }
            }
        }

        & .tug-row {
            grid-column: 1 / -1;
            display: grid;
            grid-template-rows: 0fr;
            opacity: 0;
            transition: grid-template-rows 0.5s ease-in-out, opacity 0.5s ease-in-out;

            &.shown {
                grid-template-rows: 1fr;
                opacity: 1;
            }

            & > .tug {
                overflow: hidden;
                min-height: 0;
            }
        }

        & .tug {
            display: grid;
            align-items: center;

            // Muted version of the grainy background, a step lighter than the band behind it
            & .tint.muted {
                filter: contrast(1) brightness(0.22) grayscale(2);
            }

            & .tug-bar {
                margin-top: 1vw;
                height: 2vw;
                border-radius: 0.5vw;
                position: relative;
                overflow: hidden;

                // Middle line both bars grow out from
                &::after {
                    content: '';
                    position: absolute;
                    left: calc(50% - 0.0625vw);
                    top: 0;
                    bottom: 0;
                    width: 0.125vw;
                    background: rgba(255, 255, 255, 0.4);
                    z-index: 20;
                }

                & .tug-fill {
                    position: absolute;
                    top: 0;
                    bottom: 0;
                    overflow: hidden;

                    &.left {
                        right: 50%;
                    }

                    &.right {
                        left: 50%;
                    }

                    & .tint {
                        background-image: var(--themeBackground);
                        filter: none;
                    }
                }

                // Next to the middle line on the leading side, a dark pill like the "OF" in the sets box
                // so it reads the same on the fill, the track and every theme
                & .tug-lead {
                    position: absolute;
                    top: 50%;
                    transform: translateY(-50%);
                    display: flex;
                    align-items: center;
                    font-size: 0.8vw;
                    font-weight: 600;
                    letter-spacing: 0.1vw;
                    --digit-gap: 0.1vw;
                    background: #111;
                    color: #ededed;
                    padding: 0.2vw 0.5vw;
                    border-radius: 0.25vw;
                    z-index: 10;
                    white-space: nowrap;

                    &.left {
                        right: calc(50% + 0.5vw);
                    }

                    &.right {
                        left: calc(50% + 0.5vw);
                    }
                }
            }
        }

        & .gameplay {
            width: 100%;
            height: $gameplayHeight;
            position: relative;
        }

        & .chart-info {
            display: grid;
            grid-template-columns: 1fr auto;
            gap: 2vw;
            padding: 0 4vw;
            align-items: center;

            & .chart {
                display: flex;
                gap: 2vw;
                align-items: center;

                .cover {
                    width: 8.5vw;
                    height: 8.5vw;
                    border-radius: 1vw;
                    flex-shrink: 0;
                    background-position: center;
                    background-size: cover;

                    // Base game charts have no cover we can get to
                    &.placeholder {
                        background: #111;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        font-size: 3vw;
                        color: rgba(255, 255, 255, 0.6);
                    }
                }

                & .meta {
                    display: flex;
                    flex-direction: column;
                    gap: 0.5vw;

                    & .title {
                        display: flex;
                        align-items: baseline;
                        gap: 0.5vw;
                        max-width: 50vw;

                        & span {
                            // overflow: hidden clips at the line box, the reset's line-height: 1 would cut off g, y, p
                            line-height: 1.3;
                            overflow: hidden;
                            white-space: nowrap;
                            text-overflow: ellipsis;
                        }

                        // The subtitle gives up its space first, the name is only cut off once it is gone
                        & .name {
                            font-weight: 600;
                            font-size: 1.5vw;
                            letter-spacing: 0.1vw;
                            flex-shrink: 1;
                        }

                        & .subtitle {
                            color: rgba(255, 255, 255, 0.6);
                            font-size: 1vw;
                            flex-shrink: 9999;
                        }
                    }

                    & .artist {
                        color: rgba(255, 255, 255, 0.6);
                        font-size: 1vw;
                        line-height: 1.3;
                        overflow: hidden;
                        word-break: break-all;
                        max-width: 50vw;
                        white-space: nowrap;
                        text-overflow: ellipsis;
                    }

                    & .charter {
                        color: rgba(255, 255, 255, 0.5);
                        font-size: 0.85vw;
                        line-height: 1.3;
                        letter-spacing: 0.05vw;
                        overflow: hidden;
                        max-width: 50vw;
                        white-space: nowrap;
                        text-overflow: ellipsis;
                    }

                    & .tags {
                        display: flex;
                        gap: 0.5vw;
                        margin-top: 0.25vw;

                        // Direct children only, the rolling time has spans of its own
                        & > span {
                            --digit-gap: 0.1vw;
                            font-size: 0.8vw;
                            font-weight: 600;
                            letter-spacing: 0.1vw;
                            background: rgba(255, 255, 255, 0.1);
                            padding: 0.4vw 0.6vw;
                            border-radius: 0.25vw;
                        }
                    }

                    & .progress {
                        width: 25vw;
                        height: 0.3vw;
                        background: rgba(255, 255, 255, 0.1);
                        border-radius: 0.15vw;
                        overflow: hidden;
                        position: relative;

                        & .progress-fill {
                            position: absolute;
                            top: 0;
                            bottom: 0;
                            left: 0;
                            overflow: hidden;
                            transition: width 0.3s linear;

                            & .tint {
                                background: var(--themeBackground);
                                background-size: cover;
                                position: absolute;
                                inset: 0;
                                filter: none;
                            }
                        }
                    }
                }
            }

            // Banner and ref den share one cell so they cross fade in place
            & .side {
                // Stays in the right column when there's no chart
                grid-column: 2;
                display: grid;
                justify-items: end;
                align-items: center;

                & > * {
                    grid-area: 1 / 1;
                }
            }

            & .banner {
                background: #111;
                padding: 1.25vw 2vw;
                border-radius: 0.5vw;
                display: flex;
                gap: 1vw;
                align-items: center;
                position: relative;
                overflow: hidden;

                & .tint {
                    background: url('../../assets/background.svg');
                    background-size: cover;
                    position: absolute;
                    left: 0;
                    right: 0;
                    bottom: 0;
                    top: 0;
                    z-index: 0;

                    &.themed {
                        background-image: var(--themeBackground);
                        filter: none;
                    }
                }

                & .text {
                    display: flex;
                    gap: 0.5vw;
                    flex-direction: column;
                    text-align: right;
                    position: relative;
                    z-index: 10;

                    & span:nth-child(1) {
                        font-size: 0.9vw;
                        color: rgba(0, 0, 0, 0.75);
                        letter-spacing: 0.1vw;
                    }

                    & span:nth-child(2) {
                        color: #111;
                        font-size: 1.25vw;
                        font-weight: 600;
                    }
                }

                & .mdi {
                    position: relative;
                    z-index: 10;
                    width: 3vw;
                    height: 3vw;
                    font-size: 2.5vw;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    color: #111;
                }
            }
        }
    }
}

.side-enter-active,
.side-leave-active {
    transition: opacity 0.5s ease, transform 0.5s ease;
}
.side-enter-from,
.side-leave-to {
    opacity: 0;
    transform: translateY(1vw);
}

@keyframes tugLowHealth {
    from {
        opacity: 1;
    }
    to {
        opacity: 0.35;
    }
}

.results-enter-active,
.results-leave-active {
    transition: opacity 0.4s ease-out, transform 0.4s ease-out;
}
.results-enter-from,
.results-leave-to {
    opacity: 0;
    transform: scale(0.98);
}

.lead-enter-active,
.lead-leave-active {
    transition: opacity 0.25s ease-out;
}
.lead-enter-from,
.lead-leave-to {
    opacity: 0;
}

.tug-enter-active,
.tug-leave-active {
    transition: opacity 0.5s ease-out;
}
.tug-enter-from,
.tug-leave-to {
    opacity: 0;
}
</style>
