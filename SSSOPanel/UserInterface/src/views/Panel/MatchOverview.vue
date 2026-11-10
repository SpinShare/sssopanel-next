<template>
    <AppLayout title="Match (Overview)">
        <SpinInput
            label="Spulti Match"
            type="path"
            :hint="spultiStatus"
        >
            <input
                type="text"
                v-model="spultiUrl"
                placeholder="https://spulti.ellite.dev/matches/..."
                @keydown.enter="followSpultiMatch(spultiUrl)"
            />
            <SpinButton
                :label="spulti.url && spulti.url === spultiUrl.trim() ? 'Reconnect' : 'Follow'"
                @click="followSpultiMatch(spultiUrl)"
            />
        </SpinInput>
        <SpinInput
            label="Auto Spulti"
            hint="On match overview and ingame: flips the coin once the toss is in, shows the pick/ban board for bans and picks, hides it when the song loads"
            type="horizontal"
        >
            <SpinSwitch
                v-model="autoSpulti"
                @update:model-value="setAutoSpulti"
            />
        </SpinInput>
        <RefreshDataInput :spulti-match="spulti.match" />
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

        <ol
            class="spulti-steps"
            v-if="spultiSteps.length"
        >
            <li
                v-for="(step, i) in spultiSteps"
                :key="i"
            >
                {{ step }}
            </li>
        </ol>

        <template #transition>
            <!-- Same menu as on match ingame, separate buttons didn't fit the footer at the default size -->
            <SpinMenu
                :icon="pickBanVisible ? 'layers' : 'layers-outline'"
                :color="pickBanVisible ? 'primary' : 'default'"
                tooltip="Overlays"
            >
                <SpinButton
                    @click="setPickBanVisible(!pickBanVisible)"
                    :icon="pickBanVisible ? 'cards' : 'cards-outline'"
                    :color="pickBanVisible ? 'primary' : 'default'"
                    :disabled="!pickBanVisible && !spulti.match"
                    label="Pick / Ban"
                />
                <SpinButton
                    @click="playCoinFlip(spultiCoin.key)"
                    icon="circle-multiple-outline"
                    :disabled="!spultiCoin"
                    :label="spultiCoin && spultiCoin.key === coinFlipKey ? 'Flip Coin Again' : 'Flip Coin'"
                />
            </SpinMenu>
            <SpinButton
                @click="swapPlayers"
                icon="swap-horizontal"
                :disabled="!loadedPlayer1 && !loadedPlayer2"
                v-tooltip="'Swap Players'"
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
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSelect from '@/components/Common/SpinSelect.vue';
import SpinSwitch from '@/components/Common/SpinSwitch.vue';
import SpinMenu from '@/components/Common/SpinMenu.vue';
import { ref, computed, watch, inject, onMounted, onUnmounted } from 'vue';
import useSrxd, { guessIngameName } from '@/modules/useSrxd';
import { resetMatch } from '@/modules/useWinnerPrompt';
import RefreshDataInput from '@/components/RefreshDataInput.vue';
import useSpulti, { autofilled, coinResult, draftSteps, findMapping, followSpultiMatch, playerOptions, withGuests } from '@/modules/useSpulti';
import { playCoinFlip, setPickBanVisible } from '@/modules/useSpultiOverlays';
const emitter = inject('emitter');
const game = useSrxd();

const playerMappings = ref([]);
// The selects also list spulti guests, players of the followed match who aren't in the player mapping
const baseMappings = ref([]);
const statePlayers = ref([]);
// Guests only once the mapping is in, before that everyone would look like one
const mappingsLoaded = ref(false);
// Up here since watchers earlier in the setup read them (spulti is only used once it runs)
const refreshPlayers = () => {
    playerMappings.value = mappingsLoaded.value ? withGuests(baseMappings.value, spulti.value.match, statePlayers.value) : [];
    playerSelectOptions.value = playerOptions(playerMappings.value);
};
const playerSelectOptions = ref([]);
const player1Id = ref(null);
const player2Id = ref(null);

