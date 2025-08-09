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
import { onMounted, ref, inject, onUnmounted } from 'vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSwitch from '@/components/Common/SpinSwitch.vue';
const emitter = inject('emitter');
const refDenIsVisible = ref(false);
const countdownActive = ref(false);
const countdownTime = ref(new Date());

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
    console.log('[PANEL-COUNTDOWN] refDenIsVisible toggled:', refDenIsVisible.value);
};

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
    emitter.on('state-get-response', (state) => {
        countdownActive.value = state?.countdown?.active ?? countdownActive.value;
        countdownTime.value = state?.countdown?.time ?? countdownTime.value;
        refDenIsVisible.value = state?.refDenIsVisible ?? false;
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
