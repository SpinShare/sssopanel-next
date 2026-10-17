<template>
    <AppLayout title="Settings">
        <template v-if="settingsState !== SETTING_STATE_LOADING">
            <SpinInputGroup header="General">
                <SpinInput
                    label="SSSOPanel Next"
                    hint="Version 1.3.0"
                    type="horizontal"
                >
                    <SpinButton
                        label="Updates"
                        @click="openGithubReleases"
                    />
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
                            { icon: 'flower', label: 'Spring', value: 'spring' },
                            { icon: 'white-balance-sunny', label: 'Summer', value: 'summer' },
                            { icon: 'leaf-maple', label: 'Autumn', value: 'autumn' },
                        ]"
                    />
                </SpinInput>
                <SpinInput
                    type="horizontal"
                    label="Event Year"
                    :hint="logoYears(theme).length ? 'Logo on Countdown and End (Stream)' : 'No logo for this season yet'"
                >
                    <SpinSelect
                        :disabled="settingsState === SETTING_STATE_SAVING || !logoYears(theme).length"
                        v-model="eventYear"
                        :options="eventYearOptions"
                    />
                </SpinInput>
            </SpinInputGroup>
            <SpinInputGroup header="start.gg">
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
            </SpinInputGroup>
            <SpinInputGroup header="OBS">
                <SpinInput
                    label="Scene Switching"
                    hint="Switch OBS scenes together with screens"
                    type="horizontal"
                >
                    <SpinSwitch
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        v-model="obsEnabled"
                    />
                </SpinInput>
                <template v-if="obsEnabled">
                    <SpinInput
                        label="Address"
                        hint="obs-websocket"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="text"
                            v-model="obsUrl"
                            placeholder="ws://localhost:4455"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Password"
                        hint="obs-websocket"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="password"
                            v-model="obsPassword"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Connection"
                        :hint="obsStatus"
                        type="horizontal"
                    >
                        <SpinButton
                            label="Connect"
                            :disabled="obsConnecting"
                            @click="connectObs"
                        />
                    </SpinInput>
                    <SpinInput
                        v-for="screen in obsScreens"
                        :key="screen.key"
                        :label="screen.label"
                        hint="OBS Scene"
                        type="horizontal"
                    >
                        <SpinSelect
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            v-model="obsSceneMapping[screen.key]"
                            :options="obsSceneOptions(screen.key)"
                        />
                    </SpinInput>
                </template>
            </SpinInputGroup>
            <SpinInputGroup header="Spin Rhythm XD">
                <SpinInput
                    label="Game Connection"
                    hint="Live chart, players and scores from the game"
                    type="horizontal"
                >
                    <SpinSwitch
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        v-model="srxdEnabled"
                    />
                </SpinInput>
                <template v-if="srxdEnabled">
                    <SpinInput
                        label="Address"
                        hint="Spin Rhythm XD websocket"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="text"
                            v-model="srxdUrl"
                            placeholder="ws://localhost:6767"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Connection"
                        :hint="srxdStatus"
                        type="horizontal"
                    >
                        <SpinButton
                            :label="game.connected ? 'Reconnect' : 'Connect'"
                            @click="connectSrxd"
                        />
                    </SpinInput>
                </template>
            </SpinInputGroup>
            <SpinInputGroup header="Music">
                <SpinInput
                    label="Source"
                    hint="Where the current song is read from"
                    type="horizontal"
                >
                    <SpinSelect
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        v-model="musicSource"
                        :options="[
                            { icon: 'spotify', label: 'Spotify', value: 'spotify' },
                            { icon: 'fish', label: 'Tuna (OBS plugin)', value: 'tuna' },
                        ]"
                    />
                </SpinInput>
                <template v-if="musicSource === 'tuna'">
                    <SpinInput
                        label="Tuna Address"
                        hint="Webserver of the Tuna OBS plugin"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="text"
                            v-model="tunaUrl"
                            placeholder="http://localhost:1608"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Album Cover"
                        hint="Show the cover instead of the music icon"
                        type="horizontal"
                    >
                        <SpinSwitch
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            v-model="tunaShowCover"
                        />
                    </SpinInput>
                </template>
            </SpinInputGroup>
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
import SpinSwitch from '@/components/Common/SpinSwitch.vue';
import { computed, onMounted, ref, inject, onUnmounted, watch } from 'vue';
import useSrxd from '@/modules/useSrxd';
import { logoYears } from '@/modules/eventLogos';
import useTournamentAPI from '@/modules/useStartGGApi';
import SpinLoader from '@/components/Common/SpinLoader.vue';
import router from '@/router';
import SpinInputGroup from '@/components/Common/SpinInputGroup.vue';
const emitter = inject('emitter');
const game = useSrxd();

