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
            label="Ingame Player 1"
            :hint="ingamePlayer1 ? (ingameMemberNames.includes(ingamePlayer1) ? 'In room' : 'Not in room') : 'Not linked'"
        >
            <SpinSelect
                v-model="ingamePlayer1"
                :options="ingameOptions(ingamePlayer1)"
                @update:model-value="pushIngame"
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
        <SpinInput
            label="Ingame Player 2"
            :hint="ingamePlayer2 ? (ingameMemberNames.includes(ingamePlayer2) ? 'In room' : 'Not in room') : 'Not linked'"
        >
            <SpinSelect
                v-model="ingamePlayer2"
                :options="ingameOptions(ingamePlayer2)"
                @update:model-value="pushIngame"
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
                @click="newMatch"
                icon="restart"
                :disabled="!matchStarted"
                v-tooltip="matchStarted ? 'New Match (clears scores and sets, keeps players)' : 'Nothing to clear'"
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
import useRefDenToggle from '@/modules/useRefDenToggle';
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import { ref, computed, watch, inject, onMounted, onUnmounted } from 'vue';
import useSrxd, { guessIngameName } from '@/modules/useSrxd';
import { resetMatch } from '@/modules/useWinnerPrompt';
const emitter = inject('emitter');
const game = useSrxd();

const playerMappings = ref([]);
const playerSelectOptions = ref([]);
const player1Id = ref(null);
const player2Id = ref(null);

const loadedPlayer1 = ref(null);
const loadedPlayer2 = ref(null);

const ingamePlayer1 = ref('');
const ingamePlayer2 = ref('');

const { refDenIsVisible, toggleRefDen } = useRefDenToggle();

const scores = ref({});
const historyLength = ref(0);
// Who the current match is between, so picking someone else can offer a fresh match
const matchPlayerIds = ref([]);
const matchStarted = computed(() => historyLength.value > 0 || Number(scores.value.score?.player1 ?? 0) + Number(scores.value.score?.player2 ?? 0) > 0);

const newMatch = () => {
    emitter.emit('alert-show', {
        title: 'New match?',
        message: 'Clears the scores and sets of the current match, players and chart stay.',
        actions: [
            { label: 'Cancel' },
            {
                label: 'New Match',
                color: 'primary',
                click: () => {
                    resetMatch(Number(scores.value.sets?.max ?? 3));
                    scores.value = {};
                    historyLength.value = 0;
                },
            },
        ],
    });
};

const ingameMembers = computed(() => [...(game.value.room?.players ?? []), ...(game.value.room?.spectators ?? [])]);
const ingameMemberNames = computed(() => ingameMembers.value.map((member) => member.name));

const ingameOptions = (selected) => {
    // Keep a pre-linked player selectable while they are not in the room yet
    const names = [...ingameMemberNames.value];
    if (selected && !names.includes(selected)) names.push(selected);

    return [{ icon: 'border-none-variant', label: 'None', value: '' }, ...names.map((name) => ({ icon: 'controller', label: name, value: name }))];
};

// Same name matching as match ingame, only fills empty links
const guessIngamePlayers = () => {
    if (!ingamePlayer1.value) ingamePlayer1.value = guessIngameName(loadedPlayer1.value, ingameMembers.value) ?? '';
    if (!ingamePlayer2.value) ingamePlayer2.value = guessIngameName(loadedPlayer2.value, ingameMembers.value) ?? '';
};

watch(() => game.value.room, guessIngamePlayers);

// Only the links, the players themselves wait for the transition so the screen doesn't change early
const pushIngame = () => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                currentMatch: {
                    ingame: {
                        player1: ingamePlayer1.value,
                        player2: ingamePlayer2.value,
                    },
                },
            },
        }),
    );
};

const transition = () => {
    // Different players with points still on the board, the last match was probably never restarted
    const ids = [loadedPlayer1.value?.id, loadedPlayer2.value?.id].map(String).sort();
    const newPlayers = matchPlayerIds.value.length > 0 && ids.join() !== matchPlayerIds.value.join();
    if (!newPlayers || !matchStarted.value) {
        showPlayers();
        return;
    }

    emitter.emit('alert-show', {
        title: 'New match?',
        message: 'These are different players, but the last match still has scores and sets. Clear them for this match?',
        actions: [
            { label: 'Keep Scores', click: showPlayers },
            {
                label: 'New Match',
                color: 'primary',
                click: () => {
                    resetMatch(Number(scores.value.sets?.max ?? 3));
                    scores.value = {};
                    historyLength.value = 0;
                    showPlayers();
                },
            },
        ],
    });
};

const showPlayers = () => {
    matchPlayerIds.value = [loadedPlayer1.value?.id, loadedPlayer2.value?.id].map(String).sort();
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                currentMatch: {
                    players: {
                        player1: loadedPlayer1.value,
                        player2: loadedPlayer2.value,
                    },
                    ingame: {
                        player1: ingamePlayer1.value,
                        player2: ingamePlayer2.value,
                    },
                },
            },
        }),
    );

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'match-overview',
            },
        }),
    );
};

const loadPlayers = (newMappings) => {
    playerMappings.value = newMappings;
    playerSelectOptions.value = [];

    playerMappings.value.forEach((mapping) => {
        playerSelectOptions.value.push({
            icon: 'account-box',
            label: mapping.spinshareProfile?.username ?? mapping.name,
            value: mapping.id + '',
        });
    });
};

const setPlayer1 = () => {
    loadedPlayer1.value = playerMappings.value.find((x) => x.id + '' === player1Id.value);
    ingamePlayer1.value = '';
    guessIngamePlayers();
};
const setPlayer2 = () => {
    loadedPlayer2.value = playerMappings.value.find((x) => x.id + '' === player2Id.value);
    ingamePlayer2.value = '';
    guessIngamePlayers();
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        player1Id.value = state?.currentMatch?.players?.player1?.id ? state?.currentMatch?.players?.player1?.id + '' : player1Id.value;
        player2Id.value = state?.currentMatch?.players?.player2?.id ? state?.currentMatch?.players?.player2?.id + '' : player2Id.value;
        loadedPlayer1.value = state?.currentMatch?.players?.player1 ?? loadedPlayer1.value;
        loadedPlayer2.value = state?.currentMatch?.players?.player2 ?? loadedPlayer2.value;
        ingamePlayer1.value = state?.currentMatch?.ingame?.player1 ?? ingamePlayer1.value;
        ingamePlayer2.value = state?.currentMatch?.ingame?.player2 ?? ingamePlayer2.value;
        scores.value = state?.currentMatch?.scores ?? {};
        historyLength.value = state?.currentMatch?.history?.length ?? 0;
        const players = state?.currentMatch?.players;
        matchPlayerIds.value = players?.player1 || players?.player2 ? [players?.player1?.id, players?.player2?.id].map(String).sort() : [];
        guessIngamePlayers();
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
