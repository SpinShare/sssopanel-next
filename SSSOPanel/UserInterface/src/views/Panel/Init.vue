<template>
    <AppLayout title="Init">
        <template #transition>
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

const transition = () => {
    window.electronAPI.send(
        'message',
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
</script>

<style lang="scss" scoped></style>
