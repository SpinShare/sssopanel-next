<template>
    <AppLayout title="Settings">
        <template v-if="settingsState !== SETTING_STATE_LOADING">
            <SpinInputGroup header="General">
                <SpinInput
                    label="SSSOPanel Next"
                    hint="Version 1.4.1"
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
                        :options="themeOptions"
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
            <SpinInputGroup header="Voice Bot">
                <SpinInput
                    label="Bot"
                    hint="PharahP, for the commentary header and ref den"
                    type="horizontal"
                >
                    <SpinSelect
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        v-model="voiceBotMode"
                        :options="[
                            { icon: 'cloud', label: 'Remote', value: 'remote' },
                            { icon: 'laptop', label: 'Local', value: 'local' },
                        ]"
                    />
                </SpinInput>
                <SpinInput
                    v-if="voiceBotMode === 'remote'"
                    label="Address"
                    hint="Hosted bot websocket"
                    type="horizontal"
                >
                    <input
                        :disabled="settingsState === SETTING_STATE_SAVING"
                        type="text"
                        v-model="voiceBotUrl"
                        placeholder="ws://207.180.220.125:8765"
                    />
                </SpinInput>
                <template v-else>
                    <SpinInput
                        label="Token"
                        hint="Your own bot app, needs Server Members + Message Content intents"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="password"
                            v-model="voiceBotToken"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Port"
                        hint="Local websocket"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="text"
                            v-model="voiceBotPort"
                            placeholder="8765"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Node"
                        hint="Optional, uses the bundled node when empty"
                        type="horizontal"
                    >
                        <input
                            :disabled="settingsState === SETTING_STATE_SAVING"
                            type="text"
                            v-model="voiceBotNodePath"
                            placeholder="node"
                        />
                    </SpinInput>
                    <SpinInput
                        label="Status"
                        :hint="voiceBotStatusText"
                        type="horizontal"
                    >
                        <div style="display: flex; gap: 10px">
                            <SpinButton
                                :label="voiceBotRunning ? 'Stop' : 'Start'"
                                :disabled="!voiceBotRunning && !voiceBotCanStart"
                                @click="toggleVoiceBot"
                            />
                            <SpinButton
                                icon="text-box-outline"
                                :color="showVoiceBotLog ? 'primary' : 'default'"
                                v-tooltip="showVoiceBotLog ? 'Hide Log' : 'Show Log'"
                                @click="showVoiceBotLog = !showVoiceBotLog"
                            />
                        </div>
                    </SpinInput>
                    <pre
                        v-if="showVoiceBotLog"
                        class="voicebot-log"
                        >{{ voiceBot.logs.join('\n') || 'No output yet' }}</pre
                    >
                    <template v-if="voiceBotControllable">
                        <SpinInput
                            label="Voice Channel"
                            :hint="joinedChannelName ? `In ${joinedChannelName}` : 'Not in a voice channel'"
                        >
                            <div style="display: flex; gap: 10px">
                                <SpinSelect
                                    class="channel-select"
                                    :disabled="voiceBotBusy"
                                    v-model="voiceChannelId"
                                    :options="voiceChannelOptions"
                                />
                                <SpinButton
                                    :label="joinedChannelId && joinedChannelId === voiceChannelId ? 'Leave' : 'Join'"
                                    :disabled="voiceBotBusy || !voiceChannelId"
                                    @click="toggleJoin"
                                />
                            </div>
                        </SpinInput>
                        <SpinInput
                            label="Ref Channel"
                            hint="Text channel shown in the ref den"
                        >
                            <SpinSelect
                                class="channel-select"
                                :disabled="voiceBotBusy || !selectedGuildId"
                                :model-value="refChannelId"
                                :options="refChannelOptions"
                                @update:model-value="setRefChannel"
                            />
                        </SpinInput>
                        <SpinInput
                            label="Auto Join"
                            hint="Join the voice channel when the bot starts"
                            type="horizontal"
                        >
                            <SpinSwitch
                                :disabled="settingsState === SETTING_STATE_SAVING"
                                v-model="voiceBotAutoJoin"
                            />
                        </SpinInput>
                        <SpinInput
                            v-if="voiceBotError"
                            label="Error"
                            :hint="voiceBotError"
                            type="horizontal"
                        />
                    </template>
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
import useVoiceBot from '@/modules/useVoiceBot';
import VoiceBotService from '@/modules/VoiceBotService';
import { getTheme, themes } from '@/modules/themes';
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