const SETTING_STATE_LOADING = 'loading';
const SETTING_STATE_SAVING = 'saving';
const SETTING_STATE_IDLE = 'idle';
const settingsState = ref(SETTING_STATE_LOADING);

const theme = ref('spring');
const eventYear = ref('');

// Only years that have a logo for the picked season, empty follows the newest one
const eventYearOptions = computed(() => {
    const years = logoYears(theme.value);
    return [{ icon: 'calendar-star', label: years.length ? `Latest (${years[0]})` : 'None', value: '' }, ...years.map((year) => ({ icon: 'calendar', label: year, value: year }))];
});

watch(theme, () => {
    if (eventYear.value && !logoYears(theme.value).includes(eventYear.value)) eventYear.value = '';
});

const startGGApiToken = ref('');
const startGGTournamentSlug = ref('');
const startGGTournamentId = ref(null);
const startGGTournamentName = ref('');
const startGGEvents = ref([]);
const startGGEventId = ref('-1');

const obsScreens = [
    { key: 'init', label: 'Init' },
    { key: 'countdown', label: 'Countdown' },
    { key: 'brackets', label: 'Brackets' },
    { key: 'match-overview', label: 'Match (Overview)' },
    { key: 'match-ingame', label: 'Match (Ingame)' },
    { key: 'commentators', label: 'Commentators' },
    { key: 'end-stream', label: 'End (Stream)' },
    { key: 'end-tournament', label: 'End (Tournament)' },
];

const obsEnabled = ref(false);
const obsUrl = ref('ws://localhost:4455');
const obsPassword = ref('');
const obsSceneMapping = ref(Object.fromEntries(obsScreens.map((screen) => [screen.key, ''])));
const obsScenes = ref([]);
const obsStatus = ref('Not connected');
const obsConnecting = ref(false);

const srxdEnabled = ref(true);
const srxdUrl = ref('ws://localhost:6767');
const srxdStatus = computed(() => {
    if (!game.value.enabled) return 'Disabled';
    if (game.value.connected) return 'Connected';
    return game.value.error || 'Not connected, is the game running?';
});

const musicSource = ref('spotify');
const tunaUrl = ref('http://localhost:1608');
const tunaShowCover = ref(true);

const obsSceneOptions = (screenKey) => {
    // Keep a saved scene selectable while OBS is not connected
    const scenes = [...obsScenes.value];
    const mappedScene = obsSceneMapping.value[screenKey];
    if (mappedScene && !scenes.includes(mappedScene)) scenes.push(mappedScene);

    return [{ icon: 'border-none-variant', label: 'None', value: '' }, ...scenes.map((scene) => ({ icon: 'movie-open', label: scene, value: scene }))];
};

const connectObs = () => {
    obsConnecting.value = true;
    obsStatus.value = 'Connecting...';

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'obs-connect',
            data: {
                enabled: obsEnabled.value,
                url: obsUrl.value,
                password: obsPassword.value,
            },
        }),
    );
};

const connectSrxd = () => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'srxd-connect',
            data: {
                enabled: srxdEnabled.value,
                url: srxdUrl.value,
            },
        }),
    );
};

