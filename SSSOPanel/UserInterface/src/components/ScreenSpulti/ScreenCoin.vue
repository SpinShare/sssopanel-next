<template>
    <!-- spulti's coin: spectators always see it land waveform up, the ring shows the winner's color once it lands -->
    <div
        class="screen-coin"
        :class="{ flipping: !landed }"
        :style="{ '--ring': landed ? color : 'transparent', '--skip': `${-Math.min(elapsed, FLIP_TIME)}ms`, '--flip': `${FLIP_TIME}ms` }"
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
import { COIN_FLIP_TIME as FLIP_TIME } from '@/modules/useSpultiOverlays';

const props = defineProps({
    // spulti's tap color of the winner
    color: {
        type: String,
        required: true,
    },
    // ms since the flip started, FLIP_TIME or more = already landed (no animation), negative = starts that much later
    elapsed: {
        type: Number,
        // Already landed, defineProps can't use imports
        default: 999999,
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
// Port of spulti's CoinToss: perspective on the hopping wrapper (a perspective() inside the spin's keyframes
// warped the coin mid turn), one hop, 5 turns in one direction
.screen-coin {
    position: relative;
    // Sized by the parent, aspect-ratio is newer than older OBS browser sources
    width: 100%;
    height: 100%;
    border-radius: 50%;
    box-shadow: 0 0 0 0.4vw var(--ring);
    transition: box-shadow 0.3s;
    perspective: 45vw;

    &.flipping {
        animation: hop var(--flip) both;
        animation-delay: var(--skip);

        & .spin {
            animation: spin var(--flip) cubic-bezier(0.2, 0.6, 0.4, 1) both;
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

// Lands waveform up, spectators have no side
@keyframes spin {
    from {
        transform: rotateX(0deg);
    }
    to {
        transform: rotateX(1800deg);
    }
}
@keyframes hop {
    0%,
    100% {
        transform: translateY(0);
        animation-timing-function: ease-out;
    }
    50% {
        transform: translateY(-2.5vw);
        animation-timing-function: ease-in;
    }
}
</style>
