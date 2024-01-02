<template>
    <AppLayout title="Brackets">
        <template v-if="!loading">
            <SpinInput
                v-if="!loading"
                label="Bracket"
                hint="Phase"
                type="horizontal"
            >
                <SpinSelect
                    v-model="phase"
                    :options="phases"
                    @update:model-value="loadPhaseGroupsSelect"
                />
            </SpinInput>
            <SpinInput
                v-if="!loading && phase !== ''"
                label="Pool"
                hint="Phase Group"
                type="horizontal"
            >
                <SpinSelect
                    v-model="phaseGroup"
                    :options="phaseGroups"
                    @update:model-value="updateSelectedPhaseGroup"
                />
            </SpinInput>
        </template>
        <template v-if="loading">
            <SpinLoader />
        </template>

        <template #transition>
            <SpinButton
                @click="transition"
                icon="record"
                color="primary"
                :disabled="screenData === null"
                v-tooltip="'Transition to screen'"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import { onMounted, ref, inject, onUnmounted } from 'vue';
const emitter = inject('emitter');
import SpinSelect from '@/components/Common/SpinSelect.vue';
import useTournamentAPI from '@/modules/useStartGGApi';
import SpinLoader from '@/components/Common/SpinLoader.vue';

const loading = ref(false);
const phases = ref([]);
const phaseGroups = ref([]);
const phase = ref('');
const phaseGroup = ref('');

const playerMappings = ref([]);

const screenData = ref(null);

const startGGApiToken = ref('');
const startGGEventId = ref('-1');
const startGGPhases = ref(null);

const transition = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-navigate',
            data: {
                path: 'brackets',
                params: {},
                query: {},
                richData: screenData.value,
            },
        }),
    );
};

const loadStartGGPhases = async () => {
    const { loadEventPhases } = useTournamentAPI(startGGApiToken.value);
    startGGPhases.value = await loadEventPhases(startGGEventId.value);

    phases.value = startGGPhases.value.map((phase) => {
        console.log(phase);

        return {
            icon: 'tournament',
            label: phase.name,
            value: phase.id + '',
        };
    });
};

const loadPhaseGroupsSelect = () => {
    phaseGroups.value = startGGPhases.value
        .filter((x) => x?.id + '' === phase.value)[0]
        .phaseGroups.nodes.map((phaseGroup) => ({
            icon: 'pool',
            label: `${phaseGroup.displayIdentifier} - ${new Date(phaseGroup.startAt * 1000).toLocaleDateString('en-US')}`,
            value: phaseGroup.id + '',
        }));
};
const updateSelectedPhaseGroup = async () => {
    const { loadPhaseGroup } = useTournamentAPI(startGGApiToken.value);
    const phaseData = startGGPhases.value.filter((x) => x?.id + '' === phase.value)[0];
    const phaseGroupData = await loadPhaseGroup(phaseGroup.value);

    // Matches: 0 = Match A, 1 = Match B, 2 = Match C, 3 = Match D, 4 = Match F, 5 = Match G
    const matches = [];
    phaseGroupData.sets.nodes.forEach((set) => {
        let match = { players: [] };

        set.slots.forEach((slot) => {
            const entrant = playerMappings.value.filter((x) => x?.id === slot.entrant?.id)[0];

            match.players.push({
                entrantId: slot.entrant?.id,
                entrant: entrant ?? null,
                placement: slot.standing?.placement ?? null,
                score: slot.standing?.stats?.score?.value ?? 0,
            });
        });

        matches.push(match);
    });

    screenData.value = {
        phase: phaseData.name,
        phaseGroup: phaseGroupData.displayIdentifier,
        matches: matches,
    };
};

onMounted(() => {
    loading.value = true;

    emitter.on('settings-get-full-response', async (settings) => {
        playerMappings.value = settings['currentEvent.playerMapping'] ?? [];
        startGGApiToken.value = settings['currentEvent.startgg.apiToken'] ?? '';
        startGGEventId.value = settings['currentEvent.startgg.eventId'] ?? '-1';

        if (startGGApiToken.value !== '' && startGGEventId.value !== '-1') {
            await loadStartGGPhases();
        }

        loading.value = false;
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
});
</script>

<style lang="scss" scoped></style>
