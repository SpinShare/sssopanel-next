import { inject, onMounted, onUnmounted, ref } from 'vue';

// Live Spin Rhythm XD data, pushed by the backend from the game's own local websocket.
// onMessage only sees live updates, not the snapshot loaded on mount.
export default function useSrxd(onMessage = null) {
    const emitter = inject('emitter');

    const game = ref({
        enabled: true,
        connected: false,
        error: '',
        room: null,
        track: null,
        phase: 'none',
        round: 0,
        scores: null,
        results: null,
    });

    const onSnapshot = (snapshot) => {
        game.value = { ...game.value, ...snapshot };
    };

    const onUpdate = (message) => {
        const next = {
            ...game.value,
            enabled: message.enabled ?? game.value.enabled,
            connected: message.connected ?? game.value.connected,
            error: message.error ?? game.value.error,
        };

        switch (message.type) {
            case 'state':
                Object.assign(next, {
                    room: message.room,
                    track: message.track,
                    phase: message.phase,
                    round: message.round,
                    scores: message.scores,
                    results: message.results,
                });
                break;
            case 'room':
            case 'track':
            case 'scores':
            case 'results':
                next[message.type] = message[message.type];
                break;
            case 'phase':
                next.phase = message.phase;
                next.round = message.round;
                break;
            case 'status':
                if (!next.connected) {
                    Object.assign(next, { room: null, track: null, phase: 'none', round: 0, scores: null, results: null });
                }
                break;
        }

        game.value = next;
        onMessage?.(message);
    };

    onMounted(() => {
        emitter.on('srxd-get-response', onSnapshot);
        emitter.on('srxd-update', onUpdate);

        window.electronAPI.send(
            'message',
            JSON.stringify({
                command: 'srxd-get',
            }),
        );
    });

    onUnmounted(() => {
        emitter.off('srxd-get-response', onSnapshot);
        emitter.off('srxd-update', onUpdate);
    });

    return game;
}

// The game uses TextMeshPro tags in titles, e.g. <alpha=#C0><size=75%>TE</size>
export const stripRichText = (text) => (text ?? '').replace(/<[^>]*>/g, '').trim();

const normalizeName = (name) => (name ?? '').toLowerCase().replace(/[^a-z0-9]/g, '');

// Guesses which room player belongs to a player mapping by name
export const guessIngameName = (playerMapping, members) => {
    if (!playerMapping || !members?.length) return null;

    const names = [playerMapping.spinshareProfile?.username, playerMapping.name].map(normalizeName).filter((x) => x !== '');
    return members.find((member) => names.includes(normalizeName(member.name)))?.name ?? null;
};

// Chart object for charts that are not on SpinShare (base game), same fields the screens use
export const chartFromTrack = (track) => ({
    id: null,
    title: stripRichText(track.title),
    artist: stripRichText(track.artist),
    charter: stripRichText(track.charter),
    fileReference: track.fileReference,
    paths: { cover: null },
});

// The linked players' entries from a results message, the shape the screens keep in currentMatch.results
export const pickResults = (results, ingame) => ({
    round: results?.round ?? 0,
    player1: results?.entries?.find((entry) => entry.name === ingame?.player1) ?? null,
    player2: results?.entries?.find((entry) => entry.name === ingame?.player2) ?? null,
});

// 'player1' or 'player2' once someone has the points to win the match (best of sets.max), null before that.
// Numbers because the state may hold strings from older panel versions
export const matchWinner = (scores) => {
    const maxPoints = Math.ceil(Number(scores?.sets?.max ?? 3) / 2);
    if (Number(scores?.score?.player1 ?? 0) >= maxPoints) return 'player1';
    if (Number(scores?.score?.player2 ?? 0) >= maxPoints) return 'player2';
    return null;
};

// Perfect+ hits, the early key is included in case the game ever reports early ones
const perfectPlusCount = (entry) => (entry.judgements?.perfectPlus ?? 0) + (entry.judgements?.earlyPerfectPlus ?? 0);

// Score decides, an exact score tie goes to whoever hit more Perfect+ (the more accurate run).
// Someone missing from the results left. The game's placement and then player 1 only settle a complete tie.
export const player1Won = (entry1, entry2) => {
    if (!entry1) return false;
    if (!entry2) return true;
    if (entry1.score !== entry2.score) return entry1.score > entry2.score;
    if (perfectPlusCount(entry1) !== perfectPlusCount(entry2)) return perfectPlusCount(entry1) > perfectPlusCount(entry2);
    if (entry1.placement !== entry2.placement) return entry1.placement < entry2.placement;
    return true;
};
