import { inject, onMounted, onUnmounted, ref } from 'vue';
import router from '@/router';
import { withFreshResults } from '@/modules/stateMerge';
import useSrxd, { bestAttempts, matchWinner, pickResults, player1Won as isPlayer1Winner, stripRichText } from '@/modules/useSrxd';
import VoiceBotService from '@/modules/VoiceBotService';
import { sideMap, spultiKeepsScore, spultiScores, spultiSetResults } from '@/modules/useSpulti';

// How long auto results stay up, the next song loading hides them earlier
const AUTO_RESULTS_DURATION = 30000;
// Auto progress spreads the match results carousel over this
const RESULTS_CAROUSEL_DURATION = 60000;
// The deciding set's own results stay up this long before the match results screen takes over (auto results or auto recap)
const LAST_SET_DURATION = 15000;

// Key order independent, the backend's merge keeps the old order of keys already in the state
const stableJson = (value) =>
    JSON.stringify(value, (key, v) =>
        v && typeof v === 'object' && !Array.isArray(v)
            ? Object.fromEntries(
                  Object.keys(v)
                      .sort()
                      .map((k) => [k, v[k]]),
              )
            : v,
    );

// When auto progress moves the recap to the next set or starts it (ms timestamp), null while it isn't counting down
export const nextRecapSlideAt = ref(null);

// recapVisible marks the match results screen as up, the recap timers below follow it
export const showMatchResults = () => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: { currentMatch: { recapVisible: true, resultsSlide: 0 } },
        }),
    );
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'match-results',
            },
        }),
    );
};

