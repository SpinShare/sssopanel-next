<template>
    <AppLayout title="Commentators">
        <SpinInputGroup header="vdo.ninja">
            <SpinInput
                label="Room ID"
                hint="vdo.ninja"
                type="horizontal"
            >
                <input
                    v-model="roomId"
                    type="text"
                />
            </SpinInput>
            <SpinInput
                label="Password"
                hint="vdo.ninja"
                type="horizontal"
            >
                <input
                    v-model="roomPassword"
                    type="text"
                />
            </SpinInput>
        </SpinInputGroup>

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
import SpinInputGroup from '@/components/Common/SpinInputGroup.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import { onMounted, onUnmounted, ref, inject } from 'vue';
const emitter = inject('emitter');

const roomId = ref('');
const roomPassword = ref('');

const transition = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-set',
            data: {
                commentators: {
                    roomId: roomId.value,
                    roomPassword: roomPassword.value,
                },
            },
        }),
    );

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'commentators',
            },
        }),
    );
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        roomId.value = state?.commentators?.roomId ?? roomId.value;
        roomPassword.value = state?.commentators?.roomPassword ?? roomPassword.value;
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
