<template>
    <ScreenLayout>
        <section class="screen-end-stream">
            <div class="tint winter"></div>
            <div class="noise"></div>

            <div class="content">
                <div class="left">
                    <div class="logo">
                        <img
                            :src="EventLogo"
                            class="event-logo"
                        />
                    </div>

                    <div class="next-stream">
                        <header>NEXT STREAM</header>
                        <div class="box">
                            <span>{{ formattedNextStreamDate }}</span>
                            <span>{{ relativeDays }}</span>
                        </div>
                    </div>
                </div>
                <div class="dots"></div>
                <div class="right">
                    <div class="credits">
                        <header>CREDITS</header>
                        <div class="box">
                            <div
                                class="scrolling-text"
                                ref="creditsText"
                            >
                                <h1>SSSO Winter 2024</h1>
                                <h2>staff &amp; contributors</h2>
                                <div class="lead">
                                    <span>Laura Sofia Heimann</span>
                                    <span>Lead Developer &amp; UI/Branding Design</span>
                                </div>
                                <div class="lead">
                                    <span>Ellie "ellitedev" Leijten</span>
                                    <span>Lead Community</span>
                                </div>
                                <div class="lead">
                                    <span>Michael Torres (TheWay123)</span>
                                    <span>Lead Events</span>
                                </div>

                                <div class="group">
                                    <span>tournament organizers</span>
                                    <span>Ricki</span>
                                    <span>TheWay123</span>
                                </div>

                                <div class="group">
                                    <span>organizational contributors</span>
                                    <span>Aexus</span>
                                    <span>CarbonCarbon12</span>
                                    <span>Gavi Guy</span>
                                    <span>HalloweenBaby</span>
                                    <span>Programmatic</span>
                                    <span>snaphap</span>
                                </div>
                                <div class="group">
                                    <span>it &amp; backend resources</span>
                                    <span>Ellie "ellitedev" Leijten</span>
                                    <span>Ricki</span>
                                </div>
                                <div class="group">
                                    <span>internal support</span>
                                    <span>Gavi Guy</span>
                                    <span>Ricki</span>
                                </div>
                                <div class="group">
                                    <span>broadcasters</span>
                                    <span>Axol</span>
                                    <span>Gavi Guy</span>
                                    <span>Khromatical</span>
                                </div>
                                <div class="group">
                                    <span>commentators</span>
                                    <span>Halflite</span>
                                    <span>JammyDodger</span>
                                    <span>Khromatical</span>
                                    <span>Luna</span>
                                    <span>metalman20</span>
                                    <span>rewardadrawer</span>
                                    <span>TheWay123</span>
                                </div>
                                <div class="group">
                                    <span>referees</span>
                                    <span>Gavi Guy</span>
                                    <span>Halflite</span>
                                    <span>Khromatical</span>
                                    <span>Luna</span>
                                    <span>LuminaryCat</span>
                                    <span>snaphap</span>
                                    <span>TheWay123</span>
                                </div>
                                <div class="group">
                                    <span>playtesters</span>
                                    <span>CarbonCarbon12</span>
                                    <span>Edge</span>
                                    <span>Gavi Guy</span>
                                    <span>Halflite</span>
                                    <span>Kali</span>
                                    <span>Kwazi</span>
                                    <span>Luna</span>
                                    <span>HalloweenBaby</span>
                                    <span>MrCringe</span>
                                    <span>Negative</span>
                                    <span>PnOMader</span>
                                    <span>snaphap</span>
                                    <span>TheWay123</span>
                                </div>

                                <h2>players</h2>
                                <div class="players">
                                    <span
                                        v-for="player in players"
                                        :key="player.id"
                                    >
                                        <span
                                            class="pronouns"
                                            v-if="player.spinshareProfile.pronouns"
                                        >
                                            {{ player.spinshareProfile.pronouns }}
                                        </span>
                                        <span>
                                            {{ player.spinshareProfile.username }}
                                        </span>
                                    </span>
                                </div>

                                <h2>with charts created by</h2>
                                <div class="group">
                                    <span></span>
                                    <span>A11right</span>
                                    <span>Airplane man</span>
                                    <span>Animanic</span>
                                    <span>Axol</span>
                                    <span>Ballinbino</span>
                                    <span>C4veden</span>
                                    <span>CarbonCarbon12</span>
                                    <span>Crooky</span>
                                    <span>Dacey</span>
                                    <span>Dama</span>
                                    <span>Dr. Chicken</span>
                                    <span>Edge</span>
                                    <span>Eraser</span>
                                    <span>Fether</span>
                                    <span>Gamer97</span>
                                    <span>Gavi Guy</span>
                                    <span>Halflite</span>
                                    <span>HalloweenBaby</span>
                                    <span>haomakk</span>
                                    <span>HyperDash</span>
                                    <span>Jane</span>
                                    <span>Kali</span>
                                    <span>Konomi</span>
                                    <span>Kwazi</span>
                                    <span>kyuquu</span>
                                    <span>Lava</span>
                                    <span>Loosiano</span>
                                    <span>LuminaryCat</span>
                                    <span>Luna</span>
                                    <span>Mapy</span>
                                    <span>★MrCringe★</span>
                                    <span>NEFirefox</span>
                                    <span>Negative</span>
                                    <span>NOIN</span>
                                    <span>Programmatic</span>
                                    <span>PickPig</span>
                                    <span>Reilmen</span>
                                    <span>rewardadrawer</span>
                                    <span>Ricki</span>
                                    <span>Schroeder</span>
                                    <span>SignatureMPX</span>
                                    <span>skiera</span>
                                    <span>Skillz Man</span>
                                    <span>smb</span>
                                    <span>snaphap</span>
                                    <span>Snepped</span>
                                    <span>sodapants</span>
                                    <span>Steven of Astora</span>
                                    <span>Stride</span>
                                    <span>swago</span>
                                    <span>TheBlockiest</span>
                                    <span>Theo</span>
                                    <span>TheWay123</span>
                                    <span>TreXDer</span>
                                    <span>一只某Y</span>
                                    <span>Velvet-.</span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div class="marquees">
                <div class="marquee top">
                    <template
                        v-for="n in 10"
                        :key="n"
                    >
                        <span>SEE YOU</span>
                    </template>
                </div>
                <div class="marquee bottom">
                    <template
                        v-for="n in 10"
                        :key="n"
                    >
                        <span>SEE YOU</span>
                    </template>
                </div>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import EventLogo from '@/assets/eventlogo-winter-2023.svg';
