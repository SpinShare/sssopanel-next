<template>
    <ScreenLayout>
        <section class="screen-streamstart-countdown">
            <div class="tint summer"></div>
            <div class="noise"></div>

            <div class="content">
                <img
                    :src="EventLogo"
                    class="event-logo"
                />

                <div
                    class="countdown"
                    v-if="countdownActive"
                >
                    <div class="header">STARTING UP...</div>
                    <div class="time-left">
                        <span>{{ timeLeft }}</span>
                        <span>{{ timeLeftMilliseconds }}</span>
                    </div>
                </div>
            </div>

            <div class="dots left"></div>
            <div class="dots right"></div>

            <div class="marquee top">
                <template
                    v-for="n in 10"
                    :key="n"
                >
                    <span>SPINSHARE SPEEN OPEN</span>
                </template>
            </div>
            <div class="marquee bottom">
                <template
                    v-for="n in 10"
                    :key="n"
                >
                    <span>SPINSHARE SPEEN OPEN</span>
                </template>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import EventLogo from '../../assets/eventlogo-summer-2025.svg';
import { ref, inject, onMounted, onUnmounted } from 'vue';
const emitter = inject('emitter');

const countdownActive = ref(false);
const countdownTime = ref(null);
const timeLeft = ref('00:00');
const timeLeftMilliseconds = ref('.0');

const pad = (number, length = 2) => {
    return number.toString().padStart(length, '0');
};

setInterval(() => {
    if (countdownTime.value === null) return;

    let currentDate = new Date();
    let timeDifference = countdownTime.value.getTime() - currentDate.getTime();

    if (timeDifference > 0) {
        let minutes = Math.floor((timeDifference / (1000 * 60)) % 60);
        let seconds = Math.floor((timeDifference / 1000) % 60);
        let milliseconds = Math.floor((timeDifference % 1000) / 100);

        timeLeft.value = `${pad(minutes)}:${pad(seconds)}`;
        timeLeftMilliseconds.value = `.${milliseconds}`;
    } else {
        timeLeft.value = '00:00';
        timeLeftMilliseconds.value = '.0';
    }
}, 50);

onMounted(() => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-get',
        }),
    );

    emitter.on('state-get-response', (state) => {
        countdownActive.value = state?.countdown?.active ?? countdownActive.value;
        countdownTime.value = state?.countdown?.time ? new Date(state?.countdown?.time) : countdownTime.value;
    });
});

onUnmounted(() => {
    emitter.off('state-get-response');
});
</script>

<style lang="scss" scoped>
    .screen-streamstart-countdown {
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

            &.summer {
                filter: hue-rotate(-25deg); // Default Summer: 0deg, Adjusted for late SSSO: -25deg
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
            display: flex;
            gap: 10vw;
            z-index: 10;
            align-items: center;
            font-size: 0.8vw;

            & .event-logo {
                justify-self: center;
                align-self: center;
                width: 25vw;
                height: 25vw;
            }

            & .countdown {
                background: #111111;
                color: #ededed;
                padding: 0.5vw 2.5vw;
                border-radius: 0 0.5vw 0.5vw 0.5vw;
                position: relative;

                & .header {
                    font-size: 1vw;
                    font-weight: 900;
                    letter-spacing: 0.15vw;
                    background: #111111;
                    color: #ededed;
                    position: absolute;
                    top: -1.45vw;
                    left: 0;
                    padding: 0.5vw 1vw;
                    border-top-left-radius: 0.5vw;
                    border-top-right-radius: 0.5vw;
                }

                & .time-left {
                    font-family: 'JetBrains Mono', monospace;
                    font-weight: 900;
                    display: flex;
                    align-items: flex-end;

                    & span:nth-child(1) {
                        font-size: 5vw;
                    }

                    & span:nth-child(2) {
                        font-size: 3vw;
                        font-weight: 600;
                        transform: translateY(-0.1vw);
                        opacity: 0.6;
                    }
                }
            }
        }

        & .dots {
            position: absolute;
            top: 5vw;
            width: 4vw;
            bottom: 5vw;
            background-size: 1.5vw 1.5vw;
            background-image: radial-gradient(circle, #000 10%, transparent 12%), radial-gradient(circle, #000 10%, transparent 12%);

            &.left {
                left: 2vw;
            }

            &.right {
                right: 2vw;
            }
        }

        & .marquee {
            white-space: nowrap;
            display: flex;
            gap: 2vw;
            position: absolute;
            overflow: hidden;
            font-size: 1.5vw;
            font-weight: 600;

            &.top {
                top: 1.5vw;
                left: -23.75vw;
                animation: marqueeLeft 10s infinite linear;
            }

            &.bottom {
                bottom: 1.5vw;
                left: 0;
                animation: marqueeRight 10s infinite linear;
            }
        }
    }

@keyframes marqueeLeft {
    from {
        left: -23.75vw;
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
        left: -23.75vw;
    }
}
</style>
