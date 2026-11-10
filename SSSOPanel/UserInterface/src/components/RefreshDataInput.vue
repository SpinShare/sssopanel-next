<template>
    <SpinInput
        label="Refresh Data"
        :hint="hint"
        type="horizontal"
    >
        <SpinButton
            icon="refresh"
            label="Refresh"
            :loading="loading"
            :disabled="loading"
            @click="refresh"
        />
    </SpinInput>
</template>

<script setup>
import { inject, ref } from 'vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import { refreshAll } from '@/modules/refreshData';

const props = defineProps({
    // The followed spulti match, its players' SpinShare ids fill in missing ones
    spultiMatch: {
        type: Object,
        default: null,
    },
});

const emitter = inject('emitter');
const loading = ref(false);
const hint = ref('Fetch names from start.gg, profiles (name, pronouns, avatar) and the chart from SpinShare again');

const refresh = async () => {
    loading.value = true;
    try {
        hint.value = await refreshAll(emitter, props.spultiMatch);
    } catch (error) {
        hint.value = `Refresh failed: ${error.message}`;
    } finally {
        loading.value = false;
    }
};
</script>
