import { inject, onMounted, onUnmounted } from 'vue';
import { coinResult, findMapping, sideMap, spultiGuest, spultiLinked, spultiPhase } from '@/modules/useSpulti';
import useSrxd, { guessIngameName } from '@/modules/useSrxd';
import { resetMatch } from '@/modules/useWinnerPrompt';

// The coin's flip, same as spulti's own coin toss (ScreenCoin's animations run this long)
export const COIN_FLIP_TIME = 1600;
// The board on match-ingame opens with the coin: the flip, the result held a bit, then the charts come in
export const COIN_INTRO_DURATION = 5000;
// After a round the board waits out the results delay plus this, so auto results come up first, then until they hide
const BOARD_AFTER_ROUND = 3000;

const isPanel = () => window.location.hash.startsWith('#/panel');
// Screens that have the pick/ban board, the toss waits for one of them to be live
const BOARD_ROUTES = ['/screen/match-overview', '/screen/match-ingame'];

// Pages only listen for state-get-response, so one follows once the set is through
let refreshAfterSet = false;
const setState = (data) => {
    window.electronAPI.send('message', JSON.stringify({ command: 'state-set', data }));
    refreshAfterSet = true;
};

// key = coinResult().key, the toss that got its flip on match overview or ingame. Screens up right then play the intro, a screen
// that loads later only shows the small landed coin in the board header. Shows the board too, the coin lives in it
export const playCoinFlip = (key) => setState({ coinFlipAt: Date.now(), coinFlipKey: key, pickBanVisible: true });

export const setPickBanVisible = (visible) => setState({ pickBanVisible: visible });

// The Twitch prediction on match overview (column) and ingame (over the spinsha.re banner while voting is open)
export const setChatPickVisible = (visible) => setState({ chatPickVisible: visible });

