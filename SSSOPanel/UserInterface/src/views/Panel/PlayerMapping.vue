<template>
    <AppLayout title="Player Mapping">
        <template v-if="state !== STATE_LOADING">
            <div
                class="incomplete-mapping-banner"
                v-if="playerMappings.filter((x) => !x.spinshareProfile).length > 0"
            >
                Mapping Incomplete!
            </div>

            <SpinInput
                label="Reset"
                hint="Reload all mappings"
                type="horizontal"
            >
                <SpinButton
                    label="Reset"
                    color="danger"
                    @click="resetMapping"
                />
            </SpinInput>

            <SpinInput
                label="Import CSV"
                :hint="importHint"
                type="horizontal"
            >
                <div style="display: flex; gap: 10px">
                    <SpinButton
                        v-if="tournamentSlug"
                        icon="open-in-new"
                        @click="openAttendees"
                        v-tooltip="'Open the start.gg attendee list (export it as CSV there)'"
                    />
                    <SpinButton
                        label="Import"
                        icon="file-upload-outline"
                        :disabled="state !== STATE_IDLE"
                        @click="csvInput.click()"
                    />
                </div>
                <input
                    ref="csvInput"
                    type="file"
                    accept=".csv,text/csv"
                    style="display: none"
                    @change="importCsv"
                />
            </SpinInput>

            <SpinInput
                v-for="playerMapping in playerMappings"
                :key="playerMapping.id"
                :label="playerMapping.name"
                :hint="!!playerMapping.spinshareId ? (playerMapping.spinshareProfile ? `${playerMapping.spinshareProfile.pronouns ? `(${playerMapping.spinshareProfile.pronouns})` : ''} ${playerMapping.spinshareProfile.username}` : `No profile for ID: ${playerMapping.spinshareId}`) : 'No mapping'"
            >
                <input
                    type="number"
                    v-model="playerMapping.spinshareId"
                    @change="loadEntrantSpinShareProfile(playerMapping)"
                />
            </SpinInput>
        </template>
        <template v-if="state === STATE_LOADING">
            <SpinLoader />
        </template>

        <template #transition>
            <SpinButton
                color="bright"
                icon="content-save"
                @click="saveMapping"
                :label="`(${playerMappings.filter((x) => !x.spinshareProfile).length})`"
                :disabled="state !== STATE_IDLE"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import { onMounted, ref, inject, onUnmounted } from 'vue';
import useTournamentAPI from '@/modules/useStartGGApi';
import SpinLoader from '@/components/Common/SpinLoader.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import useSpinShareApi from '@/modules/useSpinShareApi';
const emitter = inject('emitter');

const STATE_LOADING = 'loading';
const STATE_SAVING = 'saving';
const STATE_IDLE = 'idle';
const state = ref(STATE_LOADING);

const startGGApiToken = ref('');
const startGGEventId = ref('-1');
const playerMappings = ref([]);
const tournamentSlug = ref('');
const csvInput = ref(null);
const importHint = ref('SpinShare IDs from a start.gg attendee list');

const loadStartGGEventEntrants = async () => {
    const { loadEventEntrants } = useTournamentAPI(startGGApiToken.value);
    const entrants = await loadEventEntrants(startGGEventId.value);

    const entrantsMap = new Map(entrants.map((e) => [e.id, e]));

    // Flag player mappings not present in event entrants
    playerMappings.value = playerMappings.value.filter((playerMapping) => {
        if (entrantsMap.has(playerMapping.id)) {
            entrantsMap.delete(playerMapping.id);
            return true;
        } else {
            return false;
        }
    });

    // Add new event entrants not present in player mappings
    for (const entrant of entrantsMap.values()) {
        playerMappings.value.push({ ...entrant });
    }
};

const loadEntrantSpinShareProfile = async (playerMapping) => {
    console.log('Loading User');

    if (!playerMapping.spinshareId) {
        delete playerMapping.spinshareProfile;
        return;
    }

    const { loadUser } = useSpinShareApi();
    const user = await loadUser(playerMapping.spinshareId);

    if (!user) {
        delete playerMapping.spinshareProfile;
        return;
    }

    delete user?.isVerified;
    delete user?.isPatreon;
    delete user?.songs;
    delete user?.playlists;
    delete user?.reviews;
    delete user?.spinplays;
    delete user?.cards;

    playerMapping.spinshareProfile = user;
};

// Quote aware, start.gg quotes most fields and names can contain commas
const parseCsv = (text) => {
    const rows = [];
    let row = [];
    let field = '';
    let quoted = false;

    for (let i = 0; i < text.length; i++) {
        const char = text[i];
        if (quoted) {
            if (char === '"' && text[i + 1] === '"') {
                field += '"';
                i++;
            } else if (char === '"') {
                quoted = false;
            } else {
                field += char;
            }
        } else if (char === '"') {
            quoted = true;
        } else if (char === ',') {
            row.push(field);
            field = '';
        } else if (char === '\n' || char === '\r') {
            if (char === '\r' && text[i + 1] === '\n') i++;
            row.push(field);
            rows.push(row);
            row = [];
            field = '';
        } else {
            field += char;
        }
    }
    if (field !== '' || row.length) {
        row.push(field);
        rows.push(row);
    }

    return rows.filter((x) => x.some((value) => value.trim() !== ''));
};