import { onMounted, onUnmounted, inject, ref, computed } from 'vue';
const emitter = inject('emitter');

const nextStreamDate = ref(new Date());
const formattedNextStreamDate = computed(() => {
    const date = new Date(nextStreamDate.value);
    const day = ('0' + date.getDate()).slice(-2);
    const month = ('0' + (date.getMonth() + 1)).slice(-2);
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
});
const relativeDays = computed(() => {
    const today = new Date();
    const streamingDay = new Date(nextStreamDate.value);

    const diffTime = streamingDay.getTime() - today.getTime();
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    if (diffDays === 2) {
        return 'TOMORROW';
    } else if (diffDays > 1) {
        return `IN ${diffDays} DAYS`;
    } else {
        return 'IN 1 DAY';
    }
});

const players = ref([]);
const creditsText = ref(null);

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        nextStreamDate.value = state?.endOfStream?.nextStreamDate ? new Date(state?.endOfStream?.nextStreamDate) : nextStreamDate.value;
    });

    emitter.on('settings-get-full-response', async (settings) => {
        players.value = settings['currentEvent.playerMapping'] ?? [];
    });

    window.external.sendMessage(
        JSON.stringify({
            command: 'state-get',
        }),
    );

    window.external.sendMessage(
        JSON.stringify({
            command: 'settings-get-full',
        }),
    );

    const textHeight = creditsText.value.getBoundingClientRect().height;
    creditsText.value.style.setProperty('--textHeight', `${textHeight}px`);
});

onUnmounted(() => {
    emitter.off('state-get-response');
    emitter.off('settings-get-full-response');
});
</script>

