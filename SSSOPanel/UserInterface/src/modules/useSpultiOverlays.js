import { inject, onMounted, onUnmounted } from 'vue';
import { coinResult, findMapping, sideMap, spultiGuest, spultiPhase } from '@/modules/useSpulti';
import useSrxd, { guessIngameName } from '@/modules/useSrxd';
import { resetMatch } from '@/modules/useWinnerPrompt';

// The coin's flip, same as spulti's own coin toss (ScreenCoin's animations run this long)
export const COIN_FLIP_TIME = 1600;
// The board on match-ingame opens with the coin: the flip, the result held a bit, then the charts come in
export const COIN_INTRO_DURATION = 5000;

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
    let stateLoaded = false;
    let spultiMatch = null;
    // The spulti match a reset or prompt already went out for, the state lags behind the set
    let handledMatchId = null;
    // currentEvent.playerMapping, null until settings-get-full is in
    let mappings = null;
    // Subs already sent, the state lags behind the set
    const subsSent = new Set();

    // currentMatch.spultiMatchId is the spulti match the scores belong to. A new one resets them, unless the scores
    // were never tied to spulti (counted by hand), then it asks since the link may be for the match being played
    const syncMatch = () => {
        const match = spultiMatch;
        if (!stateLoaded || !match?.id || match.id === handledMatchId) return;
        const current = state.currentMatch ?? {};
        if (current.spultiMatchId === match.id) {
            handledMatchId = match.id;
            return;
        }
        handledMatchId = match.id;

        const maxSets = Number(match.bestOf) || Number(current.scores?.sets?.max ?? 3);
        const startNew = () => {
            resetMatch(maxSets);
            setState({ currentMatch: { spultiMatchId: match.id } });
        };
        const started = (current.history?.length ?? 0) > 0 || Number(current.scores?.score?.player1 ?? 0) + Number(current.scores?.score?.player2 ?? 0) > 0;
        if (current.spultiMatchId || !started) {
            startNew();
            return;
        }

        emitter.emit('alert-show', {
            title: 'New match?',
            message: `Following ${match.player1?.name ?? 'TBD'} vs ${match.player2?.name ?? 'TBD'} on spulti, but the current match still has scores and sets. Clear them for this match?`,
            actions: [
                { label: 'Keep Scores', click: () => setState({ currentMatch: { spultiMatchId: match.id } }) },
                { label: 'New Match', color: 'primary', click: startNew },
            ],
        });
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

    // The state's coinFlipKey is the toss already flipped, so reloads never flip twice.
    // Only while the strike is still on, later the board just shows the landed coin
    const syncCoin = () => {
        const coin = coinResult(spulti?.match);
        if (!state.autoSpulti || !coin || coin.key === state.coinFlipKey || coin.key === flipSent) return;
        if (!BOARD_ROUTES.includes(route) || (spulti.match.sets?.length ?? 0) > 0) return;
        flipSent = coin.key;
        playCoinFlip(coin.key);
    };

    const onSpulti = (data) => {
        spulti = data;
        spultiMatch = data?.match ?? null;
        syncMatch();
        syncSubs();
        syncCoin();

        const phase = spultiPhase(data?.match, data?.mappool)?.type ?? null;
        const matchId = data?.match?.id ?? null;
        if (matchId !== lastMatchId || !state.autoSpulti) {
            lastMatchId = matchId;
            lastPhase = phase;
            return;
        }

        if ((phase === 'ban' || phase === 'pick') && phase !== lastPhase && !state.pickBanVisible) setPickBanVisible(true);
        lastPhase = phase;
    };

    const onAnyMessage = (type, data) => {
        if (!isPanel()) return;

        if (type === 'state-get-response' || type === 'state-set-response') {
            state = data ?? {};
            stateLoaded = true;
            syncMatch();
            syncSubs();
            syncCoin();
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
    });
}