const theme = ref(getTheme().id);
const themeOptions = themes.map(({ id, name, icon }) => ({ icon, label: name, value: id }));

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
    { key: 'match-results', label: 'Match (Results)' },
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

const { status: voiceBot, bot: voiceBotConnection } = useVoiceBot();
const voiceBotMode = ref('local');
const voiceBotUrl = ref('');
const voiceBotToken = ref('');
const voiceBotPort = ref('8765');
const voiceBotNodePath = ref('');
const showVoiceBotLog = ref(false);
const voiceBotRunning = computed(() => voiceBot.value.status === 'starting' || voiceBot.value.status === 'running');
const voiceBotStatusText = computed(() => {
    if (voiceBot.value.mode !== 'local') return 'Save to switch to the local bot';
    switch (voiceBot.value.status) {
        case 'starting':
            return 'Logging in...';
        case 'running':
            return voiceBotConnection.botUsername ? `Running as ${voiceBotConnection.botUsername}` : 'Running';
        case 'crashed':
            return voiceBot.value.error || 'Crashed';
        default:
            return voiceBot.value.error || 'Stopped';
    }
});

// Start uses what's saved, not what's typed
const savedVoiceBot = ref({ mode: 'local', token: '' });
const voiceBotCanStart = computed(() => savedVoiceBot.value.mode === 'local' && savedVoiceBot.value.token !== '');
const applySavedVoiceBot = (settings) => {
    savedVoiceBot.value = { mode: settings['voicebot.mode'] === 'remote' ? 'remote' : 'local', token: (settings['voicebot.token'] ?? '').trim() };
};

// Channel controls, only the local bot takes commands
const voiceBotAutoJoin = ref(false);
const voiceChannelId = ref('');
const voiceBotGuilds = ref([]);
const voiceBotState = ref(null);
const voiceBotBusy = ref(false);
const voiceBotError = ref('');
const voiceBotControllable = computed(() => voiceBot.value.mode === 'local' && voiceBotConnection.isConnected && !!voiceBotConnection.botUsername && !!voiceBot.value.key);

const allVoiceChannels = computed(() => voiceBotGuilds.value.flatMap((guild) => guild.voice.map((channel) => ({ ...channel, guild }))));
const voiceChannelOptions = computed(() => allVoiceChannels.value.map((channel) => ({ icon: 'volume-high', label: channel.name, value: channel.id })));
const joinedChannelId = computed(() => voiceBotState.value?.voice?.[0]?.channel_id ?? '');
const joinedChannelName = computed(() => allVoiceChannels.value.find((channel) => channel.id === joinedChannelId.value)?.name ?? '');

// The ref channel belongs to the guild of the picked voice channel, or the only guild there is
const selectedGuildId = computed(() => allVoiceChannels.value.find((channel) => channel.id === voiceChannelId.value)?.guild.id ?? (voiceBotGuilds.value.length === 1 ? voiceBotGuilds.value[0].id : ''));
const refChannelId = computed(() => voiceBotState.value?.ref_channels?.[selectedGuildId.value] ?? '');
const refChannelOptions = computed(() => [{ icon: 'border-none-variant', label: 'None', value: '' }, ...(voiceBotGuilds.value.find((guild) => guild.id === selectedGuildId.value)?.text ?? []).map((channel) => ({ icon: 'pound', label: channel.name, value: channel.id }))]);

const runVoiceBot = async (action) => {
    voiceBotBusy.value = true;
    voiceBotError.value = '';
    try {
        const result = await action();
        if (result?.voice) voiceBotState.value = result;
    } catch (e) {
        voiceBotError.value = e.message;
    } finally {
        voiceBotBusy.value = false;
    }
};

const loadVoiceBotChannels = () =>
    runVoiceBot(async () => {
        voiceBotGuilds.value = (await VoiceBotService.listChannels()).guilds;
        const state = await VoiceBotService.status();
        if (state.voice?.[0]) voiceChannelId.value = state.voice[0].channel_id;
        return state;
    });