const loadedPlayer1 = ref(null);
const loadedPlayer2 = ref(null);

const ingamePlayer1 = ref('');
const ingamePlayer2 = ref('');

const scores = ref({});
const historyLength = ref(0);
// Who the current match is between, so picking someone else can offer a fresh match
const matchPlayerIds = ref([]);
// The match as it is on screen, so a swapped transition can flip everything per side with it
const currentMatch = ref({});
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

const flipSides = (sides) => (sides ? { ...sides, player1: sides.player2, player2: sides.player1 } : sides);

// Same players on the other sides, their points, sets and results have to move with them
const swappedMatch = () => {
    const players = currentMatch.value.players;
    const swapped = players?.player1 && players?.player2 && String(players.player1.id) === String(loadedPlayer2.value?.id) && String(players.player2.id) === String(loadedPlayer1.value?.id);
    if (!swapped || !matchStarted.value) return {};

    const match = currentMatch.value;
    return {
        scores: { ...match.scores, score: flipSides(match.scores?.score ?? {}) },
        history: (match.history ?? []).map((entry) => ({ ...entry, results: flipSides(entry.results), winner: entry.winner === 'player1' ? 'player2' : 'player1' })),
        ...(match.results ? { results: flipSides(match.results) } : {}),
        // Undo and replay remember the old sides
        lastSet: null,
        replay: null,
    };
};

