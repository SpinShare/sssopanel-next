import { inject, onMounted, onUnmounted, ref } from 'vue';
import { stripRichText } from '@/modules/useSrxd';

// The spulti match the backend follows (coin toss, bans, picks), pushed as a full snapshot on every change.
export default function useSpulti() {
    const emitter = inject('emitter');

    const spulti = ref({ url: '', connected: false, error: '', match: null, mappool: null });

    const onSnapshot = (snapshot) => {
        spulti.value = snapshot;
    };

    onMounted(() => {
        emitter.on('spulti-get-response', onSnapshot);
        emitter.on('spulti-update', onSnapshot);

        window.electronAPI.send('message', JSON.stringify({ command: 'spulti-get' }));
    });

    onUnmounted(() => {
        emitter.off('spulti-get-response', onSnapshot);
        emitter.off('spulti-update', onSnapshot);
    });

    return spulti;
}

export const followSpultiMatch = (url) => {
    window.electronAPI.send('message', JSON.stringify({ command: 'spulti-connect', data: { url } }));
};

// The panel's match dropdown, kept between visits to the page until refreshed or the followed site changes.
// requestId drops answers to older requests (page load + refresh racing)
export const spultiMatchList = ref({ matches: [], origin: null, error: '', loading: false, requestId: 0 });
export const loadSpultiMatches = () => {
    const requestId = spultiMatchList.value.requestId + 1;
    spultiMatchList.value = { ...spultiMatchList.value, loading: true, requestId };
    window.electronAPI.send('message', JSON.stringify({ command: 'spulti-matches', data: { requestId } }));
};
export const onSpultiMatches = (data) => {
    if (data?.requestId !== spultiMatchList.value.requestId) return;
    spultiMatchList.value = { ...spultiMatchList.value, loading: false, matches: data?.matches ?? [], origin: data?.origin ?? null, error: data?.error ?? '' };
};
// The site the backend lists matches from: the followed link's, spulti.ellite.dev otherwise (SpultiManager.ListMatches)
export const spultiOrigin = (url) => {
    try {
        const page = new URL(url);
        if (['http:', 'https:'].includes(page.protocol)) return page.origin;
    } catch {
        // ignored
    }
    return 'https://spulti.ellite.dev';
};

// What happened so far, oldest first, e.g. "Ban: alpine bans NM2 (Title)"
export const draftSteps = (match, mappool) => {
    if (!match) return [];

    const name = (side) => match[side]?.name ?? side;
    const slot = (slotId) => {
        const found = mappool?.slots?.find((s) => s.id === slotId);
        return found ? `${found.label} (${found.chart.title})` : 'a chart';
    };

    const steps = [];
    const coin = match.draft?.build?.coin;
    if (coin) steps.push(`Coin toss: ${name(coin)} won`);

    // Same as spulti's flowCredits: bans alternate from the roll's first ban, a pick is the loser of the set before.
    // The first set's pick is just the last chart left after striking
    const other = (side) => (side === 'player1' ? 'player2' : 'player1');
    const roll = match.draft?.actions?.find((a) => a.type === 'roll');
    let bans = 0;
    for (const action of match.draft?.actions ?? []) {
        switch (action.type) {
            case 'roll':
                steps.push(`Roll: ${name(action.firstBan)} bans first`);
                break;
            case 'ban':
                steps.push(`Ban: ${roll ? name(bans % 2 === 0 ? roll.firstBan : other(roll.firstBan)) : '?'} bans ${slot(action.slotId)}`);
                bans++;
                break;
            case 'pick': {
                const winner = match.sets?.[action.forSet - 1]?.winner;
                steps.push(winner ? `Pick: ${name(other(winner))} picks ${slot(action.slotId)}` : `Set ${action.forSet + 1}: ${slot(action.slotId)}`);
                break;
            }
            case 'tiebreak':
                steps.push(`Tiebreak: ${action.chart.title}`);
                break;
        }
    }

    return steps;
};

const otherSide = (side) => (side === 'player1' ? 'player2' : 'player1');

