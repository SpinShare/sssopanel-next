<template>
    <!-- The window, the route transition scales this one. The stage inside gets the fit transform -->
    <section class="layout-aspect">
        <div
            class="stage"
            :class="{ transparent: props.transparent }"
            ref="screen"
        >
            <slot />
        </div>
    </section>
</template>

<script setup>
import { onMounted, ref } from 'vue';
const props = defineProps({
    transparent: {
        type: Boolean,
        default: false,
    },
});

const screen = ref(null);

// Everything on the screens is sized in vw, so the stage is laid out at the window's full width (16:9) and scaled
// down to fit when the window is wider than that. A letterboxed stage with window vw put things out of place, and the
// transform also anchors position: fixed children (ref den) to the stage instead of the window
const updateScreen = () => {
    const width = window.innerWidth;
    const height = window.innerHeight;
    const stageHeight = (width * 9) / 16;

    screen.value.style.width = `${width}px`;
    screen.value.style.height = `${stageHeight}px`;
    // Placed by hand, grid centering goes by the unscaled box and pushed the stage down
    const scale = Math.min(1, height / stageHeight);
    const x = (width - width * scale) / 2;
    const y = (height - stageHeight * scale) / 2;
    // None at 16:9 (OBS), a transform layer renders text a little differently
    screen.value.style.transform = Math.abs(x) < 0.5 && Math.abs(y) < 0.5 ? 'none' : `translate(${x}px, ${y}px) scale(${scale})`;
    screen.value.style.fontSize = `${width * 0.015}px`;
};

onMounted(() => {
    updateScreen();
    window.addEventListener('resize', updateScreen);
});
</script>

<style lang="scss">
.layout-aspect {
    position: absolute;
    inset: 0;
    overflow: hidden;

    & > .stage {
        background: #222;
        display: grid;
        overflow: hidden;
        position: absolute;
        left: 0;
        top: 0;
        transform-origin: 0 0;
        font-family: 'Orbitron', 'Noto Sans JP', sans-serif;

        &.transparent {
            background: transparent;
        }
    }
}
</style>
