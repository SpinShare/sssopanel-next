<template>
    <AppLayout title="Match (Results)">
        <SpinInput
            label="Match"
            :hint="matchHint"
            type="horizontal"
        />
        <SpinInput
            v-if="historyLength > 1"
            label="Set"
            :hint="`${resultsSlide + 1} of ${historyLength}${slideCountdown}`"
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
                    :disabled="resultsSlide >= historyLength - 1"
                    v-tooltip="'Next Set'"
                />
            </div>
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
                v-tooltip="'New Match (clears scores and sets, keeps players)'"
            />
            <SpinButton
                @click="transition"
                icon="record"
                color="primary"
                :disabled="!historyLength"
                v-tooltip="historyLength ? 'Transition to screen' : 'No sets counted yet'"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import { computed, inject, onMounted, onUnmounted, ref, watch } from 'vue';
import { matchWinner } from '@/modules/useSrxd';
import { nextRecapSlideAt, resetMatch, showMatchResults } from '@/modules/useWinnerPrompt';
import router from '@/router';
const emitter = inject('emitter');

const refDenIsVisible = ref(false);
const players = ref({});
const ingame = ref({});
const scores = ref({});
const historyLength = ref(0);
const resultsSlide = ref(0);
const recapVisible = ref(false);

const playerName = (key) => players.value[key]?.spinshareProfile?.username || ingame.value[key] || (key === 'player1' ? 'Player 1' : 'Player 2');

const matchHint = computed(() => {
    const winner = matchWinner(scores.value);
    const score = `${playerName('player1')} ${scores.value.score?.player1 ?? 0} - ${scores.value.score?.player2 ?? 0} ${playerName('player2')}`;
    if (!historyLength.value) return `${score}, no sets counted yet`;
    return winner ? `${score}, ${playerName(winner)} takes the match` : `${score}, not won yet (the screen shows the last set only)`;
});

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

const slideCountdown = computed(() => {
    if (!recapVisible.value) return ', not on screen';
    return nextRecapSlideAt.value ? `, next set in ${Math.max(0, Math.ceil((nextRecapSlideAt.value - now.value) / 1000))}s` : '';
});

const send = (command, data) => {
    window.electronAPI.send('message', JSON.stringify({ command, data }));
};

const setResultsSlide = (slide) => {
    resultsSlide.value = Math.min(historyLength.value - 1, Math.max(0, slide));
    send('state-set', { currentMatch: { resultsSlide: resultsSlide.value } });
};

const transition = () => {
    resultsSlide.value = 0;
    recapVisible.value = true;
    showMatchResults();
};

// Back to overview to pick the next players, or transition right away for a rematch
const newMatch = () => {
    resetMatch(Number(scores.value.sets?.max ?? 3));
    router.push('/panel/match-overview');
};

const toggleRefDen = () => {
    refDenIsVisible.value = !refDenIsVisible.value;
    send('state-set', { refDenIsVisible: refDenIsVisible.value });
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        const match = state?.currentMatch ?? {};
        refDenIsVisible.value = state?.refDenIsVisible ?? false;
        players.value = match.players ?? {};
        ingame.value = match.ingame ?? {};
        scores.value = match.scores ?? {};
        historyLength.value = match.history?.length ?? 0;
        resultsSlide.value = match.resultsSlide ?? 0;
        recapVisible.value = match.recapVisible ?? false;
    });

    send('state-get');
});

onUnmounted(() => {
    emitter.off('state-get-response');
});
</script>

<style lang="scss" scoped></style>
