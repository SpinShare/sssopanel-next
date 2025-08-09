<template>
    <AppLayout title="Init">
        <template #transition>
            <SpinButton
                @click="toggleRefDen"
                :icon="refDenIsVisible ? 'comment-text-multiple' : 'comment-text-multiple-outline'"
                :color="refDenIsVisible ? 'primary' : 'default'"
                v-tooltip="'Toggle Ref Den'"
            />
            <SpinButton
                @click="transition"
                icon="record"
                color="primary"
                v-tooltip="'Transition to screen'"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import { inject, onMounted, ref } from 'vue';
const refDenIsVisible = ref(false);
const emitter = inject('emitter');

const transition = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: '',
                params: {},
                query: {},
            },
        }),
    );
};

const toggleRefDen = () => {
    refDenIsVisible.value = !refDenIsVisible.value;
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-set',
            data: {
                refDenIsVisible: refDenIsVisible.value,
            },
        }),
    );
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-get',
        }),
    );
    console.log('[PANEL-INIT] refDenIsVisible toggled:', refDenIsVisible.value);
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        refDenIsVisible.value = state?.refDenIsVisible ?? false;
    });
});
</script>

<style lang="scss" scoped></style>