const loadStartGGEvents = async () => {
    startGGTournamentId.value = null;
    startGGTournamentName.value = '';
    startGGEvents.value = [];

    if (startGGApiToken.value === '') return;

    try {
        const { loadTournamentEvents } = useTournamentAPI(startGGApiToken.value);
        const tournament = await loadTournamentEvents(startGGTournamentSlug.value);

        startGGTournamentId.value = tournament.id;
        startGGTournamentName.value = tournament.name;
        startGGEvents.value = tournament.events.map((event) => ({
            value: event.id.toString(),
            label: event.name,
            icon: 'tournament',
        }));
    } catch (e) {
        console.error(e.message);
    }
};

const saveSettings = () => {
    console.log('Saving Settings');
    settingsState.value = SETTING_STATE_SAVING;

    const settings = [{ key: 'theme', value: theme.value }, { key: 'eventYear', value: eventYear.value }, { key: 'currentEvent.startgg.apiToken', value: startGGApiToken.value }, { key: 'currentEvent.startgg.tournamentSlug', value: startGGTournamentSlug.value }, { key: 'currentEvent.startgg.tournamentId', value: startGGTournamentId.value }, { key: 'currentEvent.startgg.tournamentName', value: startGGTournamentName.value }, { key: 'currentEvent.startgg.eventId', value: startGGEventId.value }, { key: 'obs.enabled', value: obsEnabled.value }, { key: 'obs.url', value: obsUrl.value }, { key: 'obs.password', value: obsPassword.value }, { key: 'srxd.enabled', value: srxdEnabled.value }, { key: 'srxd.url', value: srxdUrl.value }, { key: 'music.source', value: musicSource.value }, { key: 'tuna.url', value: tunaUrl.value }, { key: 'tuna.showCover', value: tunaShowCover.value }, ...obsScreens.map((screen) => ({ key: `obs.scene.${screen.key}`, value: obsSceneMapping.value[screen.key] }))];

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'settings-set',
            data: settings,
        }),
    );
};

const openGithubReleases = () => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'open-in-browser',
            data: 'https://github.com/SpinShare/sssopanel-next',
        }),
    );
};

onMounted(() => {
    emitter.on('settings-get-full-response', async (settings) => {
        theme.value = settings['theme'] || 'spring';
        eventYear.value = settings['eventYear'] ? String(settings['eventYear']) : '';

        startGGApiToken.value = settings['currentEvent.startgg.apiToken'] ?? '';
        startGGTournamentSlug.value = settings['currentEvent.startgg.tournamentSlug'] ?? '';
        startGGTournamentId.value = settings['currentEvent.startgg.tournamentId'] ?? null;
        startGGTournamentName.value = settings['currentEvent.startgg.tournamentName'] ?? '';
        startGGEventId.value = settings['currentEvent.startgg.eventId'] ?? '-1';

        obsEnabled.value = settings['obs.enabled'] ?? false;
        obsUrl.value = settings['obs.url'] || 'ws://localhost:4455';
        obsPassword.value = settings['obs.password'] ?? '';
        srxdEnabled.value = settings['srxd.enabled'] ?? true;
        srxdUrl.value = settings['srxd.url'] || 'ws://localhost:6767';
        musicSource.value = settings['music.source'] ?? 'spotify';
        tunaUrl.value = settings['tuna.url'] || 'http://localhost:1608';
        tunaShowCover.value = settings['tuna.showCover'] ?? true;
        obsScreens.forEach((screen) => {
            obsSceneMapping.value[screen.key] = settings[`obs.scene.${screen.key}`] ?? '';
        });

        if (startGGApiToken.value !== '' && startGGTournamentSlug.value !== '') {
            await loadStartGGEvents();
        }

        settingsState.value = SETTING_STATE_IDLE;
    });
    emitter.on('settings-set-response', () => {
        settingsState.value = SETTING_STATE_IDLE;
    });
    emitter.on('obs-status-get-response', (status) => {
        obsConnecting.value = false;
        obsScenes.value = status.scenes;
        obsStatus.value = status.connected ? 'Connected' : status.error || 'Not connected';
    });

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'settings-get-full',
            data: '',
        }),
    );
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'obs-status-get',
        }),
    );
});

onUnmounted(() => {
    emitter.off('settings-get-full-response');
    emitter.off('settings-set-response');
    emitter.off('obs-status-get-response');
});
</script>

<style lang="scss" scoped></style>
