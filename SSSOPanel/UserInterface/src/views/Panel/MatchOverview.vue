<template>
    <AppLayout title="Match (Overview)">
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
            label="Player 2"
            :hint="loadedPlayer2 === null ? 'None selected' : loadedPlayer2?.spinshareProfile?.username ?? 'Unknown user'"
        >
            <SpinSelect
                v-model="player2Id"
                :options="playerSelectOptions"
                @update:model-value="setPlayer2"
            />
        </SpinInput>

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
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import { ref, inject, onMounted, onUnmounted } from 'vue';
const emitter = inject('emitter');

const playerMappings = ref([]);
const playerSelectOptions = ref([]);
const player1Id = ref(null);
const player2Id = ref(null);

const loadedPlayer1 = ref(null);
const loadedPlayer2 = ref(null);

const refDenIsVisible = ref(false);

const transition = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'state-set',
            data: {
                currentMatch: {
                    players: {
                        player1: loadedPlayer1.value,
                        player2: loadedPlayer2.value,
                    },
                },
            },
        }),
    );

    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'match-overview',
            },
        }),
    );
};

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
    console.log('[PANEL-MATCHOVERVIEW] refDenIsVisible toggled:', refDenIsVisible.value);
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
};
const setPlayer2 = () => {
    loadedPlayer2.value = playerMappings.value.find((x) => x.id + '' === player2Id.value);
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        player1Id.value = state?.currentMatch?.players?.player1?.id ? state?.currentMatch?.players?.player1?.id + '' : player1Id.value;
        player2Id.value = state?.currentMatch?.players?.player2?.id ? state?.currentMatch?.players?.player2?.id + '' : player2Id.value;
        loadedPlayer1.value = state?.currentMatch?.players?.player1 ?? loadedPlayer1.value;
        loadedPlayer2.value = state?.currentMatch?.players?.player2 ?? loadedPlayer2.value;
        refDenIsVisible.value = state?.refDenIsVisible ?? false;
    });

    emitter.on('settings-get-full-response', async (settings) => {
        loadPlayers(settings['currentEvent.playerMapping'] ?? []);
    });

    window.external.sendMessage(
        JSON.stringify({
            command: 'settings-get-full',
        }),
    );

    window.external.sendMessage(
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
