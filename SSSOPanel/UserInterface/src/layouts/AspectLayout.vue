<template>
    <section
        class="layout-aspect"
        ref="screen"
    >
        <slot />
    </section>
</template>

<script setup>
import { onMounted, ref } from 'vue';
const screen = ref(null);

const updateScreen = () => {
    let width = window.innerWidth;
    let height = window.innerHeight;
    let aspectRatio = width / height;

    if (aspectRatio >= 16 / 9) {
        screen.value.style.width = `${(height * 16) / 9}px`;
        screen.value.style.height = `${height}px`;
    } else {
        screen.value.style.width = `${width}px`;
        screen.value.style.height = `${(width * 9) / 16}px`;
    }

    screen.value.style.fontSize = `${width * 0.015}px`;
};

onMounted(() => {
    updateScreen();
    window.addEventListener('resize', updateScreen);
});
</script>

<style lang="scss">
.layout-aspect {
    background: #222;
    display: grid;
    overflow: hidden;
    align-self: center;
    justify-self: center;
    font-family: 'Orbitron', sans-serif;
}
</style>