// Port of spulti's matchPhase (packages/shared/src/flow.ts): strike bans alternate from the roll for the first chart,
// after that the loser of each set picks from the charts not played yet, a set without a winner gets a tiebreak
export const spultiPhase = (match, mappool) => {
    if (!match) return null;
    if (match.status === 'finished' || match.winner) return { type: 'finished' };

    const actions = match.draft?.actions ?? [];
    const slots = mappool?.slots ?? [];
    if (match.draft?.build && !actions.some((a) => a.type === 'roll')) return { type: 'build' };

    const sets = match.sets ?? [];
    const index = sets.length;
    // No findLast/at(), OBS browser sources can be older than chrome 97
    const pick = [...actions].reverse().find((a) => (a.type === 'pick' || a.type === 'tiebreak') && a.forSet === index);
    if (pick?.type === 'tiebreak') return { type: 'play', chart: pick.chart, slot: null };
    if (pick?.type === 'pick') {
        const slot = slots.find((s) => s.id === pick.slotId);
        if (slot) return { type: 'play', chart: slot.chart, slot };
    }

    if (index === 0) {
        const roll = actions.find((a) => a.type === 'roll');
        if (!roll) return { type: 'roll' };
        const banned = new Set(actions.filter((a) => a.type === 'ban').map((a) => a.slotId));
        const options = slots.filter((s) => !banned.has(s.id));
        if (options.length === 1) return { type: 'play', chart: options[0].chart, slot: options[0] };
        return { type: 'ban', side: banned.size % 2 === 0 ? roll.firstBan : otherSide(roll.firstBan) };
    }

    const last = sets[sets.length - 1];
    if (!last.winner) return { type: 'tiebreak' };
    return { type: 'pick', side: otherSide(last.winner) };
};

// What happened to every slot: { banned: side, banOrder } (strike), { picked: side } (loser's pick), { played: set index, winner }
export const slotStates = (match, mappool) => {
    const states = {};
    const actions = match?.draft?.actions ?? [];
    const roll = actions.find((a) => a.type === 'roll');
    let bans = 0;
    for (const action of actions) {
        if (action.type === 'ban') {
            // banOrder: the how-manyth ban, spulti shows it on the tile
            states[action.slotId] = { banned: roll ? (bans % 2 === 0 ? roll.firstBan : otherSide(roll.firstBan)) : null, banOrder: bans + 1 };
            bans++;
        }
        if (action.type === 'pick') {
            const winner = match.sets?.[action.forSet - 1]?.winner;
            states[action.slotId] = { picked: winner ? otherSide(winner) : null, forSet: action.forSet };
        }
    }
    // Played charts by chart id, the first set's chart is the one left after striking and has no pick action
    (match?.sets ?? []).forEach((set, index) => {
        const slot = mappool?.slots?.find((s) => s.chart.spinshareId === set.chart?.spinshareId);
        if (slot) states[slot.id] = { ...states[slot.id], banned: undefined, played: index, winner: set.winner };
    });
    return states;
};

// The scores belong to the followed match. snapshot = useSpulti's, currentMatch = the state's
export const spultiLinked = (snapshot, currentMatch) => !!snapshot?.url && !!snapshot.match?.id && currentMatch?.spultiMatchId === snapshot.match.id;

// Spulti keeps the score (sets won, undos, lag replays) for its linked match, unless the broadcaster unlinked the scores
// to count by hand (currentMatch.spultiScoreUnlinked). The live song numbers stay from the game feed
export const spultiKeepsScore = (snapshot, currentMatch) => spultiLinked(snapshot, currentMatch) && !currentMatch.spultiScoreUnlinked;

// One spulti set's results with our sides, pointTo = who spulti gave the point ('none' for a tie, see
// ScreenMatchResults). Hand points have no results
export const spultiSetResults = (set, sides) => ({
    round: set.result?.round ?? 0,
    [sides.player1]: set.result?.player1 ?? null,
    [sides.player2]: set.result?.player2 ?? null,
    pointTo: set.winner ? sides[set.winner] : 'none',
});

// The panel's scores and history from spulti's sets, sides mapped to ours. Ties and sets without a winner don't
// count, same as the panel's own counting
export const spultiScores = (match, players, fallbackMax = 3) => {
    const sides = sideMap(match, players);
    const max = Number(match?.bestOf) || Number(fallbackMax) || 3;
    const score = { player1: 0, player2: 0 };
    const history = [];
    for (const set of match?.sets ?? []) {
        if (!set.winner) continue;
        const ours = sides[set.winner];
        score[ours]++;
        history.push({
            chart: { title: stripRichText(set.chart?.title ?? ''), artist: stripRichText(set.chart?.artist ?? '') },
            results: spultiSetResults(set, sides),
            winner: ours,
        });
    }
    return { scores: { sets: { current: Math.min(max, score.player1 + score.player2 + 1), max }, score }, history };
};

// The coin result worth flipping for: the roll (who bans first) or the custom pool coin toss (who gets the last chart)
export const coinResult = (match) => {
    const roll = match?.draft?.actions?.find((a) => a.type === 'roll');
    if (roll) return { key: `roll-${match.id}`, side: roll.firstBan, text: 'bans first' };
    const coin = match?.draft?.build?.coin;
    if (coin) return { key: `coin-${match.id}`, side: coin, text: 'wins the toss' };
    return null;
};

