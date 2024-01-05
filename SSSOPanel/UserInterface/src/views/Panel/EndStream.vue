<template>
    <AppLayout title="End (Stream)">
        <SpinInput
            label="Date"
            hint="Date for next stream"
            type="horizontal"
        >
            <input
                type="date"
                v-model="nextStreamDate"
            />
        </SpinInput>

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
import SpinInput from '@/components/Common/SpinInput.vue';
import { ref, inject, onMounted, onUnmounted } from 'vue';
const emitter = inject('emitter');

const nextStreamDate = ref(new Date());

const transition = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-set',
            data: {
                endOfStream: {
                    nextStreamDate: nextStreamDate.value,
                },
            },
        }),
    );

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'end-stream',
            },
        }),
    );
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        nextStreamDate.value = state?.endOfStream?.nextStreamDate ?? nextStreamDate.value;
    });

    window.external.sendMessage(
        JSON.stringify({
            command: 'state-get',
        }),
    );
});

onUnmounted(() => {
    emitter.off('state-get-response');
});
</script>

<style lang="scss" scoped></style>
