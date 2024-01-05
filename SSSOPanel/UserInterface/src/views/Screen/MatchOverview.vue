<template>
    <ScreenLayout>
        <section class="screen-match-overview">
            <div class="tint winter"></div>
            <div class="noise"></div>

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
                                    :style="`background-image: url(${player1.spinshareProfile.avatar})`"
                                ></div>
                            </div>
                            <div class="meta">
                                <div
                                    v-if="player1.spinshareProfile.pronouns"
                                    class="pronouns"
                                >
                                    {{ player1.spinshareProfile.pronouns }}
                                </div>
                                <div class="username">
                                    {{ player1.spinshareProfile.username }}
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="versus"><span>V</span><span>E</span><span>R</span><span>S</span><span>U</span><span>S</span></div>
                    <div class="player">
                        <header>SPEENOPEN PLAYER</header>
                        <div class="box">
                            <div class="avatar-box">
                                <div
                                    class="avatar"
                                    :style="`background-image: url(${player2.spinshareProfile.avatar})`"
                                ></div>
                            </div>
                            <div class="meta">
                                <div
                                    v-if="player2.spinshareProfile.pronouns"
                                    class="pronouns"
                                >
                                    {{ player2.spinshareProfile.pronouns }}
                                </div>
                                <div class="username">
                                    {{ player2.spinshareProfile.username }}
                                </div>
                            </div>
                        </div>
                    </div>
                </section>
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
import { onMounted, onUnmounted, ref, inject } from 'vue';
const emitter = inject('emitter');

const player1 = ref(null);
const player2 = ref(null);

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        player1.value = state?.currentMatch?.players?.player1 ?? player1.value;
        player2.value = state?.currentMatch?.players?.player2 ?? player2.value;
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

        &.winter {
            filter: hue-rotate(160deg);
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

        & .player-info {
            display: grid;
            grid-template-columns: 1fr auto 1fr;
            justify-content: center;
            align-items: center;
            gap: 5vw;

            & .player {
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
                    & .meta {
                        display: flex;
                        flex-direction: column;
                        gap: 0.25vw;

                        & .pronouns {
                            color: rgba(255, 255, 255, 0.6);
                            font-size: 1vw;
                            font-weight: 600;
                        }
                        & .username {
                            font-weight: 600;
                            letter-spacing: 0.1vw;
                        }
                    }
                }
            }
            & .versus {
                font-size: 4vw;
                font-family: 'JetBrains Mono', monospace;
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
            gap: 1em;
            position: absolute;
            overflow: hidden;
            font-size: 3vw;
            font-weight: 800;
            color: rgba(0, 0, 0, 0.2);

            &.top {
                top: 0;
                left: -14.85em;
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
        left: -14.85em;
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
        left: -14.85em;
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
</style>