const toggleJoin = () =>
    runVoiceBot(async () => {
        if (joinedChannelId.value && joinedChannelId.value === voiceChannelId.value) {
            return VoiceBotService.leave();
        }
        const result = await VoiceBotService.join(voiceChannelId.value);
        // Remembered for auto join without needing a save
        window.electronAPI.send('message', JSON.stringify({ command: 'settings-set', data: [{ key: 'voicebot.voiceChannelId', value: voiceChannelId.value }] }));
        return result;
    });

const setRefChannel = (channelId) => runVoiceBot(() => VoiceBotService.setRefChannel(selectedGuildId.value, channelId || null));

// Channels only exist once the bot is logged in, this flips on every (re)connect
watch(
    voiceBotControllable,
    (ready) => {
        if (ready) loadVoiceBotChannels();
    },
    { immediate: true },
);

const toggleVoiceBot = () => {
    window.electronAPI.send('message', JSON.stringify({ command: voiceBotRunning.value ? 'voicebot-stop' : 'voicebot-start' }));
};

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

    const settings = [{ key: 'theme', value: theme.value }, { key: 'currentEvent.startgg.apiToken', value: startGGApiToken.value }, { key: 'currentEvent.startgg.tournamentSlug', value: startGGTournamentSlug.value }, { key: 'currentEvent.startgg.tournamentId', value: startGGTournamentId.value }, { key: 'currentEvent.startgg.tournamentName', value: startGGTournamentName.value }, { key: 'currentEvent.startgg.eventId', value: startGGEventId.value }, { key: 'obs.enabled', value: obsEnabled.value }, { key: 'obs.url', value: obsUrl.value }, { key: 'obs.password', value: obsPassword.value }, { key: 'srxd.enabled', value: srxdEnabled.value }, { key: 'srxd.url', value: srxdUrl.value }, { key: 'voicebot.mode', value: voiceBotMode.value }, { key: 'voicebot.url', value: voiceBotUrl.value }, { key: 'voicebot.token', value: voiceBotToken.value }, { key: 'voicebot.port', value: voiceBotPort.value }, { key: 'voicebot.nodePath', value: voiceBotNodePath.value }, { key: 'voicebot.autoJoin', value: voiceBotAutoJoin.value }, { key: 'voicebot.voiceChannelId', value: voiceChannelId.value }, { key: 'music.source', value: musicSource.value }, { key: 'tuna.url', value: tunaUrl.value }, { key: 'tuna.showCover', value: tunaShowCover.value }, ...obsScreens.map((screen) => ({ key: `obs.scene.${screen.key}`, value: obsSceneMapping.value[screen.key] }))];

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
        // Old settings were a season plus eventYear
        const year = settings['eventYear'] ? `-${settings['eventYear']}` : '';
        theme.value = getTheme(settings['theme']?.includes('-') ? settings['theme'] : `${settings['theme'] ?? ''}${year}`).id;

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
        voiceBotMode.value = settings['voicebot.mode'] === 'remote' ? 'remote' : 'local';
        voiceBotUrl.value = settings['voicebot.url'] ?? '';
        voiceBotToken.value = settings['voicebot.token'] ?? '';
        voiceBotPort.value = settings['voicebot.port'] ? String(settings['voicebot.port']) : '8765';
        voiceBotNodePath.value = settings['voicebot.nodePath'] ?? '';
        voiceBotAutoJoin.value = settings['voicebot.autoJoin'] ?? false;
        if (!voiceChannelId.value) voiceChannelId.value = settings['voicebot.voiceChannelId'] ?? '';
        applySavedVoiceBot(settings);
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
    emitter.on('settings-set-response', (settings) => {
        applySavedVoiceBot(settings);
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

<style lang="scss" scoped>
// Channel rows are stacked, so the dropdown can use the whole width at the smallest window size
.channel-select {
    flex: 1;
    min-width: 0;
    max-width: none;
}
.voicebot-log {
    max-height: 200px;
    overflow: auto;
    margin: 0;
    padding: 10px;
    font-size: 11px;
    white-space: pre-wrap;
    word-break: break-word;
    background: rgba(0, 0, 0, 0.3);
    border-radius: 4px;
}
</style>
