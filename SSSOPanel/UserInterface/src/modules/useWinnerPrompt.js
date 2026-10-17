import { inject, onMounted, onUnmounted, ref } from 'vue';
import router from '@/router';
import useSrxd, { matchWinner, pickResults, player1Won as isPlayer1Winner, stripRichText } from '@/modules/useSrxd';

// How long auto results stay up, the next song loading hides them earlier
const AUTO_RESULTS_DURATION = 30000;
// The match recap carousel gets longer, auto progress spreads its slides over RESULTS_CAROUSEL_DURATION
const AUTO_MATCH_RESULTS_DURATION = 75000;
const RESULTS_CAROUSEL_DURATION = 60000;

// When auto progress moves the recap to the next set (ms timestamp), null while it isn't counting down
export const nextRecapSlideAt = ref(null);

// Used once in App.vue, so the prompt shows up no matter which panel page is open
export default function useWinnerPrompt() {
    const emitter = inject('emitter');

    let state = {};
    let pendingResults = null;
    let lastPromptedRound = null;
    let refreshAfterSet = false;
    // Set once the next song loads, a late answer to the prompt then still counts the point but won't cover gameplay
    let resultsStale = false;
    let autoHideTimer = null;
    let slideTimer = null;
    let slideTimerFor = null;

    useSrxd((message) => {
        // App.vue also hosts the screens, those never prompt
        if (!router.currentRoute.value.path.startsWith('/panel')) return;

        if (message.type === 'phase' && message.phase === 'loading') {
            // Rounds count up again in a new room, so only the current song counts as prompted
            lastPromptedRound = null;
            resultsStale = true;
            clearTimeout(autoHideTimer);
            // The next song is coming up, the old results would cover it
            if (state.currentMatch?.resultsVisible) setState({ currentMatch: { resultsVisible: false } });
        }
        if (message.type !== 'results' || !message.results?.complete || message.results.round === lastPromptedRound) return;

        // Players and scores may have changed on any page, ask for the latest state first
        pendingResults = message.results;
        window.electronAPI.send(
            'message',
            JSON.stringify({
                command: 'state-get',
            }),
        );
    });

    const promptWinner = (results) => {
        const match = state.currentMatch ?? {};
        const ingame1 = match.ingame?.player1;
        const ingame2 = match.ingame?.player2;
        const auto = state.autoResults === true;
        if ((!auto && state.winnerPrompt === false) || !ingame1 || !ingame2) return;

        const entry1 = results.entries.find((entry) => entry.name === ingame1);
        const entry2 = results.entries.find((entry) => entry.name === ingame2);
        if (!entry1 && !entry2) return;

        lastPromptedRound = results.round;
        resultsStale = false;

        const player1Won = isPlayer1Winner(entry1, entry2);
        const winner = player1Won ? match.players?.player1 : match.players?.player2;
        const winnerName = winner?.spinshareProfile?.username ?? (player1Won ? ingame1 : ingame2);
        const [winnerEntry, loserEntry] = player1Won ? [entry1, entry2] : [entry2, entry1];
        // Auto results skip the prompt: the point is added, the results show and hide again after a while
        if (auto) {
            const matchWon = applyChoice(results, player1Won, true);
            clearTimeout(autoHideTimer);
            autoHideTimer = setTimeout(
                () => {
                    if (state.currentMatch?.resultsVisible) setState({ currentMatch: { resultsVisible: false } });
                },
                matchWon ? AUTO_MATCH_RESULTS_DURATION : AUTO_RESULTS_DURATION,
            );
            return;
        }

        const scoreLine = winnerEntry && loserEntry ? ` (${winnerEntry.score.toLocaleString()} vs ${loserEntry.score.toLocaleString()})` : '';

        emitter.emit('alert-show', {
            title: `${winnerName} won!`,
            message: `${stripRichText(match.chart?.title ?? 'This round')}${scoreLine}. Add a point and move on to the next set?`,
            switches: [{ key: 'showResults', label: 'Show results on stream', value: true }],
            actions: [
                { label: 'No', click: ({ showResults }) => applyChoice(results, null, showResults) },
                {
                    label: 'Yes',
                    color: 'primary',
                    click: ({ showResults }) => applyChoice(results, player1Won, showResults),
                },
            ],
        });
    };

    // player1Won is null when no point should be added. Returns whether this point won the match
    const applyChoice = (results, player1Won, showResults) => {
        const currentMatch = {};
        let matchWon = false;

        if (player1Won !== null) {
            // Defensive, the state may hold strings from older panel versions
            const scores = state.currentMatch?.scores ?? {};
            const maxSets = Number(scores.sets?.max ?? 3);
            const maxPoints = Math.ceil(maxSets / 2);
            const score1 = Number(scores.score?.player1 ?? 0);
            const score2 = Number(scores.score?.player2 ?? 0);

            currentMatch.scores = {
                sets: {
                    current: Math.min(maxSets, Number(scores.sets?.current ?? 1) + 1),
                    max: maxSets,
                },
                score: {
                    player1: player1Won ? Math.min(maxPoints, score1 + 1) : score1,
                    player2: player1Won ? score2 : Math.min(maxPoints, score2 + 1),
                },
            };

            // Every counted set for the recap carousel, the first point of a match starts a fresh one
            const history = score1 + score2 === 0 ? [] : [...(state.currentMatch?.history ?? [])];
            history.push({
                chart: {
                    title: stripRichText(state.currentMatch?.chart?.title ?? ''),
                    artist: stripRichText(state.currentMatch?.chart?.artist ?? ''),
                },
                results: pickResults(results, state.currentMatch?.ingame),
            });
            currentMatch.history = history;

            matchWon = !matchWinner(state.currentMatch?.scores) && !!matchWinner(currentMatch.scores);
            if (matchWon) currentMatch.resultsSlide = 0;
        }

        if (showResults && !resultsStale) {
            currentMatch.results = pickResults(results, state.currentMatch?.ingame);
            currentMatch.resultsVisible = true;
        }

        if (Object.keys(currentMatch).length) setState({ currentMatch });
        return matchWon;
    };

    // Auto progress moves the recap carousel along and stops on the last set, a manual step restarts the wait
    const syncSlideTimer = () => {
        const match = state.currentMatch ?? {};
        const count = match.history?.length ?? 0;
        const slide = match.resultsSlide ?? 0;
        const running = state.resultsAutoProgress === true && match.resultsVisible === true && !!matchWinner(match.scores) && slide < count - 1;
        const key = running ? `${slide}/${count}` : null;
        if (key === slideTimerFor) return;

        clearTimeout(slideTimer);
        slideTimerFor = key;
        nextRecapSlideAt.value = null;
        if (!running) return;

        nextRecapSlideAt.value = Date.now() + RESULTS_CAROUSEL_DURATION / count;
        slideTimer = setTimeout(() => {
            slideTimerFor = null;
            nextRecapSlideAt.value = null;
            setState({ currentMatch: { resultsSlide: slide + 1 } });
        }, RESULTS_CAROUSEL_DURATION / count);
    };

    const setState = (data) => {
        window.electronAPI.send(
            'message',
            JSON.stringify({
                command: 'state-set',
                data,
            }),
        );

        // Pages only listen for state-get-response, refreshed once the set is through (IPC messages can be handled out of order)
        refreshAfterSet = true;
    };

    // Pages clear every listener of the state events they use when they unmount, a wildcard listener survives that
    const onAnyMessage = (type, data) => {
        if (type !== 'state-get-response' && type !== 'state-set-response') return;

        state = data ?? {};
        syncSlideTimer();
        if (type === 'state-set-response' && refreshAfterSet) {
            refreshAfterSet = false;
            window.electronAPI.send(
                'message',
                JSON.stringify({
                    command: 'state-get',
                }),
            );
        }
        if (type === 'state-get-response' && pendingResults) {
            const results = pendingResults;
            pendingResults = null;
            promptWinner(results);
        }
    };

    onMounted(() => {
        emitter.on('*', onAnyMessage);
    });

    onUnmounted(() => {
        clearTimeout(autoHideTimer);
        clearTimeout(slideTimer);
        nextRecapSlideAt.value = null;
        emitter.off('*', onAnyMessage);
    });
}
