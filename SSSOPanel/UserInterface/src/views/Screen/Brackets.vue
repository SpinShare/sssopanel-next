<template>
    <ScreenLayout>
        <section class="screen-brackets">
            <div class="tint"></div>
            <div class="noise"></div>

            <div class="content">
                <header>
                    <div class="text">
                        <span>{{ phase }}</span>
                        <span>Pool {{ phaseGroup }}</span>
                    </div>
                    <div class="bars"></div>
                </header>
                <div class="bracket">
                    <section>
                        <div class="upper">
                            <header>Semi-Final</header>
                            <div class="games">
                                <div class="game game-a">
                                    <span class="game-tag">A</span>
                                    <template
                                        v-for="(player, i) in matches[0]?.players"
                                        :key="i"
                                    >
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                                <div class="game game-b">
                                    <span class="game-tag">B</span>
                                    <template
                                        v-for="(player, i) in matches[1]?.players"
                                        :key="i"
                                    >
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower">
                            <header>Losers Semi-Final</header>
                            <div class="games">
                                <div class="game game-f">
                                    <span class="game-tag">F</span>
                                    <template
                                        v-for="(player, i) in matches[2]?.players"
                                        :key="i"
                                    >
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>
                    <section>
                        <div class="upper">
                            <header>Final</header>
                            <div class="games">
                                <div class="game game-c">
                                    <span class="game-tag">C</span>
                                    <template
                                        v-for="(player, i) in matches[3]?.players"
                                        :key="i"
                                    >
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower">
                            <header>Losers Final</header>
                            <div class="games">
                                <div class="game game-g">
                                    <span class="game-tag">G</span>
                                    <template
                                        v-for="(player, i) in matches[4]?.players"
                                        :key="i"
                                    >
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>
                    <section>
                        <div class="upper">
                            <header>Grand Final</header>
                            <div class="games">
                                <div class="game game-d">
                                    <span class="game-tag">D</span>
                                    <template
                                        v-for="(player, i) in matches[5]?.players"
                                        :key="i"
                                    >
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>
                </div>
                <div class="bars"></div>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import { inject, onMounted, onUnmounted, ref } from 'vue';
import ScreenBracketUser from '@/components/ScreenBracketUser/ScreenBracketUser.vue';
const emitter = inject('emitter');

const phase = ref('');
const phaseGroup = ref('');
const matches = ref([]);

onMounted(() => {
    emitter.on('current-route-get-response', (data) => {
        phase.value = data?.RichData?.phase ?? '';
        phaseGroup.value = data?.RichData?.phaseGroup ?? '';
        matches.value = data?.RichData?.matches ?? [];
    });
});
onUnmounted(() => {
    emitter.off('current-route-get-response');
});
</script>

<style lang="scss" scoped>
.screen-brackets {
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
        gap: 1vw;
        grid-auto-rows: auto 1fr auto;
        position: absolute;
        left: 4vw;
        right: 4vw;
        bottom: 4vw;
        top: 4vw;
        font-size: 1vw;
        z-index: 10;
        color: #eee;

        & > header {
            display: grid;
            grid-template-columns: auto 1fr;
            gap: 2vw;

            & .text {
                display: flex;
                flex-direction: column;
                gap: 0.5vw;

                & span:nth-child(1) {
                    font-weight: bold;
                    font-size: 1vw;
                    letter-spacing: 0.1vw;
                }
                & span:nth-child(2) {
                    color: rgba(255, 255, 255, 0.4);
                    font-size: 2vw;
                    letter-spacing: 0.2vw;
                }
            }
        }
        & .bracket {
            display: grid;
            grid-template-columns: 1fr 1fr 1fr;
            gap: 2vw;

            & > section {
                display: flex;
                gap: 2.5vw;
                flex-direction: column;
                justify-content: center;

                & .upper,
                .lower {
                    display: flex;
                    flex-direction: column;
                    font-size: 1vw;

                    & header {
                        align-self: flex-start;
                        background: #111;
                        padding: 0.5vw 1.5vw;
                        font-weight: bold;
                        letter-spacing: 0.2vw;
                        border-top-left-radius: 0.5vw;
                        border-top-right-radius: 0.5vw;
                    }
                    & .games {
                        letter-spacing: 0.1vw;
                        border-radius: 0 0.5vw 0.5vw 0.5vw;
                        background: #111;
                        font-size: 1.15vw;
                        padding: 0.75vw;
                        display: flex;
                        flex-direction: column;
                        gap: 0.5vw;

                        & .game {
                            display: grid;
                            gap: 0.25vw;
                            background: #ededed;
                            color: #222;
                            padding: 0.5vw;
                            border-radius: 0.3vw;
                            position: relative;

                            & .game-tag {
                                position: absolute;
                                font-weight: bold;
                                font-size: 1.5vw;
                                background: #111;
                                border-radius: 0.25vw;
                                color: #ededed;
                                padding: 0.3vw 0 0.3vw 0.6vw;
                                letter-spacing: 0;
                                top: 2.25vw;
                                left: -2.5vw;
                            }
                        }
                    }
                }
            }
        }
        & .bars {
            background: repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.25), rgba(255, 255, 255, 0.25) 0.5vw, transparent 0.75vw, transparent 2vw);
            min-height: 1vw;
        }
    }

    & img {
        height: 10%;
    }
}
</style>
