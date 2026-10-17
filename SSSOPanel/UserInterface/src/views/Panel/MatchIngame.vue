<template>
    <AppLayout title="Match (Ingame)">
        <SpinInputGroup
            header="Game"
            :collapsable="true"
            :collapsed="true"
        >
            <SpinInput
                label="Spin Rhythm XD"
                :hint="gameStatus"
                type="horizontal"
            />
            <SpinInput
                label="Chart from Game"
                hint="Load the picked chart automatically"
                type="horizontal"
            >
                <SpinSwitch
                    v-model="autoChart"
                    @update:model-value="setAutoChart"
                />
            </SpinInput>
            <SpinInput
                label="Chart ID"
                :hint="loadedChart === null ? 'Not loaded' : loadedChart?.title ?? 'Unknown chart ID'"
            >
                <input
                    v-model="chartId"
                    type="number"
                    @change="onChartIdChange"
                />
            </SpinInput>
            <SpinInput
                label="Auto Results"
                hint="Add the point and show the results on its own, hidden after 30s"
                type="horizontal"
            >
                <SpinSwitch
                    v-model="autoResults"
                    @update:model-value="(value) => setStateFlag('autoResults', value)"
                />
            </SpinInput>
            <SpinInput
                label="Winner Prompt"
                :hint="autoResults ? 'Not needed with Auto Results' : 'Ask to add a point when a round ends'"
                type="horizontal"
            >
                <SpinSwitch
                    v-model="winnerPrompt"
                    :disabled="autoResults"
                    @update:model-value="setWinnerPrompt"
                />
            </SpinInput>
        </SpinInputGroup>
        <SpinInputGroup
            header="Scores"
            :collapsable="true"
            :collapsed="true"
        >
            <SpinInput
                label="Sets"
                type="horizontal"
            >
                <div style="display: flex; gap: 10px">
                    <input
                        v-model="scoresSetsCurrent"
                        type="number"
                        min="1"
                        :max="scoresSetsMax"
                        placeholder="Current"
                        @change="updateState"
                        style="flex-grow: 0; max-width: 90px"
                    />
                    <input
                        v-model="scoresSetsMax"
                        type="number"
                        :min="scoresSetsCurrent"
                        max="9"
                        placeholder="Max"
                        @change="updateState"
                        style="flex-grow: 0; max-width: 90px"
                    />
                </div>
            </SpinInput>
            <SpinInput
                label="Score"
                hint="Player 1"
                type="horizontal"
            >
                <input
                    v-model="scoresScorePlayer1"
                    type="number"
                    min="0"
                    :max="Math.ceil(scoresSetsMax / 2)"
                    @change="updateState"
                />
            </SpinInput>
            <SpinInput
                label="Score"
                hint="Player 2"
                type="horizontal"
            >
                <input
                    v-model="scoresScorePlayer2"
                    type="number"
                    min="0"
                    :max="Math.ceil(scoresSetsMax / 2)"
                    @change="updateState"
                />
            </SpinInput>
        </SpinInputGroup>
        <SpinInputGroup
            header="Player 1 Setup"
            :collapsable="true"
            :collapsed="true"
        >
            <SpinInput
                label="Player 1"
                :hint="loadedPlayer1 === null ? 'None selected' : loadedPlayer1?.spinshareProfile?.username ?? 'Unknown user'"
            >
                <SpinSelect
                    v-model="player1Id"
                    :options="playerSelectOptions"
                    @update:model-value="setPlayer1"
                />
            </SpinInput>
            <SpinInput
                label="Ingame Player"
                :hint="ingamePlayer1 ? (ingameMemberNames.includes(ingamePlayer1) ? 'In room' : 'Not in room') : 'Not linked'"
            >
                <SpinSelect
                    v-model="ingamePlayer1"
                    :options="ingameOptions(ingamePlayer1)"
                    @update:model-value="updateState"
                />
            </SpinInput>
            <SpinInput
                v-if="currentScreen === '/screen/match-ingame'"
                label="Update Players"
                hint="Pushes the selected players to the screen"
                type="horizontal"
            >
                <SpinButton
                    label="Update"
                    color="bright"
                    @click="updateState"
                />
            </SpinInput>
        </SpinInputGroup>
        <SpinInputGroup
            header="Player 2 Setup"
            :collapsable="true"
            :collapsed="true"
        >
            <SpinInput
                label="Player 2"
                :hint="loadedPlayer2 === null ? 'None selected' : loadedPlayer2?.spinshareProfile?.username ?? 'Unknown user'"
            >
                <SpinSelect
                    v-model="player2Id"
                    :options="playerSelectOptions"
                    @update:model-value="setPlayer2"
                />
            </SpinInput>
            <SpinInput
                label="Ingame Player"
                :hint="ingamePlayer2 ? (ingameMemberNames.includes(ingamePlayer2) ? 'In room' : 'Not in room') : 'Not linked'"
            >
                <SpinSelect
                    v-model="ingamePlayer2"
                    :options="ingameOptions(ingamePlayer2)"
                    @update:model-value="updateState"
                />
            </SpinInput>
            <SpinInput
                v-if="currentScreen === '/screen/match-ingame'"
                label="Update Players"
                hint="Pushes the selected players to the screen"
                type="horizontal"
            >
                <SpinButton
                    label="Update"
                    color="bright"
                    @click="updateState"
                />
            </SpinInput>
        </SpinInputGroup>

        <template #transition>
            <SpinButton
                @click="toggleRefDen"
                :icon="refDenIsVisible ? 'comment-text-multiple' : 'comment-text-multiple-outline'"
                :color="refDenIsVisible ? 'primary' : 'default'"
                v-tooltip="'Toggle Ref Den'"
            />
            <SpinButton
                @click="toggleResults"
                :icon="resultsVisible ? 'trophy' : 'trophy-outline'"
                :color="resultsVisible ? 'primary' : 'default'"
                :disabled="!resultsVisible && !game.results?.complete"
                v-tooltip="'Toggle Results'"
            />

            <SpinButton
                @click="transition"
                icon="record"
                color="primary"
                :disabled="loadedChart === null"
                v-tooltip="'Transition to screen'"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinInputGroup from '@/components/Common/SpinInputGroup.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import SpinSwitch from '@/components/Common/SpinSwitch.vue';