const showPlayers = () => {
    matchPlayerIds.value = [loadedPlayer1.value?.id, loadedPlayer2.value?.id].map(String).sort();
    const match = {
        ...swappedMatch(),
        players: {
            player1: loadedPlayer1.value,
            player2: loadedPlayer2.value,
        },
        ingame: {
            player1: ingamePlayer1.value,
            player2: ingamePlayer2.value,
        },
    };
    // This page only reads the state on mount, a second transition must not flip it back
    currentMatch.value = { ...currentMatch.value, ...match };
    if (match.scores) scores.value = match.scores;

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: { currentMatch: match },
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

const spulti = useSpulti();
const spultiCoin = computed(() => coinResult(spulti.value.match));
const pickBanVisible = ref(false);
const coinFlipKey = ref(null);
const spultiUrl = ref('');
// Fill the box once from what the backend follows, after that it's the user's
watch(
    () => spulti.value.url,
    (url) => {
        if (!spultiUrl.value) spultiUrl.value = url ?? '';
    },
);
const spultiStatus = computed(() => {
    if (!spulti.value.url) return 'Paste a match link to follow it';
    if (spulti.value.error) return spulti.value.error;
    if (!spulti.value.connected) return 'Connecting...';
    const match = spulti.value.match;
    if (!match) return 'Connected, match not found';
    const missing = spultiPlayers.value?.names.length ? `, using spulti's profile for ${spultiPlayers.value.names.join(', ')} (not in player mapping)` : '';
    return `${match.player1?.name ?? 'TBD'} vs ${match.player2?.name ?? 'TBD'}, ${match.score.player1}-${match.score.player2} (${match.status})${missing}`;
});
// Kept in the state, useSpultiOverlays (App.vue) does the work on every panel page
const autoSpulti = ref(false);
const setAutoSpulti = (value) => window.electronAPI.send('message', JSON.stringify({ command: 'state-set', data: { autoSpulti: value } }));
const spultiSteps = computed(() => draftSteps(spulti.value.match, spulti.value.mappool));

// Picks the followed match's players, same order as spulti. Only a selection like picking them by hand, the screen
// changes on transition. Once per match and pairing, so a manual change afterwards sticks
const spultiPlayers = computed(() => {
    const match = spulti.value.match;
    if (!match) return null;
    return {
        key: `${match.id}:${match.player1?.name}:${match.player2?.name}`,
        player1: findMapping(match.player1, playerMappings.value),
        player2: findMapping(match.player2, playerMappings.value),
        // Not in the mapping, filled in as spulti guests
        names: [match.player1, match.player2].filter((player) => player && !findMapping(player, baseMappings.value)).map((player) => player.name),
    };
});
// After the state is in, it would overwrite the picks otherwise
const stateLoaded = ref(false);
let lastStatePlayers = null;
watch([spultiPlayers, stateLoaded, mappingsLoaded], ([found]) => {
    if (!found || !stateLoaded.value || !mappingsLoaded.value || found.key === autofilled.key || (!found.player1 && !found.player2)) return;
    autofilled.key = found.key;
    if (found.player1 && found.player1.id + '' !== player1Id.value) {
        player1Id.value = found.player1.id + '';
        setPlayer1();
    }
    if (found.player2 && found.player2.id + '' !== player2Id.value) {
        player2Id.value = found.player2.id + '';
        setPlayer2();
    }
});

watch(() => spulti.value.match, refreshPlayers);

const loadPlayers = (newMappings) => {
    baseMappings.value = newMappings;
    mappingsLoaded.value = true;
    refreshPlayers();
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

// Only flips the selection, like picking players it shows on the next transition
const swapPlayers = () => {
    [player1Id.value, player2Id.value] = [player2Id.value, player1Id.value];
    [loadedPlayer1.value, loadedPlayer2.value] = [loadedPlayer2.value, loadedPlayer1.value];
    [ingamePlayer1.value, ingamePlayer2.value] = [ingamePlayer2.value, ingamePlayer1.value];
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        statePlayers.value = [state?.currentMatch?.players?.player1, state?.currentMatch?.players?.player2];
        refreshPlayers();
        // Only when the players on screen changed, other modules refresh the state too and that wiped picks not shown yet
        const playersKey = JSON.stringify([state?.currentMatch?.players?.player1?.id, state?.currentMatch?.players?.player2?.id, state?.currentMatch?.ingame]);
        if (playersKey !== lastStatePlayers) {
            lastStatePlayers = playersKey;
            player1Id.value = state?.currentMatch?.players?.player1?.id ? state?.currentMatch?.players?.player1?.id + '' : player1Id.value;
            player2Id.value = state?.currentMatch?.players?.player2?.id ? state?.currentMatch?.players?.player2?.id + '' : player2Id.value;
            loadedPlayer1.value = state?.currentMatch?.players?.player1 ?? loadedPlayer1.value;
            loadedPlayer2.value = state?.currentMatch?.players?.player2 ?? loadedPlayer2.value;
            ingamePlayer1.value = state?.currentMatch?.ingame?.player1 ?? ingamePlayer1.value;
            ingamePlayer2.value = state?.currentMatch?.ingame?.player2 ?? ingamePlayer2.value;
        }
        // Same player with a refreshed profile, a transition would put the old one back otherwise
        const fresh = state?.currentMatch?.players ?? {};
        if (fresh.player1 && String(fresh.player1.id) === String(loadedPlayer1.value?.id)) loadedPlayer1.value = fresh.player1;
        if (fresh.player2 && String(fresh.player2.id) === String(loadedPlayer2.value?.id)) loadedPlayer2.value = fresh.player2;
        currentMatch.value = state?.currentMatch ?? {};
        scores.value = state?.currentMatch?.scores ?? {};
        historyLength.value = state?.currentMatch?.history?.length ?? 0;
        const players = state?.currentMatch?.players;
        matchPlayerIds.value = players?.player1 || players?.player2 ? [players?.player1?.id, players?.player2?.id].map(String).sort() : [];
        guessIngamePlayers();
        stateLoaded.value = true;
        autoSpulti.value = state?.autoSpulti ?? false;
        pickBanVisible.value = state?.pickBanVisible ?? false;
        coinFlipKey.value = state?.coinFlipKey ?? null;
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

<style lang="scss" scoped>
.spulti-steps {
    margin: 0 0 15px;
    padding-left: 25px;
    opacity: 0.7;
    font-size: 0.9em;
    line-height: 1.6;
}
</style>
