<template>
    <AspectLayout>
        <main class="layout-screen" :style="{ gridTemplateRows }">
            <ScreenVCHeader v-if="showHeader" />

            <slot />

            <ScreenFooter />
        </main>
    </AspectLayout>
</template>

<script setup>
import { inject, onMounted, onUnmounted, computed } from 'vue';
import { useRoute } from 'vue-router';
import router from '@/router';
import AspectLayout from '@/layouts/AspectLayout.vue';
import ScreenFooter from '@/components/ScreenFooter/ScreenFooter.vue';
import ScreenVCHeader from '../components/ScreenVCHeader/ScreenVCHeader.vue';

const emitter = inject('emitter');
const route = useRoute();

const showHeader = computed(() => {
    return route.name !== 'ViewScreenCountdown';
});

const gridTemplateRows = computed(() => {
    return showHeader.value ? 'auto 1fr auto' : '1fr auto';
});

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
    //grid-template-rows: auto 1fr auto;
    background: #222;
    font-family: 'Orbitron', sans-serif;
}
</style>
