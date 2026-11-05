import { inject, onMounted, onUnmounted, ref } from 'vue';

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

// What happened to every slot: { banned: side } (strike), { picked: side } (loser's pick), { played: set index, winner }
export const slotStates = (match, mappool) => {
    const states = {};
    const actions = match?.draft?.actions ?? [];
    const roll = actions.find((a) => a.type === 'roll');
    let bans = 0;
    for (const action of actions) {
        if (action.type === 'ban') {
            states[action.slotId] = { banned: roll ? (bans % 2 === 0 ? roll.firstBan : otherSide(roll.firstBan)) : null };
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
