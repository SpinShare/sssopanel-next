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
                <div class="bracket"
                     :class="{ 'has-reset': matches?.find((x) => x.identifier === 'E')?.players?.every(player => player?.entrant !== null) }"
                     v-if="(phaseGroup != 'E1') && (phaseGroup != 'F1')">
                    <section>

                        <div class="upper">
                            <header>Semi-Final</header>
                            <div class="games">
                                <div class="game game-a">
                                    <span class="game-tag">A</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'A')?.players"
                                              :key="i">
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                                <div class="game game-b">
                                    <span class="game-tag">B</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'B')?.players"
                                              :key="i">
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
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'F')?.players"
                                              :key="i">
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
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'C')?.players"
                                              :key="i">
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
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'G')?.players"
                                              :key="i">
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
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'D')?.players"
                                              :key="i">
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>
                    <section v-if="matches?.find((x) => x.identifier === 'E')?.players?.every(player => player?.entrant !== null)">
                        <div class="upper">
                            <header>Grand Final Reset</header>
                            <div class="games">
                                <div class="game game-e">
                                    <span class="game-tag">E</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'E')?.players"
                                              :key="i">
                                        <ScreenBracketUser :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>
                </div>

                <div class="bracket-ko"
                     :class="{ 'has-reset': matches?.find((x) => x.identifier === 'I')?.players?.every(player => player?.entrant !== null) }"
                     v-if="(phaseGroup == 'E1') || (phaseGroup == 'F1')">
                    <section>
                        <div class="upper">
                            <header>Quarter-Final</header>
                            <div class="games">
                                <div class="game game-a">
                                    <span class="game-tag">A</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'A')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                                <div class="game game-b">
                                    <span class="game-tag">B</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'B')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                                <div class="game game-c">
                                    <span class="game-tag">C</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'C')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                                <div class="game game-d">
                                    <span class="game-tag">D</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'D')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower">
                            <header>Losers Round 1</header>
                            <div class="games">
                                <div class="game game-j">
                                    <span class="game-tag">J</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'J')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                                <div class="game game-k">
                                    <span class="game-tag">K</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'K')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>

                    <section>
                        <div class="upper">
                            <header>Semi-Final</header>
                            <div class="games">
                                <div class="game game-e">
                                    <span class="game-tag">E</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'E')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                                <div class="game game-f">
                                    <span class="game-tag">F</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'F')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower">
                            <header>Losers Quarter-Final</header>
                            <div class="games">
                                <div class="game game-l">
                                    <span class="game-tag">L</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'L')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                                <div class="game game-m">
                                    <span class="game-tag">M</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'M')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>

                    <section>
                        <div class="upper">
                            <header>Winners Final</header>
                            <div class="games">
                                <div class="game game-g">
                                    <span class="game-tag">G</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'G')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower">
                            <header>Losers Semi-Final</header>
                            <div class="games">
                                <div class="game game-n">
                                    <span class="game-tag">N</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'N')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>

                    <section>
                        <div class="upper">
                            <header>Grand Final</header>
                            <div class="games">
                                <div class="game game-h">
                                    <span class="game-tag">H</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'H')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower">
                            <header>Losers Final</header>
                            <div class="games">
                                <div class="game game-o">
                                    <span class="game-tag">O</span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'O')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                    </section>

                    <section v-if="matches?.find((x) => x.identifier === 'I')?.players?.every(player => player?.entrant !== null)">
                        <div class="upper">
                            <header>Grand Final Reset</header>
                            <div class="games">
                                <div class="game game-i">
                                    <span class="game-tag-small">I </span>
                                    <template v-for="(player, i) in matches?.find((x) => x.identifier === 'I')?.players"
                                              :key="i">
                                        <ScreenBracketUserKO :player="player" />
                                    </template>
                                </div>
                            </div>
                        </div>
                        <div class="lower"></div>
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
    import ScreenBracketUserKO from '@/components/ScreenBracketUser/ScreenBracketUserKO.vue';
    const emitter = inject('emitter');

    const phase = ref('');
    const phaseGroup = ref('');
    const matches = ref([]);

    onMounted(() => {
        emitter.on('state-get-response', (state) => {
            phase.value = state?.brackets?.phase ?? phase.value;
            phaseGroup.value = state?.brackets?.phaseGroup ?? phaseGroup.value;
            matches.value = state?.brackets?.matches ?? matches.value;
        });

        window.external.sendMessage(
            JSON.stringify({
                command: 'state-get',
            }),
        );
    });
    onUnmounted(() => {
        emitter.off('state-get-response');
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
                        font-size: 1.5vw;
                        letter-spacing: 0.2vw;
                    }
                }
            }

            & .bracket {
                display: grid;
                grid-template-columns: 1fr 1fr 1fr;
                gap: 2vw;

                &.has-reset {
                    grid-template-columns: 1fr 1fr 1fr 1fr;
                }

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
                                    font-size: 1vw;
                                    background: #111;
                                    border-radius: 0.25vw;
                                    color: #ededed;
                                    padding: 0.3vw 0 0.3vw 0.6vw;
                                    letter-spacing: 0;
                                    top: 2.25vw;
                                    left: -2vw;
                                }
                            }
                        }
                    }
                }
            }


            & .bracket-ko {
                display: grid;
                grid-template-columns: 1fr 1fr 1fr 1fr;
                gap: 2vw;

                &.has-reset {
                    grid-template-columns: 1fr 1fr 1fr 1fr 1fr;
                }

                & > section {
                    display: flex;
                    gap: 2.75vh;
                    flex-direction: column;
                    justify-content: center;

                    & .upper {
                        display: flex;
                        flex-direction: column;
                        font-size: 1vw;
                        height: 60%;
                        justify-content: center;

                        & header {
                            align-self: flex-start;
                            background: #111;
                            padding: 0.3vw 1vw;
                            font-weight: bold;
                            font-size: 0.7vw;
                            letter-spacing: 0.2vw;
                            border-top-left-radius: 0.5vw;
                            border-top-right-radius: 0.5vw;
                        }

                        & .games {
                            letter-spacing: 0.1vw;
                            border-radius: 0 0.5vw 0.5vw 0.5vw;
                            background: #111;
                            font-size: 0.8vw;
                            padding: 0.5vw;
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
                                    font-size: 0.85vw;
                                    background: #111;
                                    border-radius: 0.25vw;
                                    color: #ededed;
                                    padding: 0.3vw 0.5vw 0.3vw 0.6vw;
                                    letter-spacing: 0;
                                    top: 1.8vw;
                                    left: -2vw;
                                }

                                & .game-tag-small {
                                    position: absolute;
                                    font-weight: bold;
                                    font-size: 0.85vw;
                                    background: #111;
                                    border-radius: 0.25vw;
                                    color: #ededed;
                                    padding: 0.3vw 0.9vw 0.3vw 0.7vw;
                                    letter-spacing: 0;
                                    top: 1.8vw;
                                    left: -2vw;
                                }
                            }
                        }
                    }

                    & .lower {
                        display: flex;
                        height: 34%;
                        flex-direction: column;
                        font-size: 1vw;
                        justify-content: center;

                        & header {
                            align-self: flex-start;
                            background: #111;
                            padding: 0.3vw 1.2vw;
                            font-weight: bold;
                            font-size: 0.7vw;
                            letter-spacing: 0.2vw;
                            border-top-left-radius: 0.5vw;
                            border-top-right-radius: 0.5vw;
                        }

                        & .games {
                            letter-spacing: 0.1vw;
                            border-radius: 0 0.5vw 0.5vw 0.5vw;
                            background: #111;
                            font-size: 0.8vw;
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
                                    font-size: 0.85vw;
                                    background: #111;
                                    border-radius: 0.25vw;
                                    color: #ededed;
                                    padding: 0.3vw 0.5vw 0.3vw 0.6vw;
                                    letter-spacing: 0;
                                    top: 1.9vw;
                                    left: -2vw;
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