// Panel only (App.vue). A different spulti match means a new match: scores reset to its best of, see syncMatch.
// With autoSpulti on: a toss that wasn't flipped yet plays once match overview or ingame is the live screen (held
// until then), a ban or pick turn shows the pick/ban board, the song loading hides it
export default function useSpultiOverlays() {
    const emitter = inject('emitter');
    const game = useSrxd();

    let state = {};
    let lastMatchId;
    let route = '';
    let spulti = null;
    // Sent but maybe not in the state yet
    let flipSent = null;
    let lastPhase = null;
    // Sets played so far, more means a round just ended
    let lastSets = null;
    // When the board may come back after a round (ms timestamp), null when nothing is waiting
    let boardAt = null;
    let boardTimer;
    let stateLoaded = false;
    let spultiMatch = null;
    // The spulti match a reset or prompt already went out for, the state lags behind the set
    let handledMatchId = null;
    // currentEvent.playerMapping, null until settings-get-full is in
    let mappings = null;
    // Subs already sent, the state lags behind the set
    const subsSent = new Set();
    // The in-game names last taken from spulti
    let ingameSent = null;

    // currentMatch.spultiMatchId is the spulti match the scores belong to. A new one resets them
    const syncMatch = () => {
        const match = spultiMatch;
        if (!stateLoaded || !match?.id || match.id === handledMatchId) return;
        const current = state.currentMatch ?? {};
        if (current.spultiMatchId === match.id) {
            handledMatchId = match.id;
            return;
        }
        handledMatchId = match.id;

        // Spulti keeps the score from here on (useWinnerPrompt), so whatever was counted by hand makes way
        resetMatch(Number(match.bestOf) || Number(current.scores?.sets?.max ?? 3));
        // spultiPosted too, coming back to a match later starts counting its posts fresh
        setState({ currentMatch: { spultiMatchId: match.id, spultiPosted: null, spultiScoreUnlinked: false } });
    };

    // An organizer subbed someone in on spulti: that side's player is the sub now (subbedFor = who they replaced).
    // The state follows, so the screens and the results posts show the sub. Only for the spulti match the scores
    // belong to, a link to some other match never touches the players
    const syncSubs = () => {
        const match = spultiMatch;
        const current = state.currentMatch ?? {};
        if (!stateLoaded || !mappings || !match?.id || current.spultiMatchId !== match.id) return;

        const sides = sideMap(match, current.players);
        const members = [...(game.value.room?.players ?? []), ...(game.value.room?.spectators ?? [])];
        for (const side of ['player1', 'player2']) {
            const sub = match[side];
            const ours = sides[side];
            const key = `${match.id}:${side}:${sub?.name}`;
            if (!sub || !match.subbedFor?.[side] || subsSent.has(key)) continue;
            if (current.players?.[ours] && findMapping(sub, [current.players[ours]])) continue;
            subsSent.add(key);

            const player = findMapping(sub, mappings) ?? spultiGuest(sub);
            // Cleared first, state-set deep merges and would keep the old player's fields
            setState({ currentMatch: { players: { [ours]: null } } });
            setState({ currentMatch: { players: { [ours]: player }, ingame: { [ours]: guessIngameName(player, members) ?? '' } } });
        }
    };

    // Who's who in the room, as the referee mapped it on spulti. Spulti wins for the match it keeps the score of, a side it
    // doesn't know yet (or an older spulti without names) keeps the guess
    const syncIngame = () => {
        const current = state.currentMatch ?? {};
        const names = spultiMatch?.ingame;
        if (!stateLoaded || !names || !spultiLinked(spulti, current)) return;
        const sides = sideMap(spultiMatch, current.players);
        const ingame = {};
        for (const side of ['player1', 'player2']) if (names[side] && current.ingame?.[sides[side]] !== names[side]) ingame[sides[side]] = names[side];
        // Matching again forgets what was sent, so a later hand edit gets put back too
        if (!Object.keys(ingame).length) {
            ingameSent = null;
            return;
        }
        const key = `${spultiMatch.id}:${JSON.stringify(ingame)}`;
        if (key === ingameSent) return;
        ingameSent = key;
        setState({ currentMatch: { ingame } });
    };

    // The state's coinFlipKey is the toss already flipped, so reloads never flip twice.
    // Only while the strike is still on, later the board just shows the landed coin
    const syncCoin = () => {
        const coin = coinResult(spulti?.match);
        if (!state.autoSpulti || !coin || coin.key === state.coinFlipKey || coin.key === flipSent) return;
        if (!BOARD_ROUTES.includes(route) || (spulti.match.sets?.length ?? 0) > 0) return;
        flipSent = coin.key;
        playCoinFlip(coin.key);
    };

    const cancelBoard = () => {
        boardAt = null;
        clearTimeout(boardTimer);
    };

    // The board after a round: once the wait is over and the results are gone again. If the loser already picked,
    // it still comes up so the pick shows
    const syncBoard = () => {
        if (boardAt === null) return;
        if (!state.autoSpulti || state.pickBanVisible) return cancelBoard();
        if (Date.now() < boardAt || state.currentMatch?.resultsVisible || state.currentMatch?.recapVisible) return;
        cancelBoard();
        if (spultiPhase(spulti?.match, spulti?.mappool)?.type !== 'finished') setPickBanVisible(true);
    };

    const onSpulti = (data) => {
        spulti = data;
        spultiMatch = data?.match ?? null;
        syncMatch();
        syncSubs();
        syncIngame();
        syncCoin();

        const phase = spultiPhase(data?.match, data?.mappool)?.type ?? null;
        const matchId = data?.match?.id ?? null;
        const sets = data?.match?.sets?.length ?? 0;
        if (matchId !== lastMatchId || !state.autoSpulti) {
            lastMatchId = matchId;
            lastPhase = phase;
            lastSets = sets;
            cancelBoard();
            return;
        }

        // A set undone, whatever was waiting is for a round that didn't happen. The board stays as the operator has it
        if (sets < lastSets) cancelBoard();
        else if (sets > lastSets && phase !== 'finished') {
            cancelBoard();
            boardAt = Date.now() + Number(state.resultsDelay ?? 0) * 1000 + BOARD_AFTER_ROUND;
            boardTimer = setTimeout(syncBoard, boardAt - Date.now());
        } else if (boardAt === null && (phase === 'ban' || phase === 'pick') && phase !== lastPhase && !state.pickBanVisible) setPickBanVisible(true);
        lastPhase = phase;
        lastSets = sets;
    };

    const onAnyMessage = (type, data) => {
        if (!isPanel()) return;

        if (type === 'state-get-response' || type === 'state-set-response') {
            state = data ?? {};
            stateLoaded = true;
            syncMatch();
            syncSubs();
            syncIngame();
            syncCoin();
            syncBoard();
        }
        if (type === 'settings-get-full-response') {
            mappings = data?.['currentEvent.playerMapping'] ?? [];
            syncSubs();
        }
        if (type === 'state-set-response' && refreshAfterSet) {
            refreshAfterSet = false;
            window.electronAPI.send('message', JSON.stringify({ command: 'state-get' }));
        }
        if (type === 'spulti-get-response' || type === 'spulti-update') onSpulti(data);
        if ((type === 'screen-navigate-response' || type === 'current-route-get-response') && data?.Path) {
            route = data.Path;
            syncCoin();
        }
        if (type === 'srxd-update' && data?.type === 'phase' && data.phase === 'loading' && state.autoSpulti) {
            cancelBoard();
            if (state.pickBanVisible) setPickBanVisible(false);
        }
    };

    onMounted(() => {
        emitter.on('*', onAnyMessage);
        if (!isPanel()) return;
        window.electronAPI.send('message', JSON.stringify({ command: 'state-get' }));
        window.electronAPI.send('message', JSON.stringify({ command: 'spulti-get' }));
        window.electronAPI.send('message', JSON.stringify({ command: 'current-route-get' }));
        window.electronAPI.send('message', JSON.stringify({ command: 'settings-get-full' }));
    });

    onUnmounted(() => {
        emitter.off('*', onAnyMessage);
        cancelBoard();
    });
}