// Which of our players (state currentMatch.players) a spulti side is, same matching as the overview autofill.
// Returns 'player1' | 'player2' | null
export const ourSide = (match, side, players) => ['player1', 'player2'].find((key) => players?.[key] && findMapping(match?.[side], [players[key]])) ?? null;

// spulti side -> our side ('player1' = left on screen). Falls back to the same side when nobody matches
export const sideMap = (match, players) => {
    const one = ourSide(match, 'player1', players);
    const two = ourSide(match, 'player2', players);
    const flipped = one === 'player2' || (!one && two === 'player1');
    return flipped ? { player1: 'player2', player2: 'player1' } : { player1: 'player1', player2: 'player2' };
};

// Each outcome's share of the coins, null until Twitch reported coins for every outcome. Two shares add up to 100
export const predictionShares = (prediction) => {
    const outcomes = prediction?.outcomes ?? [];
    const total = outcomes.reduce((sum, outcome) => sum + (outcome.points ?? 0), 0);
    if (!total || outcomes.some((outcome) => outcome.points == null)) return null;
    const parts = outcomes.map((outcome) => ({ ...outcome, share: Math.round((outcome.points / total) * 100) }));
    if (parts.length === 2) parts[1].share = 100 - parts[0].share;
    return parts;
};

// How a paid out prediction went, the same call on every screen: called = the side with the most coins won, a dead even
// split (by coins, not the rounded share) has no favorite (called null). Null before it resolved or without coins
export const predictionVerdict = (prediction) => {
    if (prediction?.status !== 'resolved' || !prediction.winner) return null;
    const parts = predictionShares(prediction);
    const won = parts?.find((part) => part.side === prediction.winner);
    if (!won) return null;
    const top = Math.max(...parts.map((part) => part.points));
    const split = parts.every((part) => part.points === top);
    return { side: won.side, name: won.name, share: won.share, called: split ? null : won.points === top };
};

const normalizeName = (name) => (name ?? '').toLowerCase().replace(/[^a-z0-9]/g, '');
// "Prefix | Tag" on start.gg, the tag alone counts too
const nameVariants = (name) =>
    [
        name,
        String(name ?? '')
            .split('|')
            .pop(),
    ]
        .map(normalizeName)
        .filter((x) => x !== '');

// Which match + pairing match-overview last filled its players from, kept across page visits so a manual change sticks
export const autofilled = { key: null };

// The player mapping entry for a spulti match player, by SpinShare id first, then by name
export const findMapping = (player, mappings) => {
    if (!player || !mappings?.length) return null;
    const spinshareId = player.user?.spinshareId;
    if (spinshareId != null) {
        const byId = mappings.find((mapping) => mapping.spinshareId != null && String(mapping.spinshareId) === String(spinshareId));
        if (byId) return byId;
    }
    const names = [...nameVariants(player.name), ...nameVariants(player.user?.username)];
    return mappings.find((mapping) => [...nameVariants(mapping.name), ...nameVariants(mapping.spinshareProfile?.username)].some((name) => names.includes(name))) ?? null;
};

// A spulti match player who isn't in the player mapping (someone outside the event, e.g. testing), shaped like a
// mapping entry from spulti's own profile data so the screens and the matching work the same
export const spultiGuest = (player) => ({
    id: `spulti-${player.user?.spinshareId ?? normalizeName(player.name)}`,
    name: player.name,
    spinshareId: player.user?.spinshareId ?? null,
    spinshareProfile: player.user ? { username: player.user.username, avatar: player.user.avatarUrl, pronouns: player.user.pronouns } : null,
    spultiGuest: true,
});

// The player mapping plus guests: spulti players without a mapping entry, and players on screen (extra) that aren't
// in it anymore, so the selects can still show them
export const withGuests = (mappings, match, extra = []) => {
    const all = [...(mappings ?? [])];
    const add = (player) => {
        if (player && !all.some((x) => x.id + '' === player.id + '')) all.push(player);
    };
    for (const side of ['player1', 'player2']) {
        const player = match?.[side];
        if (player && !findMapping(player, mappings)) add(spultiGuest(player));
    }
    extra.filter((player) => player?.spultiGuest).forEach(add);
    return all;
};

export const playerOptions = (players) =>
    players.map((player) => ({
        icon: player.spultiGuest ? 'account-arrow-down' : 'account-box',
        label: `${player.spinshareProfile?.username ?? player.name}${player.spultiGuest ? ' (spulti)' : ''}`,
        value: player.id + '',
    }));
