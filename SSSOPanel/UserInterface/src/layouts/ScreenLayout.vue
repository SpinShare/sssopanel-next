<template>
    <AspectLayout :transparent="props.transparent">
        <main
            class="layout-screen"
            :class="{ transparent: props.transparent }"
            :style="{ gridTemplateRows }"
        >
            <ScreenVCHeader v-if="showHeader" />
            <ScreenRefDen />

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
import ScreenRefDen from '../components/ScreenRefDen/ScreenRefDen.vue';

const props = defineProps({
    transparent: {
        type: Boolean,
        default: false,
    },
});

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

    window.electronAPI.send(
        'message',
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

    &.transparent {
        background: transparent;
    }
}
</style>
