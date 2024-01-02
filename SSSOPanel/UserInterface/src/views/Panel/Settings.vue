<template>
    <AppLayout title="Settings">
        <template v-if="settingsState !== SETTING_STATE_LOADING">
            <div class="group">
                <header>General</header>
                <SpinInput
                    label="SSSOPanel Next"
                    hint="Version 1.0.0"
                    type="horizontal"
                >
                    <SpinButton label="Updates" />
                </SpinInput>
                <SpinInput
                    type="horizontal"
                    label="Theme"
                >
                    <SpinSelect
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        v-model="theme"
                        :options="[
                            { icon: 'snowflake', label: 'Winter', value: 'winter' },
                            { icon: 'white-balance-sunny', label: 'Summer', value: 'summer' },
                        ]"
                    />
                </SpinInput>
            </div>
            <div class="group">
                <header>start.gg</header>
                <SpinInput
                    label="Token"
                    hint="start.gg"
                    type="horizontal"
                >
                    <input
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        type="text"
                        v-model="startGGApiToken"
                    />
                </SpinInput>
                <SpinInput
                    label="Slug"
                    hint="Tournament"
                    type="horizontal"
                    v-if="startGGApiToken !== ''"
                >
                    <input
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        type="text"
                        v-model="startGGTournamentSlug"
                        placeholder="spinshare-speenopen-winter-2024"
                        @change="loadStartGGEvents"
                    />
                </SpinInput>
                <SpinInput
                    type="horizontal"
                    v-if="startGGTournamentId !== null"
                    :label="startGGTournamentName"
                    :hint="`ID: ${startGGTournamentId}`"
                />
                <SpinInput
                    type="horizontal"
                    label="Event"
                    v-if="startGGApiToken !== '' && startGGTournamentSlug !== '' && startGGEvents.length !== 0"
                >
                    <SpinSelect
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        v-model="startGGEventId"
                        :options="[{ icon: 'border-none-variant', label: 'None', value: '-1' }, ...startGGEvents]"
                    />
                </SpinInput>
                <SpinInput
                    type="horizontal"
                    label="Players"
                    hint="Map start.gg players with SpinShare profiles"
                    v-if="startGGApiToken !== '' && startGGTournamentSlug !== '' && startGGEventId !== '-1'"
                >
                    <SpinButton
                        label="Open Mapping"
                        @click="router.push({ path: '/panel/player-mapping' })"
                    />
                </SpinInput>
            </div>
        </template>
        <template v-if="settingsState === SETTING_STATE_LOADING">
            <SpinLoader />
        </template>

        <template #transition>
            <SpinButton
                color="bright"
                icon="content-save"
                :disabled="settingsState !== SETTING_STATE_IDLE"
                @click="saveSettings"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import { onMounted, ref, inject } from 'vue';
import useTournamentAPI from '@/modules/useStartGGApi';
import SpinLoader from '@/components/Common/SpinLoader.vue';
import router from '@/router';
const emitter = inject('emitter');

const SETTING_STATE_LOADING = 'loading';
const SETTING_STATE_SAVING = 'saving';
const SETTING_STATE_IDLE = 'idle';
const settingsState = ref(SETTING_STATE_LOADING);

const theme = ref('winter');

const startGGApiToken = ref('');
const startGGTournamentSlug = ref('');
const startGGTournamentId = ref(null);
const startGGTournamentName = ref('');
const startGGEvents = ref([]);
const startGGEventId = ref('-1');

onMounted(() => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'settings-get-full',
            data: '',
        }),
    );
});
emitter.on('settings-get-full-response', async (settings) => {
    theme.value = settings['theme'] ?? 'winter';

    startGGApiToken.value = settings['currentEvent.startgg.apiToken'] ?? '';
    startGGTournamentSlug.value = settings['currentEvent.startgg.tournamentSlug'] ?? '';
    startGGTournamentId.value = settings['currentEvent.startgg.tournamentId'] ?? null;
    startGGTournamentName.value = settings['currentEvent.startgg.tournamentName'] ?? '';
    startGGEventId.value = settings['currentEvent.startgg.eventId'] ?? '-1';
    if (startGGApiToken.value !== '' && startGGTournamentSlug.value !== '') {
        await loadStartGGEvents();
    }

    settingsState.value = SETTING_STATE_IDLE;
});

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

const saveSettings = () => {
    console.log('Saving Settings');
    settingsState.value = SETTING_STATE_SAVING;

    const settings = [
        { key: 'theme', value: theme.value },
        { key: 'currentEvent.startgg.apiToken', value: startGGApiToken.value },
        { key: 'currentEvent.startgg.tournamentSlug', value: startGGTournamentSlug.value },
        { key: 'currentEvent.startgg.tournamentId', value: startGGTournamentId.value },
        { key: 'currentEvent.startgg.tournamentName', value: startGGTournamentName.value },
        { key: 'currentEvent.startgg.eventId', value: startGGEventId.value },
    ];

    window.external.sendMessage(
        JSON.stringify({
            command: 'settings-set',
            data: settings,
        }),
    );
};
emitter.on('settings-set-response', () => {
    settingsState.value = SETTING_STATE_IDLE;
});
</script>

<style lang="scss" scoped>
.group {
    display: grid;
    gap: 15px;

    &:not(:first-of-type) header {
        margin-top: 15px;
    }
    & header {
        font-size: 0.8rem;
        font-weight: bold;
        padding: 6px 20px;
        background: rgba(var(--colorBaseText), 0.8);
        color: rgb(var(--colorBase));
        margin: 0 -20px;
    }
}
</style>