<style lang="scss" scoped>
.screen-end-stream {
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

        &.winter {
            filter: hue-rotate(160deg);
        }
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5vw;
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        top: 0;
        mix-blend-mode: soft-light;
    }

    & .content {
        position: absolute;
        top: 5vw;
        left: 5vw;
        right: 5vw;
        bottom: 5vw;
        display: grid;
        grid-template-columns: 1fr auto 1fr;
        gap: 5vw;
        z-index: 10;
        justify-content: center;
        align-items: center;
        font-size: 0.8vw;

        & .left {
            display: grid;
            align-self: stretch;
            grid-template-rows: 1fr auto;
            gap: 2vw;

            & .logo {
                display: flex;
                justify-content: center;
                align-items: center;

                & .event-logo {
                    width: 20vw;
                    height: 20vw;
                }
            }

            & .next-stream {
                display: flex;
                flex-direction: column;
                align-items: flex-start;

                & header {
                    font-size: 1vw;
                    font-weight: 900;
                    letter-spacing: 0.15vw;
                    background: #111111;
                    color: #ededed;
                    padding: 0.5vw 1vw;
                    border-top-left-radius: 0.5vw;
                    border-top-right-radius: 0.5vw;
                }
                & .box {
                    align-self: stretch;
                    text-align: center;
                    background: #111111;
                    color: #ededed;
                    padding: 2vw 0;
                    border-radius: 0 0.5vw 0.5vw 0.5vw;
                    font-family: 'JetBrains Mono', monospace;
                    font-weight: 900;
                    display: flex;
                    flex-direction: column;
                    align-items: center;
                    gap: 0.5vw;

                    & span:nth-child(1) {
                        font-size: 2.5vw;
                    }
                    & span:nth-child(2) {
                        font-size: 1.25vw;
                        font-weight: 600;
                        opacity: 0.6;
                    }
                }
            }
        }

        & .right {
            align-self: stretch;
            display: grid;
            overflow: hidden;

            & .credits {
                display: flex;
                flex-direction: column;
                align-items: flex-start;
                overflow: hidden;

                & header {
                    font-size: 1vw;
                    font-weight: 900;
                    letter-spacing: 0.15vw;
                    background: #111111;
                    color: #ededed;
                    padding: 0.5vw 1vw;
                    border-top-left-radius: 0.5vw;
                    border-top-right-radius: 0.5vw;
                }
                & .box {
                    align-self: stretch;
                    flex-grow: 1;
                    text-align: center;
                    background: #111111;
                    color: #ededed;
                    padding: 1.5vw 0;
                    border-radius: 0 0.5vw 0.5vw 0.5vw;
                    overflow: hidden;
                    position: relative;

                    & .scrolling-text {
                        display: flex;
                        flex-direction: column;
                        align-items: center;
                        gap: 2.5vw;
                        font-size: 1vw;
                        animation: scroll 90s linear infinite;
                        position: absolute;
                        width: 100%;
                        height: 100%;

                        & h1 {
                            margin: 0;
                            font-size: 2.5vw;
                            font-weight: bold;
                        }
                        & h2 {
                            margin: 0;
                            font-size: 1.75vw;
                            font-weight: bold;
                        }
                        & > div {
                            display: flex;
                            flex-direction: column;
                            align-items: center;

                            &.lead {
                                gap: 0.75vw;

                                & span:nth-child(1) {
                                    font-weight: bold;
                                    font-size: 1.15vw;
                                    letter-spacing: 0.1vw;
                                }
                                & span:nth-child(2) {
                                    color: rgba(255, 255, 255, 0.6);
                                }
                            }

                            &.group {
                                gap: 0.5vw;

                                & span:nth-child(1) {
                                    font-weight: bold;
                                    letter-spacing: 0.1vw;
                                }
                                & span:not(:nth-child(1)) {
                                    color: rgba(255, 255, 255, 0.6);
                                }
                            }

                            &.players {
                                gap: 0.5vw;
                                display: flex;
                                flex-direction: column;
                                align-items: center;

                                & > span {
                                    display: flex;
                                    gap: 1vw;

                                    & .pronouns {
                                        color: rgba(255, 255, 255, 0.6);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        & .dots {
            width: 4vw;
            height: 100%;
            background-size: 1.5vw 1.5vw;
            background-image: radial-gradient(circle, #000 10%, transparent 12%), radial-gradient(circle, #000 10%, transparent 12%);
        }
    }

    & .marquees {
        transform: rotate(-4deg) scale(1.15);
        position: absolute;
        top: 4vw;
        bottom: 4vw;
        left: 0;
        right: 0;
        z-index: 3;

        & .marquee {
            white-space: nowrap;
            display: flex;
            gap: 1em;
            position: absolute;
            overflow: hidden;
            font-size: 3vw;
            font-weight: 800;
            color: rgba(0, 0, 0, 0.2);

            &.top {
                top: 0;
                left: -18.5vw;
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

@keyframes marqueeLeft {
    from {
        left: -18.5vw;
    }
    to {
        left: 0;
    }
}

@keyframes marqueeRight {
    from {
        left: 0;
    }
    to {
        left: -18.5vw;
    }
}

@keyframes scroll {
    0% {
        transform: translateY(100%);
    }
    100% {
        transform: translateY(calc(-1 * var(--textHeight) * 7));
    }
}
</style>
