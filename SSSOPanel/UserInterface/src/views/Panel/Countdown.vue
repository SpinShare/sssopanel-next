<template>
    <AppLayout title="Countdown">
        <SpinInput
            label="Active"
            hint="Shows a countdown"
            type="horizontal"
        >
            <SpinSwitch v-model="countdownActive" />
        </SpinInput>

        <transition
            name="promo"
            mode="out-in"
        >
            <SpinInput
                v-if="countdownActive"
                label="Time"
                hint="Final time for the countdown"
                type="horizontal"
            >
                <input
                    type="datetime-local"
                    v-model="countdownTime"
                />
            </SpinInput>
        </transition>

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
import { onMounted, ref, inject, onUnmounted } from 'vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSwitch from '@/components/Common/SpinSwitch.vue';
const emitter = inject('emitter');

const countdownActive = ref(false);
const countdownTime = ref(new Date());

const transition = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-set',
            data: {
                countdown: {
                    active: countdownActive.value,
                    time: countdownTime.value,
                },
            },
        }),
    );

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'countdown',
            },
        }),
    );
};

onMounted(() => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-get',
        }),
    );

    emitter.on('state-get-response', (state) => {
        countdownActive.value = state?.countdown?.active ?? countdownActive.value;
        countdownTime.value = state?.countdown?.time ?? countdownTime.value;
    });
});

onUnmounted(() => {
    emitter.off('state-get-response');
});
</script>

<style lang="scss" scoped></style>
