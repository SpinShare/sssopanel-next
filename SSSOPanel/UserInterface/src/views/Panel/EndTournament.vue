<template>
    <AppLayout title="End (Tournament)">
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
import useRefDenToggle from '@/modules/useRefDenToggle';
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
const { refDenIsVisible, toggleRefDen } = useRefDenToggle();

const transition = () => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'end-tournament',
                params: {},
                query: {},
            },
        }),
    );
};
</script>

<style lang="scss" scoped></style>