const normalizeName = (name) => (name ?? '').toLowerCase().replace(/[^a-z0-9]/g, '');

// Attendee ids are not entrant ids, so rows are matched to entrants by name ("Prefix | Tag" on start.gg)
const importCsv = async (event) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) return;

    const [header, ...rows] = parseCsv(await file.text());
    if (!header) {
        importHint.value = 'Empty file';
        return;
    }

    const column = (name) => header.findIndex((x) => x.trim().toLowerCase() === name);
    let spinshareColumn = column('spinshare user id');
    if (spinshareColumn === -1) spinshareColumn = header.length - 1;
    const tagColumn = column('gamertag');
    const shortColumn = column('short gamertag');
    const prefixColumn = column('prefix');

    const attendees = new Map();
    for (const row of rows) {
        const spinshareId = parseInt(row[spinshareColumn], 10);
        if (!spinshareId) continue;

        const tag = row[tagColumn] ?? '';
        const short = row[shortColumn] ?? '';
        const prefix = row[prefixColumn] ?? '';
        for (const name of [tag, short, prefix && short ? `${prefix} | ${short}` : '']) {
            if (normalizeName(name)) attendees.set(normalizeName(name), spinshareId);
        }
    }

    state.value = STATE_LOADING;
    const unmatched = [];
    let matched = 0;
    for (const playerMapping of playerMappings.value) {
        const spinshareId = attendees.get(normalizeName(playerMapping.name));
        if (!spinshareId) {
            unmatched.push(playerMapping.name);
            continue;
        }

        matched++;
        if (playerMapping.spinshareId + '' === spinshareId + '' && playerMapping.spinshareProfile) continue;
        playerMapping.spinshareId = spinshareId;
        // One at a time, the SpinShare api doesn't need 30 requests at once
        await loadEntrantSpinShareProfile(playerMapping);
    }
    state.value = STATE_IDLE;

    importHint.value = `Matched ${matched} of ${playerMappings.value.length}${unmatched.length ? `, no match for ${unmatched.join(', ')}` : ''}. Save to keep it`;
};

const openAttendees = () => {
    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'open-in-browser',
            data: `https://www.start.gg/admin/tournament/${tournamentSlug.value}/attendees`,
        }),
    );
};

const saveMapping = () => {
    console.log('Saving Mapping');
    state.value = STATE_SAVING;

    const settings = [{ key: 'currentEvent.playerMapping', value: playerMappings.value }];

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'settings-set',
            data: settings,
        }),
    );
};

const resetMapping = async () => {
    const { loadEventEntrants } = useTournamentAPI(startGGApiToken.value);
    const entrants = await loadEventEntrants(startGGEventId.value);

    const entrantsMap = new Map(entrants.map((e) => [e.id, e]));
    playerMappings.value = [];

    for (const entrant of entrantsMap.values()) {
        playerMappings.value.push({ ...entrant, inEvent: true });
    }
};

onMounted(() => {
    emitter.on('settings-get-full-response', async (settings) => {
        playerMappings.value = settings['currentEvent.playerMapping'] ?? [];
        startGGApiToken.value = settings['currentEvent.startgg.apiToken'] ?? '';
        startGGEventId.value = settings['currentEvent.startgg.eventId'] ?? '-1';
        tournamentSlug.value = settings['currentEvent.startgg.tournamentSlug'] ?? '';

        if (startGGApiToken.value !== '' && startGGEventId.value !== '-1') {
            await loadStartGGEventEntrants();
        }

        state.value = STATE_IDLE;
    });
    emitter.on('settings-set-response', () => {
        state.value = STATE_IDLE;
    });

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'settings-get-full',
            data: '',
        }),
    );
});

onUnmounted(() => {
    emitter.off('settings-get-full-response');
    emitter.off('settings-set-response');
});
</script>

<style lang="scss" scoped>
.incomplete-mapping-banner {
    background: #111;
    background-image: linear-gradient(rgba(var(--colorError), 1), rgba(var(--colorError), 1)), linear-gradient(white, white);
    background-blend-mode: overlay;
    color: rgb(var(--colorError));
    position: absolute;
    padding: 15px;
    text-align: center;
    top: 50px;
    left: 0;
    right: 0;
}
</style>

<style lang="scss" scoped v-if="playerMappings.filter((x) => !x.spinshareProfile).length > 0">
.setup-input:first-of-type {
    margin-top: 50px;
}
</style>