import { computed, onMounted, onUnmounted, ref, inject, watch } from 'vue';
import useCurrentScreen from '@/modules/useCurrentScreen';
import useSpinShareApi from '@/modules/useSpinShareApi';
import useSrxd, { chartFromTrack, guessIngameName, pickResults } from '@/modules/useSrxd';

const currentScreen = useCurrentScreen();
const emitter = inject('emitter');
const game = useSrxd();

const playerMappings = ref([]);
const playerSelectOptions = ref([]);
const player1Id = ref(null);
const player2Id = ref(null);

const loadedPlayer1 = ref(null);
const loadedPlayer2 = ref(null);
const chartId = ref(0);
const loadedChart = ref(null);
const scoresSetsCurrent = ref(1);
const scoresSetsMax = ref(3);
const scoresScorePlayer1 = ref(0);
const scoresScorePlayer2 = ref(0);

// False until the first state-get-response, nothing gets pushed before that
const stateLoaded = ref(false);
const ingamePlayer1 = ref('');
const ingamePlayer2 = ref('');
const autoChart = ref(true);
const winnerPrompt = ref(true);
const autoResults = ref(false);

const gameStatus = computed(() => {
    if (!game.value.enabled) return 'Disabled in settings';
    if (!game.value.connected) return 'Not connected';
    if (!game.value.room) return 'Connected, not in a room';
    return `${game.value.room.name} (${game.value.phase})`;
});

const ingameMemberNames = computed(() => [...(game.value.room?.players ?? []), ...(game.value.room?.spectators ?? [])].map((member) => member.name));

const ingameOptions = (selected) => {
    // Keep a linked player selectable after they left the room
    const names = [...ingameMemberNames.value];
    if (selected && !names.includes(selected)) names.push(selected);

    return [{ icon: 'border-none-variant', label: 'None', value: '' }, ...names.map((name) => ({ icon: 'controller', label: name, value: name }))];
};

// Links mapped players to room players with the same name, unless already linked
const guessIngamePlayers = (push) => {
    const members = [...(game.value.room?.players ?? []), ...(game.value.room?.spectators ?? [])];
    let changed = false;

    if (!ingamePlayer1.value) {
        ingamePlayer1.value = guessIngameName(loadedPlayer1.value, members) ?? '';
        changed ||= ingamePlayer1.value !== '';
    }
    if (!ingamePlayer2.value) {
        ingamePlayer2.value = guessIngameName(loadedPlayer2.value, members) ?? '';
        changed ||= ingamePlayer2.value !== '';
    }

    if (changed && push) updateState();
};

const loadChartFromTrack = async (track) => {
    let chart = null;

    if (track.fileReference?.startsWith('spinshare_')) {
        const { loadChart } = useSpinShareApi();
        chart = await loadChart(track.fileReference);
    }

    // Base game or not uploaded, the game is the only source for metadata
    loadedChart.value = chart ?? chartFromTrack(track);
    chartId.value = loadedChart.value.id ?? 0;
    updateState();
};

const setStateFlag = (key, value) => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                [key]: value,
            },
        }),
    );
};

// Kept in the state so turning it off sticks when the page is opened again
const setAutoChart = (value) => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                autoChart: value,
            },
        }),
    );
};

// The prompt itself lives in useWinnerPrompt.js (used in App.vue) so it shows on every panel page
const setWinnerPrompt = (value) => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                winnerPrompt: value,
            },
        }),
    );
};

const autoLoadChart = (track, oldTrack) => {
    // Waits for the saved state, pushing before that would overwrite the match with empty defaults
    if (!stateLoaded.value || !autoChart.value || !track) return;
    // Tracks are sent again whenever availability or metadata changes
    if (track.fileReference === oldTrack?.fileReference && track.title === oldTrack?.title) return;
    if (track.fileReference && track.fileReference === loadedChart.value?.fileReference) return;

    loadChartFromTrack(track);
};

