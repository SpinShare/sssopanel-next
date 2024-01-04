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

const saveMapping = () => {
    console.log('Saving Mapping');
    state.value = STATE_SAVING;

    const settings = [{ key: 'currentEvent.playerMapping', value: playerMappings.value }];

    window.external.sendMessage(
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

        if (startGGApiToken.value !== '' && startGGEventId.value !== '-1') {
            await loadStartGGEventEntrants();
        }

        state.value = STATE_IDLE;
    });
    emitter.on('settings-set-response', () => {
        state.value = STATE_IDLE;
    });

    window.external.sendMessage(
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
