<template>
    <AppLayout title="Match (Ingame)">
        <SpinInputGroup header="Controls">
            <SpinInput
                label="Streams"
                type="horizontal"
            >
                <div style="display: flex; gap: 10px">
                    <SpinButton
                        label="On"
                        @click="
                            () => {
                                streamsActive = true;
                                updateState();
                            }
                        "
                        :disabled="streamsActive"
                    />
                    <SpinButton
                        label="Off"
                        @click="
                            () => {
                                streamsActive = false;
                                updateState();
                            }
                        "
                        :disabled="!streamsActive"
                    />
                </div>
            </SpinInput>
            <SpinInput
                label="Audio"
                type="horizontal"
            >
                <div style="display: flex; gap: 10px">
                    <SpinButton
                        label="Left Stream"
                        @click="
                            () => {
                                audioActive = 'left';
                                updateState();
                            }
                        "
                        :disabled="audioActive === 'left'"
                    />
                    <SpinButton
                        label="Right Stream"
                        @click="
                            () => {
                                audioActive = 'right';
                                updateState();
                            }
                        "
                        :disabled="audioActive === 'right'"
                    />
                </div>
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
                        min="0"
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
                label="Region"
                hint="Livestream"
                type="horizontal"
            >
                <SpinSelect
                    v-model="player1Region"
                    :options="regions"
                />
            </SpinInput>
            <SpinInput
                label="Key"
                hint="Livestream"
                type="horizontal"
            >
                <input
                    v-model="player1Key"
                    type="text"
                />
            </SpinInput>
            <SpinInput
                v-if="currentScreen === '/screen/match-ingame'"
                label="Update Players"
                hint="This restarts the livestream area"
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
                label="Region"
                hint="Livestream"
                type="horizontal"
            >
                <SpinSelect
                    v-model="player2Region"
                    :options="regions"
                />
            </SpinInput>
            <SpinInput
                label="Key"
                hint="Livestream"
                type="horizontal"
            >
                <input
                    v-model="player2Key"
                    type="text"
                />
            </SpinInput>
            <SpinInput
                v-if="currentScreen === '/screen/match-ingame'"
                label="Update Players"
                hint="This restarts the livestream area"
                type="horizontal"
            >
                <SpinButton
                    label="Update"
                    color="bright"
                    @click="updateState"
                />
            </SpinInput>
        </SpinInputGroup>
        <SpinInput
            label="Chart ID"
            :hint="loadedChart === null ? 'Not loaded' : loadedChart?.title ?? 'Unknown chart ID'"
        >
            <input
                v-model="chartId"
                type="number"
                @change="loadChart"
            />
        </SpinInput>

        <template #transition>
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
import { onMounted, onUnmounted, ref, inject } from 'vue';
import useCurrentScreen from '@/modules/useCurrentScreen';
import useSpinShareApi from '@/modules/useSpinShareApi';

const currentScreen = useCurrentScreen();
const emitter = inject('emitter');

const regions = [
    { icon: 'earth', label: 'EU3', value: 'eu3' },
    { icon: 'earth', label: 'USE', value: 'use' },
    { icon: 'earth', label: 'USW', value: 'usw' },
    { icon: 'earth', label: 'OCE', value: 'oce' },
];

const player1Region = ref('eu3');
const player1Key = ref('');
const player2Region = ref('eu3');
const player2Key = ref('');
const chartId = ref(0);
const loadedChart = ref(null);
const streamsActive = ref(true);
const audioActive = ref('left');
const scoresSetsCurrent = ref(0);
const scoresSetsMax = ref(3);
const scoresScorePlayer1 = ref(0);
const scoresScorePlayer2 = ref(0);

const transition = () => {
    updateState();

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'match-ingame',
            },
        }),
    );
};

const updateState = () => {
    let screenData = {
        currentMatch: {
            players: {
                player1: {
                    region: player1Region.value,
                    key: player1Key.value,
                },
                player2: {
                    region: player2Region.value,
                    key: player2Key.value,
                },
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
            streamsActive: streamsActive.value,
            audioActive: audioActive.value,
        },
    };

    window.external.sendMessage(
        JSON.stringify({
            command: 'state-set',
            data: screenData,
        }),
    );
};

const loadChart = async () => {
    loadedChart.value = null;

    const { loadChart } = useSpinShareApi();
    loadedChart.value = await loadChart(chartId.value);
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        player1Key.value = state?.currentMatch?.players?.player1?.key ?? player1Key.value;
        player1Region.value = state?.currentMatch?.players?.player1?.region ?? player1Region.value;

        player2Key.value = state?.currentMatch?.players?.player2?.key ?? player2Key.value;
        player2Region.value = state?.currentMatch?.players?.player2?.region ?? player2Region.value;

        scoresSetsCurrent.value = state?.currentMatch?.scores?.sets?.current ?? scoresSetsCurrent.value;
        scoresSetsMax.value = state?.currentMatch?.scores?.sets?.max ?? scoresSetsMax.value;
        scoresScorePlayer1.value = state?.currentMatch?.scores?.score?.player1 ?? scoresScorePlayer1.value;
        scoresScorePlayer2.value = state?.currentMatch?.scores?.score?.player2 ?? scoresScorePlayer2.value;

        loadedChart.value = state?.currentMatch?.chart ?? loadedChart.value;
        chartId.value = state?.currentMatch?.chart?.id ?? chartId.value;

        streamsActive.value = state?.currentMatch?.streamsActive ?? streamsActive.value;
        audioActive.value = state?.currentMatch?.audioActive ?? audioActive.value;
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
