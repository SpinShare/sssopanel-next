<template>
    <ScreenLayout>
        <section class="screen-match-ingame">
            <section class="match-info">MATCHINFO</section>
            <section class="streams">
                <OvenPlayerVue3
                    ref="stream1Ref"
                    :config="stream1Config"
                    @ready="player1ReadyHandler"
                    @error="player1ErrorHandler"
                />
                <OvenPlayerVue3
                    ref="stream2Ref"
                    :config="steam2Config"
                    @ready="player2ReadyHandler"
                    @error="player2ErrorHandler"
                />
            </section>
            <section class="chart-info">CHARTINFO</section>
        </section>
    </ScreenLayout>
</template>

<script setup>
import OvenPlayerVue3 from 'ovenplayer-vue3';
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import { ref, inject, onMounted, onUnmounted } from 'vue';
const emitter = inject('emitter');

const streamConfig = {
    mute: true,
    doubleTapToSeek: false,
    controls: false,
};
const stream1Ref = ref(null);
const stream2Ref = ref(null);
const stream1Config = {
    ...streamConfig,
    sources: [
        {
            type: 'webrtc',
            file: 'ws://eu3.rtmp.spinsha.re:3333/app/yJA4MQ98',
        },
    ],
};
const steam2Config = {
    ...streamConfig,
    sources: [
        {
            type: 'webrtc',
            file: 'ws://eu3.rtmp.spinsha.re:3333/app/yJA4MQ98',
        },
    ],
};

const player1 = ref(null);
const player2 = ref(null);
const streamActive = ref(true);
const audioActive = ref('left');

onMounted(() => {
    emitter.on('current-route-get-response', (data) => {
        player1.value = data?.RichData?.player1 ?? null;
        player2.value = data?.RichData?.player2 ?? null;
    });
});
onUnmounted(() => {
    emitter.off('current-route-get-response');
});
</script>

<style lang="scss" scoped>
.screen-match-ingame {
    display: grid;
    grid-template-rows: 1fr auto 1fr;
    align-items: center;

    & .streams {
        position: relative;
        width: 100%;
        height: 28vw;
        display: grid;
        grid-template-columns: 1fr 1fr;
    }
}
</style>
