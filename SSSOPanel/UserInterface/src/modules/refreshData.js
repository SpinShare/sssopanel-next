import useSpinShareApi from '@/modules/useSpinShareApi';
import useTournamentAPI from '@/modules/useStartGGApi';
import { findMapping } from '@/modules/useSpulti';

// The api sends a lot more than the screens use, the player mapping is saved in settings.json
export const cleanProfile = (user) => {
    if (!user) return null;
    const profile = { ...user };
    ['isVerified', 'isPatreon', 'songs', 'playlists', 'reviews', 'spinplays', 'cards'].forEach((key) => delete profile[key]);
    return profile;
};

const send = (command, data) => window.electronAPI.send('message', JSON.stringify({ command, data }));

// Sends a command and waits for its response (wildcard, pages off() their own listeners)
const request = (emitter, command, data) =>
    new Promise((resolve, reject) => {
        const timer = setTimeout(() => {
            emitter.off('*', handler);
            reject(new Error(`No answer to ${command}`));
        }, 10000);
        const handler = (type, response) => {
            if (type !== `${command}-response`) return;
            clearTimeout(timer);
            emitter.off('*', handler);
            resolve(response);
        };
        emitter.on('*', handler);
        send(command, data);
    });

// A few at a time, spinsha.re doesn't need 100 requests at once
const inBatches = async (items, size, fn) => {
    for (let i = 0; i < items.length; i += size) await Promise.all(items.slice(i, i + size).map(fn));
};

// Fresh SpinShare profiles for every entry with an id. A failed fetch keeps the old profile. Returns how many updated
export const refreshProfiles = async (mappings) => {
    const { loadUser } = useSpinShareApi();
    let updated = 0;
    await inBatches(
        mappings.filter((mapping) => mapping.spinshareId),
        5,
        async (mapping) => {
            const user = cleanProfile(await loadUser(mapping.spinshareId));
            if (!user) return;
            mapping.spinshareProfile = user;
            updated++;
        },
    );
    return updated;
};

// Fresh names from start.gg by entrant id, everything else stays
export const refreshEntrantNames = async (mappings, apiToken, eventId) => {
    if (!apiToken || !eventId || eventId === '-1') return 0;
    const { loadEventEntrants } = useTournamentAPI(apiToken);
    const entrants = await loadEventEntrants(eventId);
    const byId = new Map((entrants ?? []).map((entrant) => [entrant.id + '', entrant]));
    let updated = 0;
    for (const mapping of mappings) {
        const entrant = byId.get(mapping.id + '');
        if (entrant && entrant.name !== mapping.name) {
            mapping.name = entrant.name;
            updated++;
        }
    }
    return updated;
};

// Everything the screens show from outside: start.gg names, SpinShare profiles (players without a SpinShare id get
// the one from their spulti account), the players on screen and the chart. Panel only. Returns a short summary
export const refreshAll = async (emitter, spultiMatch = null) => {
    const settings = await request(emitter, 'settings-get-full');
    const state = await request(emitter, 'state-get');
    const mappings = [...(settings['currentEvent.playerMapping'] ?? [])].map((mapping) => ({ ...mapping }));
    const notes = [];

    try {
        const renamed = await refreshEntrantNames(mappings, settings['currentEvent.startgg.apiToken'], settings['currentEvent.startgg.eventId']);
        if (renamed) notes.push(`${renamed} start.gg name${renamed === 1 ? '' : 's'} changed`);
    } catch {
        notes.push('start.gg failed');
    }

    // Mapped players without a SpinShare id, spulti knows it when they have an account there
    for (const side of ['player1', 'player2']) {
        const spultiPlayer = spultiMatch?.[side];
        const spinshareId = spultiPlayer?.user?.spinshareId;
        if (!spinshareId) continue;
        const mapping = findMapping(spultiPlayer, mappings);
        if (mapping && !mapping.spinshareId) mapping.spinshareId = spinshareId;
    }

    const profiles = await refreshProfiles(mappings);
    notes.push(`${profiles} SpinShare profile${profiles === 1 ? '' : 's'}`);
    send('settings-set', [{ key: 'currentEvent.playerMapping', value: mappings }]);

    // Players on screen: their fresh mapping entry, guests (not in the mapping) straight from SpinShare
    const { loadUser, loadChart } = useSpinShareApi();
    const current = state?.currentMatch ?? {};
    const players = {};
    for (const side of ['player1', 'player2']) {
        const player = current.players?.[side];
        if (!player) continue;
        const mapping = mappings.find((x) => x.id + '' === player.id + '');
        if (mapping) {
            players[side] = mapping;
        } else if (player.spinshareId) {
            const user = cleanProfile(await loadUser(player.spinshareId));
            if (user) players[side] = { ...player, spinshareProfile: user };
        }
    }
    if (Object.keys(players).length) {
        // Cleared first, state-set deep merges and would keep fields the fresh one doesn't have
        send('state-set', { currentMatch: { players: Object.fromEntries(Object.keys(players).map((side) => [side, null])) } });
        send('state-set', { currentMatch: { players } });
    }

    if (current.chart?.id) {
        const chart = await loadChart(current.chart.id);
        if (chart) {
            send('state-set', { currentMatch: { chart } });
            notes.push('chart');
        } else {
            notes.push('chart failed');
        }
    }

    // Pages only listen for the get responses
    send('settings-get-full');
    send('state-get');
    return `Refreshed ${notes.join(', ')}`;
};
