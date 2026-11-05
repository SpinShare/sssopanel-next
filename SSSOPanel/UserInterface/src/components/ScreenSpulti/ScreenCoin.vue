<template>
    <!-- spulti's coin: spectators always see it land waveform up, the ring shows the winner's color once it lands -->
    <div
        class="screen-coin"
        :class="{ flipping: !landed }"
        :style="{ '--ring': landed ? color : 'transparent', '--skip': `-${Math.min(elapsed, FLIP_TIME)}ms` }"
    >
        <div class="spin">
            <img
                class="face"
                :src="coinFront"
                alt=""
            />
            <img
                class="face back"
                :src="coinBack"
                alt=""
            />
        </div>
    </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref } from 'vue';
import coinFront from '@/assets/spulti/coin-front.svg';
import coinBack from '@/assets/spulti/coin-back.svg';

// Matches the animations below
const FLIP_TIME = 3000;

const props = defineProps({
    // spulti's tap color of the winner
    color: {
        type: String,
        required: true,
    },
    // ms since the flip started, FLIP_TIME or more = already landed (no animation)
    elapsed: {
        type: Number,
        // FLIP_TIME, defineProps can't use local consts
        default: 3000,
    },
});

const emit = defineEmits(['landed']);

const landed = ref(props.elapsed >= FLIP_TIME);
let timer = null;
onMounted(() => {
    if (landed.value) return;
    timer = setTimeout(() => {
        landed.value = true;
        emit('landed');
    }, FLIP_TIME - props.elapsed);
});
onUnmounted(() => clearTimeout(timer));
</script>

<style lang="scss" scoped>
.screen-coin {
    position: relative;
    // Sized by the parent, aspect-ratio is newer than older OBS browser sources
    width: 100%;
    height: 100%;
    border-radius: 50%;
    box-shadow: 0 0 0 0.4vw var(--ring);
    transition: box-shadow 0.3s;

    &.flipping {
        // The hop, separate from the spin so the spin keeps one direction
        animation: hop 3s ease-in-out both;
        animation-delay: var(--skip);

        & .spin {
            animation: spin 3s cubic-bezier(0.2, 0.6, 0.35, 1) both;
            animation-delay: var(--skip);
        }
    }

    & .spin {
        position: absolute;
        inset: 0;
        transform-style: preserve-3d;
    }

    & .face {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;
        backface-visibility: hidden;

        &.back {
            transform: rotateX(180deg);
        }
    }
}

// 6 turns, lands face up
@keyframes spin {
    from {
        transform: perspective(60vw) rotateX(0deg);
    }
    to {
        transform: perspective(60vw) rotateX(2160deg);
    }
}
@keyframes hop {
    0%,
    100% {
        transform: translateY(0);
    }
    35% {
        transform: translateY(-4vw);
    }
    70% {
        transform: translateY(0);
    }
    82% {
        transform: translateY(-0.8vw);
    }
}
</style>
