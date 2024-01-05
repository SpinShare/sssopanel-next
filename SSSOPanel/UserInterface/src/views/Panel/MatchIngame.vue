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
                        @click="setStreamsActive(true)"
                        :disabled="streamsActive"
                    />
                    <SpinButton
                        @click="setStreamsActive(false)"
                        label="Off"
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
                        @click="setAudioActive('left')"
                        :disabled="audioActive === 'left'"
                    />
                    <SpinButton
                        label="Right Stream"
                        @click="setAudioActive('right')"
                        :disabled="audioActive === 'right'"
                    />
                </div>
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
                    @click="updatePlayers"
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
                    @click="updatePlayers"
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
const chartId = ref(null);
const loadedPlayer1 = ref(null);
const loadedPlayer2 = ref(null);
const loadedChart = ref(null);
const streamsActive = ref(true);
const audioActive = ref('left');

const transition = () => {
    const screenData = {
        player1: {
            ...loadedPlayer1.value,
            region: player1Region.value,
            key: player1Key.value,
        },
        player2: {
            ...loadedPlayer2.value,
            region: player2Region.value,
            key: player2Key.value,
        },
        chart: loadedChart.value,
        streamsActive: streamsActive.value,
        audioActive: audioActive.value,
    };

    console.log(screenData);

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'match-ingame',
                params: {},
                query: {},
                richData: screenData,
            },
        }),
    );
};

const updatePlayers = () => {
    const screenData = {
        player1: {
            ...loadedPlayer1.value,
            region: player1Region.value,
            key: player1Key.value,
        },
        player2: {
            ...loadedPlayer2.value,
            region: player2Region.value,
            key: player2Key.value,
        },
    };

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-match-update',
            data: screenData,
        }),
    );
};

const setStreamsActive = (active) => {
    streamsActive.value = active;

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-match-update',
            data: {
                streamsActive: streamsActive.value,
            },
        }),
    );
};

const setAudioActive = (side) => {
    audioActive.value = side;

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-match-update',
            data: {
                audioActive: audioActive.value,
            },
        }),
    );
};

const loadChart = async () => {
    loadedChart.value = null;

    const { loadChart } = useSpinShareApi();
    loadedChart.value = await loadChart(chartId.value);
};

onMounted(() => {
    emitter.on('current-route-get-response', (data) => {
        loadedPlayer1.value = data?.RichData?.player1 ?? null;
        loadedPlayer2.value = data?.RichData?.player2 ?? null;
    });
});

onUnmounted(() => {
    emitter.off('current-route-get-response');
});
</script>

<style lang="scss" scoped></style>