// Fresh scores and no sets for the next match, players, links, chart and set count stay so a rematch needs nothing else.
// Explicit values instead of null, screens keep their old value when a key is null
export const resetMatch = (maxSets = 3) => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-set',
            data: {
                currentMatch: {
                    scores: { sets: { current: 1, max: maxSets }, score: { player1: 0, player2: 0 } },
                    history: [],
                    resultsVisible: false,
                    recapVisible: false,
                    resultsSlide: 0,
                    lastSet: null,
                    replay: null,
                },
            },
        }),
    );
};

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
    let lastPicked = null;
    // Results waiting out state.resultsDelay (seconds)
    let delayed = null;
    let delayTimer = null;
    // The spulti match the backend follows right now, the state keeps spultiMatchId after unfollowing
    let followedSpultiMatchId = null;
    let spulti = null;
    // How many of a spulti match's sets were posted, here and in the state (spultiPosted), the state lags behind the set
    let postedFor = null;
    let postedCount = 0;
    // Spulti's set count when the song loaded, a set past it is this round's. null = not known (panel opened mid round)
    let setsAtLoading = null;
    // The results up are the feed's, spulti's counted ones replace them once its set is in
    let swapResults = false;
    let stateLoaded = false;

    useSrxd((message) => {
        // App.vue also hosts the screens, those never prompt
        if (!router.currentRoute.value.path.startsWith('/panel')) return;

        if (message.type === 'phase' && message.phase === 'loading') {
            setsAtLoading = spulti?.match ? spulti.match.sets?.length ?? 0 : null;
            swapResults = false;
            // Rounds count up again in a new room, so only the current song counts as prompted
            lastPromptedRound = null;
            resultsStale = true;
            clearTimeout(autoHideTimer);
            clearTimeout(delayTimer);
            delayed = null;
            // The next song is coming up, the old results would cover it
            if (state.currentMatch?.resultsVisible || state.currentMatch?.recapVisible) setState({ currentMatch: { resultsVisible: false, recapVisible: false } });
        }
        if (message.type !== 'results' || message.results?.round === lastPromptedRound) return;

        // The game can send complete results before every count is in, with a delay set the latest message for the
        // round wins once it runs out
        if (delayed && delayed.round === message.results?.round) {
            // A partial one (someone still finishing) must not replace complete results with fewer players
            if (message.results.complete || (message.results.entries?.length ?? 0) >= (delayed.entries?.length ?? 0)) delayed = message.results;
            return;
        }
        if (!message.results?.complete) return;

        const delay = Number(state.resultsDelay ?? 0);
        if (delay > 0) {
            delayed = message.results;
            clearTimeout(delayTimer);
            delayTimer = setTimeout(() => {
                const results = delayed;
                delayed = null;
                if (results) requestPrompt(results);
            }, delay * 1000);
            return;
        }
        requestPrompt(message.results);
    });

    // Players and scores may have changed on any page, ask for the latest state first
    const requestPrompt = (results) => {
        pendingResults = results;
        window.electronAPI.send(
            'message',
            JSON.stringify({
                command: 'state-get',
            }),
        );
    };

    const promptWinner = (results) => {
        const match = state.currentMatch ?? {};
        const ingame1 = match.ingame?.player1;
        const ingame2 = match.ingame?.player2;
        const auto = state.autoResults === true;
        if ((!auto && state.winnerPrompt === false) || !ingame1 || !ingame2) return;

        let picked = pickResults(results, match.ingame);
        if (!picked.player1 && !picked.player2) return;
        if (spultiKeepsScore(spulti, match)) return showSpultiResults(picked, auto);
        // A replayed set keeps each player's best attempt, only for the same chart
        const replay = match.replay;
        if (replay && (!replay.fileReference || replay.fileReference === match.chart?.fileReference)) picked = bestAttempts(replay, picked);
        const entry1 = picked.player1;
        const entry2 = picked.player2;

        lastPromptedRound = results.round;
        resultsStale = false;

        const player1Won = isPlayer1Winner(entry1, entry2);
        const winner = player1Won ? match.players?.player1 : match.players?.player2;
        const winnerName = winner?.spinshareProfile?.username ?? (player1Won ? ingame1 : ingame2);
        const [winnerEntry, loserEntry] = player1Won ? [entry1, entry2] : [entry2, entry1];
        // Auto results skip the prompt: the point is added, the results show and hide again after a while.
        // A won match moves on to the match results screen instead (syncRecapTimer)
        if (auto) {
            const matchWon = applyChoice(picked, player1Won, true);
            clearTimeout(autoHideTimer);
            if (matchWon) return;
            autoHideTimer = setTimeout(() => {
                if (state.currentMatch?.resultsVisible) setState({ currentMatch: { resultsVisible: false } });
            }, AUTO_RESULTS_DURATION);
            return;
        }

        const scoreLine = winnerEntry && loserEntry ? ` (${winnerEntry.score.toLocaleString()} vs ${loserEntry.score.toLocaleString()})` : '';

        emitter.emit('alert-show', {
            title: `${winnerName} won!`,
            message: `${stripRichText(match.chart?.title ?? 'This round')}${scoreLine}. Add a point and move on to the next set?`,
            switches: [{ key: 'showResults', label: 'Show results on stream', value: true }],
            actions: [
                { label: 'Replay', click: ({ showResults }) => applyChoice(picked, null, showResults, true) },
                { label: 'No', click: ({ showResults }) => applyChoice(picked, null, showResults) },
                {
                    label: 'Yes',
                    color: 'primary',
                    click: ({ showResults }) => applyChoice(picked, player1Won, showResults),
                },
            ],
        });
    };

    // This round's spulti set, once it's in
    const roundSet = () => (setsAtLoading !== null && (spulti?.match?.sets?.length ?? 0) > setsAtLoading ? spulti.match.sets[spulti.match.sets.length - 1] : null);
    // A hand point has no results of its own, the feed's stay with spulti's winner
    const countedResults = (set, picked) => {
        const counted = spultiSetResults(set, sideMap(spulti.match, state.currentMatch?.players));
        return set.result ? counted : { ...picked, pointTo: counted.pointTo };
    };

    // Spulti counts the point (and any lag replay), so this only shows the round's results
    const showSpultiResults = (picked, auto) => {
        lastPromptedRound = picked.round;
        resultsStale = false;
        const show = () => {
            if (resultsStale) return;
            const counted = roundSet();
            // What spulti counted: the better run after a lag replay, the point a ref gave by hand
            // pointTo always written, the state merges into the last round's results
            const results = counted ? countedResults(counted, picked) : { ...picked, pointTo: null };
            swapResults = !counted && setsAtLoading !== null;
            setState({ currentMatch: { results, resultsVisible: true } });
            clearTimeout(autoHideTimer);
            // The deciding set moves on to the match results screen instead (syncRecapTimer)
            autoHideTimer = setTimeout(() => {
                if (state.currentMatch?.resultsVisible && !matchWinner(state.currentMatch?.scores)) setState({ currentMatch: { resultsVisible: false } });
            }, AUTO_RESULTS_DURATION);
        };
        if (auto) return show();
        const match = state.currentMatch ?? {};
        emitter.emit('alert-show', {
            title: 'Round over!',
            message: `${stripRichText(match.chart?.title ?? 'This round')}. spulti counts the point, show the results on stream?`,
            actions: [
                { label: 'No', click: () => {} },
                { label: 'Show', color: 'primary', click: show },
            ],
        });
    };

    // Spulti keeps the score: the panel's scores and history follow its sets on every change, so undos and lag
    // replays on spulti undo the point here too. New sets get posted like the panel's own, spultiPosted = how many of
    // that match's sets were posted, so a reload or reconnect doesn't post them again
    const syncSpultiScore = () => {
        const current = state.currentMatch ?? {};
        if (!spultiKeepsScore(spulti, current)) {
            // Relinking counts from what spulti has then (spultiPosted is cleared on unlink), sets from the hand
            // counted stretch aren't posted again
            postedFor = null;
            return;
        }
        const matchId = spulti.match.id;
        const { scores, history } = spultiScores(spulti.match, current.players, current.scores?.sets?.max);

        const statePosted = current.spultiPosted?.matchId === matchId ? current.spultiPosted.count : undefined;
        if (postedFor !== matchId) {
            postedFor = matchId;
            // Following a match that already has sets: those were before our time, nothing to post
            postedCount = statePosted ?? history.length;
        }
        // An undone set that comes back is posted again, it may have changed
        postedCount = Math.min(postedCount, history.length);
        const newSet = history.length > postedCount;
        postedCount = history.length;

        // Compared to the state, not to what was sent, so a hand edit on another page gets put back
        const same = stableJson({ scores, history }) === stableJson({ scores: current.scores, history: current.history ?? [] });
        if (!same || statePosted !== postedCount) {
            const update = { scores, history, lastSet: null, replay: null, spultiPosted: { matchId, count: postedCount } };
            const wasWon = !!matchWinner(current.scores);
            const won = !!matchWinner(scores);
            // The deciding set shows on its own first, same as counting it here. An undone deciding set takes the recap down
            if (won && !wasWon) Object.assign(update, { resultsSlide: 0, recapVisible: false });
            // A deciding point with no results up (given by hand, results hidden) still gets its card, auto results
            // moves on to the recap from there
            if (won && !wasWon && state.autoResults === true && !current.resultsVisible && !resultsStale) Object.assign(update, { results: { ...history[history.length - 1].results }, resultsVisible: true });
            if (!won && current.recapVisible) update.recapVisible = false;
            setState({ currentMatch: update });
        }

        const counted = roundSet();
        if (swapResults && counted && current.resultsVisible) {
            swapResults = false;
            setState({ currentMatch: { results: countedResults(counted, current.results ?? {}) } });
        }

        if (newSet) {
            const charts = (spulti.match.sets ?? []).filter((set) => set.winner).map((set) => set.chart);
            const last = history[history.length - 1];
            const chart = charts[charts.length - 1];
            lastPicked = {
                picked: last.results,
                set: history.length,
                scores,
                history: matchWinner(scores) ? history : [],
                outcome: last.winner,
                chart: chart && { title: stripRichText(chart.title ?? ''), artist: stripRichText(chart.artist ?? ''), charter: stripRichText(chart.charter ?? ''), id: chart.spinshareId ?? null },
            };
            postResults();
        }
    };

    // picked is pickResults() shaped, player1Won is null when no point should be added. replay keeps the
    // results so the next attempt at this set is merged with them. Returns whether this point won the match
    const applyChoice = (picked, player1Won, showResults, replay = false) => {
        const scoresBefore = state.currentMatch?.scores ?? {};
        const currentMatch = {
            replay: replay ? { ...picked, fileReference: state.currentMatch?.chart?.fileReference ?? null } : null,
            // Lets the panel undo this set and replay it later, e.g. after auto results already counted it
            lastSet: replay ? null : { scores: scoresBefore, results: picked, counted: player1Won !== null, fileReference: state.currentMatch?.chart?.fileReference ?? null },
        };
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
                results: picked,
                // Who got the point, not always the higher score when the ref picks
                winner: player1Won ? 'player1' : 'player2',
            });
            currentMatch.history = history;

            matchWon = !matchWinner(state.currentMatch?.scores) && !!matchWinner(currentMatch.scores);
            // The deciding set shows on its own first, the recap starting at set 1 right away was confusing
            if (matchWon) {
                currentMatch.resultsSlide = 0;
                currentMatch.recapVisible = false;
            }
        }

        if (showResults && !resultsStale) {
            currentMatch.results = { ...picked, pointTo: null };
            currentMatch.resultsVisible = true;
        }

        // What this set left the scores at, replaying only works while they're unchanged (not after a new match or edits)
        if (currentMatch.lastSet) currentMatch.lastSet.scoresAfter = currentMatch.scores ?? scoresBefore;
        setState({ currentMatch });
        lastPicked = { picked, set: Number(scoresBefore.sets?.current ?? 1), scores: currentMatch.scores ?? scoresBefore, history: matchWon ? currentMatch.history : [], outcome: replay ? 'replay' : player1Won === null ? 'none' : player1Won ? 'player1' : 'player2' };
        postResults();
        return matchWon;
    };

    // Posts the set to the ref channel through the local voice bot, the hosted one doesn't take commands
    const postResults = () => {
        if (state.postResults !== true || !lastPicked) return;

        const { picked, set, scores, history, outcome, chart } = lastPicked;
        lastPicked = null;
        const match = state.currentMatch ?? {};
        const player = (key) => ({
            name: match.players?.[key]?.spinshareProfile?.username ?? picked[key]?.name ?? match.ingame?.[key] ?? 'Unknown',
            entry: picked[key],
        });

        VoiceBotService.request('post_results', {
            results: {
                // spulti's sets bring their own chart, the one on screen may have moved on already
                chart: chart ?? {
                    title: stripRichText(match.chart?.title ?? ''),
                    artist: stripRichText(match.chart?.artist ?? ''),
                    charter: stripRichText(match.chart?.charter ?? ''),
                    id: match.chart?.id ?? null,
                },
                set,
                sets: Number(scores.sets?.max ?? 3),
                score: { player1: Number(scores.score?.player1 ?? 0), player2: Number(scores.score?.player2 ?? 0) },
                outcome,
                matchWinner: matchWinner(scores),
                // Only from the set that won the match, the bot adds a match results embed for it
                history: history.map((entry) => ({
                    title: entry.chart?.title ?? '',
                    player1: entry.results?.player1 ?? null,
                    player2: entry.results?.player2 ?? null,
                    winner: entry.winner ?? (isPlayer1Winner(entry.results?.player1, entry.results?.player2) ? 'player1' : 'player2'),
                })),
                player1: player('player1'),
                player2: player('player2'),
                // The match results embed links the spulti match page
                spultiMatchId: match.spultiMatchId && match.spultiMatchId === followedSpultiMatchId ? match.spultiMatchId : null,
            },
        }).catch((error) => {
            emitter.emit('alert-show', { title: 'Could not post results', message: error.message, actions: [{ label: 'OK', color: 'primary' }] });
        });
    };

    // Auto results and auto recap move from the deciding set's results to the match results screen on their own
    let recapTimer = null;
    let recapTimerFor = false;
    const syncRecapTimer = () => {
        const match = state.currentMatch ?? {};
        const running = (state.autoResults === true || state.resultsAutoProgress === true) && match.resultsVisible === true && match.recapVisible !== true && !!matchWinner(match.scores) && !!match.history?.length;
        if (running === recapTimerFor) return;

        clearTimeout(recapTimer);
        recapTimerFor = running;
        nextRecapSlideAt.value = null;
        if (!running) return;

        nextRecapSlideAt.value = Date.now() + LAST_SET_DURATION;
        recapTimer = setTimeout(() => {
            recapTimerFor = false;
            nextRecapSlideAt.value = null;
            showMatchResults();
            refreshAfterSet = true;
            // Follow the screen so the recap controls are right there
            if (router.currentRoute.value.path !== '/panel/match-results') router.push('/panel/match-results');
        }, LAST_SET_DURATION);
    };

    // Auto progress moves the match results carousel along and stops on the last set, a manual step restarts the wait
    const syncSlideTimer = () => {
        const match = state.currentMatch ?? {};
        const count = match.history?.length ?? 0;
        const slide = match.resultsSlide ?? 0;
        const running = state.resultsAutoProgress === true && match.recapVisible === true && !!matchWinner(match.scores) && slide < count - 1;
        const key = running ? `${slide}/${count}` : null;
        if (key === slideTimerFor) return;

        clearTimeout(slideTimer);
        slideTimerFor = key;
        if (!recapTimerFor) nextRecapSlideAt.value = null;
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
                data: withFreshResults(data, state),
            }),
        );

        // Pages only listen for state-get-response, refreshed once the set is through (IPC messages can be handled out of order)
        refreshAfterSet = true;
    };

    // Pages clear every listener of the state events they use when they unmount, a wildcard listener survives that
    const onAnyMessage = (type, data) => {
        // Screens get the same state, their recap timers would push the stream screen to the panel page
        if (!router.currentRoute.value.path.startsWith('/panel')) return;
        if (type === 'spulti-update' || type === 'spulti-get-response') {
            followedSpultiMatchId = data?.url ? data.match?.id ?? null : null;
            spulti = data;
            if (stateLoaded) syncSpultiScore();
            return;
        }
        if (type !== 'state-get-response' && type !== 'state-set-response') return;

        state = data ?? {};
        stateLoaded = true;
        syncSpultiScore();
        syncRecapTimer();
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
        clearTimeout(delayTimer);
        clearTimeout(slideTimer);
        clearTimeout(recapTimer);
        nextRecapSlideAt.value = null;
        emitter.off('*', onAnyMessage);
    });
}