watch(() => game.value.track, autoLoadChart);

watch(
    () => game.value.room,
    () => guessIngamePlayers(true),
);

const transition = () => {
    updateState();

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'match-ingame',
            },
        }),
    );
};

const updateState = () => {
    if (!stateLoaded.value) return;

    let screenData = {
        currentMatch: {
            players: {
                player1: loadedPlayer1.value,
                player2: loadedPlayer2.value,
            },
            scores: {
                sets: {
                    current: scoresSetsCurrent.value,
                    max: scoresSetsMax.value,
                },
                score: {
                    player1: scoresScorePlayer1.value,
                    player2: scoresScorePlayer2.value,
                },
            },
            chart: loadedChart.value,
            ingame: {
                player1: ingamePlayer1.value,
                player2: ingamePlayer2.value,
            },
        },
    };

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: screenData,
        }),
    );
};

const onChartIdChange = async () => {
    await loadChart();
    if (loadedChart.value) updateState();
};

const loadChart = async () => {
    loadedChart.value = null;

    const { loadChart } = useSpinShareApi();
    loadedChart.value = await loadChart(chartId.value);
};

const loadPlayers = (newMappings) => {
    playerMappings.value = newMappings;
    playerSelectOptions.value = [];

    playerMappings.value.forEach((mapping) => {
        playerSelectOptions.value.push({
            icon: 'account-box',
            label: mapping.spinshareProfile.username,
            value: mapping.id + '',
        });
    });
};

const setPlayer1 = () => {
    loadedPlayer1.value = playerMappings.value.find((x) => x.id + '' === player1Id.value);
    ingamePlayer1.value = '';
    guessIngamePlayers(false);
};
const setPlayer2 = () => {
    loadedPlayer2.value = playerMappings.value.find((x) => x.id + '' === player2Id.value);
    ingamePlayer2.value = '';
    guessIngamePlayers(false);
};

const refDenIsVisible = ref(false);
const resultsVisible = ref(false);

// Shows the last finished round over the gameplay cutout, hidden again when the next song loads
const toggleResults = () => {
    resultsVisible.value = !resultsVisible.value;

    const currentMatch = { resultsVisible: resultsVisible.value };
    if (resultsVisible.value) {
        currentMatch.results = pickResults(game.value.results, { player1: ingamePlayer1.value, player2: ingamePlayer2.value });
    }

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: { currentMatch },
        }),
    );
};

const toggleRefDen = () => {
    refDenIsVisible.value = !refDenIsVisible.value;
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                refDenIsVisible: refDenIsVisible.value,
            },
        }),
    );
    console.log('[PANEL-MATCHOVERVIEW] refDenIsVisible toggled:', refDenIsVisible.value);
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        player1Id.value = state?.currentMatch?.players?.player1?.id ? state?.currentMatch?.players?.player1?.id + '' : player1Id.value;
        loadedPlayer1.value = state?.currentMatch?.players?.player1 ?? loadedPlayer1.value;

        player2Id.value = state?.currentMatch?.players?.player2?.id ? state?.currentMatch?.players?.player2?.id + '' : player2Id.value;
        loadedPlayer2.value = state?.currentMatch?.players?.player2 ?? loadedPlayer2.value;

        scoresSetsCurrent.value = state?.currentMatch?.scores?.sets?.current ?? scoresSetsCurrent.value;
        scoresSetsMax.value = state?.currentMatch?.scores?.sets?.max ?? scoresSetsMax.value;
        scoresScorePlayer1.value = state?.currentMatch?.scores?.score?.player1 ?? scoresScorePlayer1.value;
        scoresScorePlayer2.value = state?.currentMatch?.scores?.score?.player2 ?? scoresScorePlayer2.value;

        loadedChart.value = state?.currentMatch?.chart ?? loadedChart.value;
        chartId.value = state?.currentMatch?.chart?.id ?? chartId.value;

        ingamePlayer1.value = state?.currentMatch?.ingame?.player1 ?? ingamePlayer1.value;
        ingamePlayer2.value = state?.currentMatch?.ingame?.player2 ?? ingamePlayer2.value;

        refDenIsVisible.value = state?.refDenIsVisible ?? false;
        resultsVisible.value = state?.currentMatch?.resultsVisible ?? false;
        winnerPrompt.value = state?.winnerPrompt ?? true;
        autoResults.value = state?.autoResults ?? false;
        autoChart.value = state?.autoChart ?? true;

        if (!stateLoaded.value) {
            stateLoaded.value = true;
            // Catch up on anything the game sent before the state was there
            autoLoadChart(game.value.track, null);
            guessIngamePlayers(true);
        }
    });

    emitter.on('settings-get-full-response', async (settings) => {
        loadPlayers(settings['currentEvent.playerMapping'] ?? []);
    });

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'settings-get-full',
        }),
    );

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-get',
        }),
    );
});

onUnmounted(() => {
    emitter.off('settings-get-full-response');
    emitter.off('state-get-response');
});
</script>

<style lang="scss" scoped></style>
