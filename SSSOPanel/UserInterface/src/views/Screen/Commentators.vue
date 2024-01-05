<template>
    <ScreenLayout>
        <section class="screen-commentators">
            <div class="tint winter"></div>
            <div class="noise"></div>

            <div class="content">
                <header>COMMENTATORS</header>
                <div class="streams">
                    <iframe :src="`https://vdo.ninja/?scene&room=${roomId}&password=${roomPassword}&clean&transparent&noheader&&sl=skype&style=2&na`"></iframe>
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
import { onMounted, onUnmounted, ref, inject } from 'vue';
const emitter = inject('emitter');

const roomId = ref('');
const roomPassword = ref('');

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        roomId.value = state?.commentators?.roomId ?? roomId.value;
        roomPassword.value = state?.commentators?.roomPassword ?? roomPassword.value;
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
.screen-commentators {
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
        z-index: 10;
        position: absolute;
        top: 7vw;
        bottom: 7vw;
        left: 10vw;
        right: 10vw;
        display: grid;
        grid-template-rows: auto 1fr;

        & header {
            justify-self: flex-start;
            font-size: 1vw;
            font-weight: 900;
            letter-spacing: 0.15vw;
            background: #111111;
            color: #ededed;
            padding: 0.5vw 1vw;
            border-top-left-radius: 0.5vw;
            border-top-right-radius: 0.5vw;
        }

        & .streams {
            background: #111111;
            border-radius: 0 1vw 1vw 1vw;
            overflow: hidden;

            & iframe {
                position: absolute;
                width: 100%;
                height: 100%;
                top: 0;
                left: 0;
                right: 0;
                bottom: 0;
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
