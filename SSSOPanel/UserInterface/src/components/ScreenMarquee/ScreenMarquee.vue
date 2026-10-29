<template>
    <div
        ref="viewport"
        class="marquee"
        :class="{ scrolling: overflowing, 'overlap-start': overlapStart }"
    >
        <div
            ref="track"
            class="track"
        >
            <span
                ref="content"
                class="content"
                ><slot
            /></span>
            <span
                v-if="overflowing"
                class="content"
                aria-hidden="true"
                ><slot
            /></span>
        </div>
    </div>
</template>

<script setup>
import { nextTick, onMounted, onUnmounted, ref } from 'vue';

defineProps({
    // Pulls the left fade under whatever sits before it (like an album cover), so the text doesn't start with a gap
    overlapStart: Boolean,
});

// Pixels per second, so long and short texts scroll at the same speed
const SPEED = 60;
// Rests at the start every time it comes around
const PAUSE = 3000;

const viewport = ref(null);
const track = ref(null);
const content = ref(null);
const overflowing = ref(false);

let animation = null;
let observer = null;

const update = async () => {
    if (!viewport.value || !content.value) return;
    // Without the fade padding, otherwise it would never fit again once it scrolls.
    // With overlap-start the left padding is always there and sits under the margin, so it counts
    const style = getComputedStyle(content.value);
    const overlap = viewport.value.classList.contains('overlap-start');
    const width = content.value.offsetWidth - (overlap ? 0 : parseFloat(style.paddingLeft)) - parseFloat(style.paddingRight);
    const fits = width <= viewport.value.clientWidth;

    animation?.cancel();
    animation = null;
    overflowing.value = !fits;
    if (fits) return;

    await nextTick();
    // Another update may have run meanwhile and found that it fits after all
    if (!overflowing.value || animation || !track.value?.children[1]) return;
    // One copy plus the gap, the second copy then sits exactly where the first started
    const distance = track.value.children[1].offsetLeft - track.value.children[0].offsetLeft;
    const scrollTime = (distance / SPEED) * 1000;
    const duration = PAUSE + scrollTime;
    animation = track.value.animate(
        [
            { transform: 'translateX(0)', offset: 0 },
            { transform: 'translateX(0)', offset: PAUSE / duration },
            { transform: `translateX(-${distance}px)`, offset: 1 },
        ],
        { duration, iterations: Infinity, easing: 'linear' },
    );
};

onMounted(() => {
    // Fonts loading late or the footer resizing changes whether it fits
    observer = new ResizeObserver(() => update());
    observer.observe(viewport.value);
    observer.observe(content.value);
});

onUnmounted(() => {
    observer?.disconnect();
    animation?.cancel();
});
</script>

<style lang="scss" scoped>
.marquee {
    --fade: 1.5vw;
    min-width: 0;
    overflow: hidden;

    &.scrolling {
        // Older obs builds ship a chromium that only knows the prefixed one
        -webkit-mask-image: linear-gradient(to right, transparent, #000 var(--fade), #000 calc(100% - var(--fade)), transparent);
        mask-image: linear-gradient(to right, transparent, #000 var(--fade), #000 calc(100% - var(--fade)), transparent);

        // At rest the text starts past the fade, and the gap keeps the copies out of each other's fade
        & .content {
            padding: 0 var(--fade);
        }
    }

    // Same left side scrolling or not, otherwise it could flip between the two
    &.overlap-start {
        margin-left: calc(-1 * var(--fade));

        & .content {
            padding-left: var(--fade);
        }
    }

    & .track {
        display: flex;
        width: max-content;
    }
    & .content {
        display: flex;
        align-items: center;
        gap: 0.75vw;
        white-space: nowrap;
        flex-shrink: 0;
    }
}
</style>
