<template>
    <ScreenLayout>
        <section class="screen-match-ingame">
            <div class="tint"></div>
            <div class="noise"></div>

            <div class="content">
                <section class="match-info">
                    <div
                        class="player"
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
                                {{ player1?.spinshareProfile?.username }}
                            </div>
                        </div>
                    </div>
                    <div class="stats">
                        <div class="tint winter"></div>
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
                                <transition
                                    name="default"
                                    mode="out-in"
                                    v-for="n in Math.ceil(scoresSetsMax / 2)"
                                >
                                    <span
                                        class="point"
                                        :key="n + (n < scoresScorePlayer1 + 1)"
                                        :class="{ active: n < scoresScorePlayer1 + 1 }"
                                    ></span>
                                </transition>
                            </div>
                            <div class="player">
                                <transition
                                    name="default"
                                    mode="out-in"
                                    v-for="n in Math.ceil(scoresSetsMax / 2)"
                                >
                                    <span
                                        class="point"
                                        :key="n + (n < scoresScorePlayer2 + 1)"
                                        :class="{ active: n < scoresScorePlayer2 + 1 }"
                                    ></span>
                                </transition>
                            </div>
                        </div>
                    </div>
                    <div
                        class="player"
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
                                {{ player2?.spinshareProfile?.username }}
                            </div>
                        </div>
                        <div
                            class="avatar"
                            :style="`background-image: url(${player2?.spinshareProfile?.avatar})`"
                        ></div>
                    </div>
                </section>
                <section class="streams">
                    <transition
                        name="default"
                        mode="out-in"
                    >
                        <div
                            class="stream"
                            :style="{ opacity: streamsActive ? 1 : 0 }"
                            :key="'stream1-' + streamsActive"
                        >
                            <transition
                                name="promo"
                                mode="out-in"
                            >
                                <div
                                    class="audio-active"
                                    v-if="audioActive === 'left'"
                                >
                                    <span class="mdi mdi-volume-high"></span>
                                </div>
                            </transition>

                            <OvenPlayerVue3
                                ref="stream1Ref"
                                :config="stream1Config"
                            />
                        </div>
                    </transition>
                    <transition
                        name="default"
                        mode="out-in"
                    >
                        <div
                            class="stream"
                            :style="{ opacity: streamsActive ? 1 : 0 }"
                            :key="'stream2-' + streamsActive"
                        >
                            <transition
                                name="promo"
                                mode="out-in"
                            >
                                <div
                                    class="audio-active"
                                    v-if="audioActive === 'right'"
                                >
                                    <span class="mdi mdi-volume-high"></span>
                                </div>
                            </transition>

                            <OvenPlayerVue3
                                ref="stream2Ref"
                                :config="stream2Config"
                            />
                        </div>
                    </transition>
                </section>
                <section
                    class="chart-info"
                    v-if="!!chart"
                >
                    <div class="chart">
                        <div
                            class="cover"
                            :style="`background-image: url('${chart.paths.cover}')`"
                        ></div>
                        <div class="meta">
                            <span class="title">{{ chart.title }}</span>
                            <span class="artist">{{ chart.artist }} &bull; {{ chart.charter }}</span>
                        </div>
                    </div>
                    <div class="banner">
                        <div class="tint winter"></div>
                        <div class="noise"></div>

                        <div class="text">
                            <span>Get this chart on SpinSha.re!</span>
                            <span>spinsha.re/song/{{ chart.id }}</span>
                        </div>
                        <span class="mdi mdi-archive-music-outline"></span>
                    </div>
                </section>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import OvenPlayerVue3 from 'ovenplayer-vue3';
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import { ref, inject, onMounted, onUnmounted } from 'vue';
const emitter = inject('emitter');

const streamConfig = {
    autoStart: true,
    showBigPlayButton: false,
    mute: true,
    doubleTapToSeek: false,
    controls: false,
};
const stream1Ref = ref(null);
const stream2Ref = ref(null);
const stream1Config = {
    ...streamConfig,
};
const stream2Config = {
    ...streamConfig,
};

const player1 = ref(null);
const player2 = ref(null);
const chart = ref(null);
const scoresSetsCurrent = ref(0);
const scoresSetsMax = ref(3);
const scoresScorePlayer1 = ref(0);
const scoresScorePlayer2 = ref(0);
const streamsActive = ref(true);
const audioActive = ref('left');

const updateAudioState = () => {
    if (audioActive.value === 'left') {
        stream1Ref.value.playerInstance.setMute(false);
        stream2Ref.value.playerInstance.setMute(true);
    } else {
        stream1Ref.value.playerInstance.setMute(true);
        stream2Ref.value.playerInstance.setMute(false);
    }
};

const updateStreamsState = () => {
    if (streamsActive.value) {
        stream1Ref.value.playerInstance.play();
        stream2Ref.value.playerInstance.play();
    } else {
        stream1Ref.value.playerInstance.stop();
        stream2Ref.value.playerInstance.stop();
    }
};

