<template>
    <AppLayout title="Match (Ingame)">
        <!-- Match recap carousel, only there while it is on screen -->
        <SpinInputGroup
            v-if="matchWon && resultsVisible && historyLength > 0"
            header="Match Recap"
        >
            <SpinInput
                v-if="!recapVisible"
                label="Recap"
                :hint="`Showing the last set${recapCountdown}`"
                type="horizontal"
            >
                <SpinButton
                    label="Show Recap"
                    color="bright"
                    @click="showRecap"
                />
            </SpinInput>
            <SpinInput
                v-if="recapSlides > 1"
                label="Set"
                :hint="`${resultsSlide + 1} of ${recapSlides}${recapCountdown}`"
                type="horizontal"
            >
                <div style="display: flex; gap: 10px">
                    <SpinButton
                        @click="setResultsSlide(resultsSlide - 1)"
                        icon="chevron-left"
                        :disabled="resultsSlide <= 0"
                        v-tooltip="'Previous Set'"
                    />
                    <SpinButton
                        @click="setResultsSlide(resultsSlide + 1)"
                        icon="chevron-right"
                        :disabled="resultsSlide >= recapSlides - 1"
                        v-tooltip="'Next Set'"
                    />
                </div>
            </SpinInput>
        </SpinInputGroup>
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
                label="Auto Recap"
                hint="Step through the match recap on its own, spread over a minute"
                type="horizontal"
            >
                <SpinSwitch
                    v-model="resultsAutoProgress"
                    @update:model-value="(value) => setStateFlag('resultsAutoProgress', value)"
                />
            </SpinInput>
            <SpinInput
                label="Post Results"
                hint="Post each set to the ref channel, needs the local voice bot running"
                type="horizontal"
            >
                <SpinSwitch
                    v-model="postResults"
                    @update:model-value="(value) => setStateFlag('postResults', value)"
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
                @click="replayLastSet"
                icon="replay"
                :disabled="!canReplay"
                v-tooltip="'Replay last set'"
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
import { nextRecapSlideAt } from '@/modules/useWinnerPrompt';
import useSrxd, { chartFromTrack, guessIngameName, matchWinner, pickResults } from '@/modules/useSrxd';

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
const resultsAutoProgress = ref(false);
const resultsSlide = ref(0);
const historyLength = ref(0);
const history = ref([]);
const recapVisible = ref(false);
const postResults = ref(false);
const lastSet = ref(null);
const savedResults = ref(null);

const matchWon = computed(() => !!matchWinner({ sets: { max: scoresSetsMax.value }, score: { player1: scoresScorePlayer1.value, player2: scoresScorePlayer2.value } }));
const recapSlides = computed(() => (matchWon.value && resultsVisible.value && recapVisible.value ? historyLength.value : 0));

const sameScores = (a, b) => Number(a?.sets?.current) === Number(b?.sets?.current) && Number(a?.sets?.max) === Number(b?.sets?.max) && Number(a?.score?.player1) === Number(b?.score?.player1) && Number(a?.score?.player2) === Number(b?.score?.player2);
const canReplay = computed(() => !!lastSet.value && sameScores(lastSet.value.scoresAfter, { sets: { current: scoresSetsCurrent.value, max: scoresSetsMax.value }, score: { player1: scoresScorePlayer1.value, player2: scoresScorePlayer2.value } }));

const showRecap = () => {
    recapVisible.value = true;
    resultsSlide.value = 0;
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: { currentMatch: { recapVisible: true, resultsSlide: 0 } },
        }),
    );
};

// Undoes the last set (its point too, if one was added) and keeps its results, the next attempt
// at it is merged with them so each player keeps their best run (see useWinnerPrompt)
const replayLastSet = () => {
    const set = lastSet.value;
    if (!canReplay.value) return;

    emitter.emit('alert-show', {
        title: 'Replay last set?',
        message: set.counted ? "Removes the point from the last set. The replay counts each player's best attempt." : "The replay counts each player's best attempt.",
        actions: [
            { label: 'Cancel' },
            {
                label: 'Replay',
                color: 'primary',
                click: () => {
                    const currentMatch = {
                        replay: { ...set.results, fileReference: set.fileReference ?? null },
                        lastSet: null,
                        resultsVisible: false,
                        recapVisible: false,
                    };
                    if (set.counted) {
                        currentMatch.scores = set.scores;
                        currentMatch.history = history.value.slice(0, -1);
                    }

                    window.electronAPI.send(
                        'message',
                        JSON.stringify({
                            command: 'state-set',
                            data: { currentMatch },
                        }),
                    );
                    // The page only listens for state-get-response, so it follows along by hand
                    lastSet.value = null;
                    resultsVisible.value = false;
                    recapVisible.value = false;
                    if (set.counted) {
                        scoresSetsCurrent.value = set.scores?.sets?.current ?? scoresSetsCurrent.value;
                        scoresScorePlayer1.value = set.scores?.score?.player1 ?? scoresScorePlayer1.value;
                        scoresScorePlayer2.value = set.scores?.score?.player2 ?? scoresScorePlayer2.value;
                        history.value = currentMatch.history;
                        historyLength.value = history.value.length;
                    }
                },
            },
        ],
    });
};

// Ticks while auto recap counts down, so the hint shows the seconds left
const now = ref(Date.now());
let nowTimer = null;
watch(
    nextRecapSlideAt,
    (value) => {
        clearInterval(nowTimer);
        nowTimer = null;
        now.value = Date.now();
        if (value) nowTimer = setInterval(() => (now.value = Date.now()), 250);
    },
    { immediate: true },
);
onUnmounted(() => clearInterval(nowTimer));

const recapCountdown = computed(() => {
    if (!recapVisible.value) return nextRecapSlideAt.value ? `, recap in ${Math.max(0, Math.ceil((nextRecapSlideAt.value - now.value) / 1000))}s` : '';
    if (!resultsAutoProgress.value) return '';
    if (!nextRecapSlideAt.value) return resultsSlide.value >= recapSlides.value - 1 ? ', auto recap done' : '';
    return `, next set in ${Math.max(0, Math.ceil((nextRecapSlideAt.value - now.value) / 1000))}s`;
});

const setResultsSlide = (slide) => {
    resultsSlide.value = Math.min(recapSlides.value - 1, Math.max(0, slide));
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: { currentMatch: { resultsSlide: resultsSlide.value } },
        }),
    );
};

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
        // The saved results can be merged with an earlier attempt (replay), so they win for the same round
        const picked = pickResults(game.value.results, { player1: ingamePlayer1.value, player2: ingamePlayer2.value });
        currentMatch.results = savedResults.value?.round === picked.round && (savedResults.value.player1 || savedResults.value.player2) ? savedResults.value : picked;
        currentMatch.resultsSlide = 0;
        currentMatch.recapVisible = false;
        resultsSlide.value = 0;
        recapVisible.value = false;
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
        resultsAutoProgress.value = state?.resultsAutoProgress ?? false;
        resultsSlide.value = state?.currentMatch?.resultsSlide ?? 0;
        historyLength.value = state?.currentMatch?.history?.length ?? 0;
        history.value = state?.currentMatch?.history ?? [];
        recapVisible.value = state?.currentMatch?.recapVisible ?? false;
        lastSet.value = state?.currentMatch?.lastSet ?? null;
        savedResults.value = state?.currentMatch?.results ?? null;
        postResults.value = state?.postResults ?? false;
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
