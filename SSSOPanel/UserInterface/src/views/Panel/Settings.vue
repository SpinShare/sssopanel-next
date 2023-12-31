<template>
    <AppLayout title="Settings">
        <SpinInput
            label="SSSOPanel Next"
            hint="Version 1.0.0"
            type="horizontal"
        >
            <SpinButton label="Updates" />
        </SpinInput>
        <SpinInput
            label="Token"
            hint="start.gg"
            type="horizontal"
        >
            <input
                type="text"
                v-model="startGGApiToken"
                @input="resetSettingsTimeout"
            />
        </SpinInput>
        <SpinInput
            label="Slug"
            hint="Tournament"
            type="horizontal"
            v-if="startGGApiToken !== ''"
        >
            <input
                type="text"
                v-model="startGGTournamentSlug"
                placeholder="spinshare-speenopen-winter-2024"
                @input="resetSettingsTimeout"
                @change="loadStartGGEvents"
            />
        </SpinInput>
        <SpinInput
            v-if="startGGTournamentId !== null"
            :label="startGGTournamentName"
            :hint="`ID: ${startGGTournamentId}`"
        />
        <SpinInput
            type="horizontal"
            label="Event"
            v-if="
                startGGApiToken !== '' &&
                startGGTournamentSlug !== '' &&
                startGGEvents.length !== 0
            "
        >
            <SpinSelect
                v-model="startGGEventId"
                :options="[
                    { icon: 'border-none-variant', label: 'None', value: '-1' },
                    ...startGGEvents,
                ]"
                @update:modelValue="resetSettingsTimeout"
            />
        </SpinInput>
        <SpinInput
            type="horizontal"
            label="Players"
            hint="Map start.gg players with SpinShare profiles"
            v-if="
                startGGApiToken !== '' &&
                startGGTournamentSlug !== '' &&
                startGGEventId !== '-1'
            "
        >
            <SpinButton label="Open Mapping" />
        </SpinInput>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import { ref } from 'vue';
import useTournamentAPI from '@/modules/useStartGGApi';

const startGGApiToken = ref('');
const startGGTournamentSlug = ref('');
const startGGTournamentId = ref(null);
const startGGTournamentName = ref('');
const startGGEvents = ref([]);
const startGGEventId = ref('-1');

const loadStartGGEvents = async () => {
    startGGTournamentId.value = null;
    startGGTournamentName.value = '';
    startGGEvents.value = [];

    const { loadTournamentEvents } = useTournamentAPI(startGGApiToken.value);
    const tournament = await loadTournamentEvents(startGGTournamentSlug.value);

    startGGTournamentId.value = tournament.id;
    startGGTournamentName.value = tournament.name;
    startGGEvents.value = tournament.events.map((event) => ({
        value: event.id.toString(),
        label: event.name,
        icon: 'tournament',
    }));
};

let saveSettingsTimeout = null;
const saveSettings = () => {
    console.log('Saving Settings');
};
const resetSettingsTimeout = () => {
    if (saveSettingsTimeout !== null) clearTimeout(saveSettingsTimeout);

    saveSettingsTimeout = setTimeout(saveSettings, 2000);
};
</script>

<style lang="scss" scoped></style>
