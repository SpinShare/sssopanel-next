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
            command: 'screen-navigate',
            data: {
                path: 'commentators',
                params: {},
                query: {},
                richData: {
                    roomId: roomId.value,
                    roomPassword: roomPassword.value,
                },
            },
        }),
    );
};

onMounted(() => {
    emitter.on('current-route-get-response', (data) => {
        roomId.value = data?.RichData?.roomId ?? '';
        roomPassword.value = data?.RichData?.roomPassword ?? '';
    });
});

onUnmounted(() => {
    emitter.off('current-route-get-response');
});
</script>

<style lang="scss" scoped></style>