const updateStreams = () => {
    if (!stream1Ref.value.playerInstance.getPlaylist()[0]?.sources[0]?.file?.includes(player1.value.key ?? 'unknown')) {
        stream1Ref.value.playerInstance.stop();
        stream1Ref.value.playerInstance.load([
            {
                type: 'webrtc',
                file: `ws://${player1.value.region ?? 'unknown'}.rtmp.spinsha.re:3333/app/${player1.value.key ?? 'unknown'}`,
            },
        ]);
        stream1Ref.value.playerInstance.play();
    }

    if (!stream2Ref.value.playerInstance.getPlaylist()[0]?.sources[0]?.file?.includes(player2.value.key ?? 'unknown')) {
        stream2Ref.value.playerInstance.stop();
        stream2Ref.value.playerInstance.load([
            {
                type: 'webrtc',
                file: `ws://${player2.value.region ?? 'unknown'}.rtmp.spinsha.re:3333/app/${player2.value.key ?? 'unknown'}`,
            },
        ]);
        stream2Ref.value.playerInstance.play();
    }
};

onMounted(() => {
    emitter.on('current-route-get-response', (data) => {
        player1.value = data?.RichData?.player1 ?? null;
        player2.value = data?.RichData?.player2 ?? null;
        chart.value = data?.RichData?.chart ?? null;
        streamsActive.value = data?.RichData?.streamsActive ?? true;
        audioActive.value = data?.RichData?.audioActive ?? 'left';

        scoresSetsCurrent.value = data?.RichData?.scores?.sets?.current ?? 0;
        scoresSetsMax.value = data?.RichData?.scores?.sets?.max ?? 3;
        scoresScorePlayer1.value = data?.RichData?.scores?.score?.player1 ?? 0;
        scoresScorePlayer2.value = data?.RichData?.scores?.score?.player2 ?? 0;

        updateAudioState();
        updateStreamsState();
        updateStreams();
    });
    emitter.on('screen-match-update-response', (data) => {
        player1.value = data?.player1 ?? player1.value;
        player2.value = data?.player2 ?? player2.value;
        chart.value = data?.chart ?? chart.value;
        streamsActive.value = data?.streamsActive ?? streamsActive.value;
        audioActive.value = data?.audioActive ?? audioActive.value;

        scoresSetsCurrent.value = data?.scores?.sets?.current ?? scoresSetsCurrent.value;
        scoresSetsMax.value = data?.scores?.sets?.max ?? scoresSetsMax.value;
        scoresScorePlayer1.value = data?.scores?.score?.player1 ?? scoresScorePlayer1.value;
        scoresScorePlayer2.value = data?.scores?.score?.player2 ?? scoresScorePlayer2.value;

        updateAudioState();
        updateStreamsState();
        updateStreams();
    });
});
onUnmounted(() => {
    emitter.off('current-route-get-response');
    emitter.off('screen-match-update-response');
});
</script>

<style lang="scss" scoped>
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

        & .match-info {
            display: grid;
            grid-template-columns: 1fr auto 1fr;
            gap: 2vw;
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
                }

                &:nth-child(3) {
                    justify-content: flex-end;

                    & .meta {
                        text-align: right;
                    }
                }
            }

            & .stats {
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

                    &.winter {
                        filter: hue-rotate(160deg);
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

        & .streams {
            position: relative;
            width: 100%;
            height: 28vw;
            display: grid;
            grid-template-columns: 1fr 1fr;
            background: #111;

            & .stream {
                position: relative;

                & .audio-active {
                    position: absolute;
                    left: 0.5vw;
                    bottom: 0.5vw;
                    padding: 0.25vw 0.5vw;
                    background: #111;
                    color: #ededed;
                    border-radius: 0.5vw;
                    font-size: 1.5vw;
                    z-index: 500;
                }
            }
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
                    width: 7vw;
                    height: 7vw;
                    border-radius: 1vw;
                    flex-shrink: 0;
                    background-position: center;
                    background-size: cover;
                }
                & .meta {
                    display: flex;
                    flex-direction: column;
                    gap: 0.5vw;

                    & .title {
                        font-weight: 600;
                        font-size: 1.5vw;
                        letter-spacing: 0.1vw;
                        overflow: hidden;
                        word-break: break-all;
                        max-width: 50vw;
                        white-space: nowrap;
                        text-overflow: ellipsis;
                    }
                    & .artist {
                        color: rgba(255, 255, 255, 0.6);
                        font-size: 1vw;
                        overflow: hidden;
                        word-break: break-all;
                        max-width: 50vw;
                        white-space: nowrap;
                        text-overflow: ellipsis;
                    }
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

                    &.winter {
                        filter: hue-rotate(160deg);
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
</style>
