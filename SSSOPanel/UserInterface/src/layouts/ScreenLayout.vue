<template>
    <AspectLayout>
        <main class="layout-screen">
            <slot />

            <ScreenFooter />
        </main>
    </AspectLayout>
</template>

<script setup>
import { inject, onMounted, onUnmounted } from 'vue';
import router from '@/router';
import AspectLayout from '@/layouts/AspectLayout.vue';
import ScreenFooter from '@/components/ScreenFooter/ScreenFooter.vue';

const emitter = inject('emitter');

onMounted(() => {
    emitter.on('screen-navigate-response', (data) => {
        router.push({
            path: data.Path,
            query: data.Query ?? '',
            params: data.Parameters ?? '',
        });
    });

    emitter.on('current-route-get-response', (data) => {
        if (data.Path == null) return;
        router.push({
            path: data.Path,
            query: data.Query ?? '',
            params: data.Parameters ?? '',
        });
    });

    window.external.sendMessage(
        JSON.stringify({
            command: 'current-route-get',
        }),
    );
});

onUnmounted(() => {
    emitter.off('screen-navigate-response');
    emitter.off('current-route-get-response');
});
</script>

<style lang="scss">
.layout-screen {
    width: 100%;
    height: 100%;
    display: grid;
    grid-template-rows: 1fr auto;
    background: #222;
    font-family: 'Orbitron', sans-serif;
}
</style>
