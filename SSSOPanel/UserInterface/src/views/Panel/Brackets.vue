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
                :disabled="phase.length === 0 || phaseGroup.length === 0"
                v-tooltip="'Transition to screen'"
            />
        </template>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import SpinButton from '@/components/Common/SpinButton.vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import { onMounted, ref, inject } from 'vue';
const emitter = inject('emitter');
import SpinSelect from '@/components/Common/SpinSelect.vue';
import useTournamentAPI from '@/modules/useStartGGApi';
import SpinLoader from '@/components/Common/SpinLoader.vue';

const loading = ref(false);
const phases = ref([]);
const phaseGroups = ref([]);
const phase = ref('');
const phaseGroup = ref('');

const screenData = ref('');

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
                query: {
                    data: screenData.value,
                },
            },
        }),
    );
};

onMounted(() => {
    loading.value = true;

    window.external.sendMessage(
        JSON.stringify({
            command: 'settings-get-full',
            data: '',
        }),
    );
});
emitter.on('settings-get-full-response', async (settings) => {
    startGGApiToken.value = settings['currentEvent.startgg.apiToken'] ?? '';
    startGGEventId.value = settings['currentEvent.startgg.eventId'] ?? '-1';

    if (startGGApiToken.value !== '' && startGGEventId.value !== '-1') {
        await loadStartGGPhases();
    }

    loading.value = false;
});

const loadStartGGPhases = async () => {
    const { loadEventPhases } = useTournamentAPI(startGGApiToken.value);
    startGGPhases.value = await loadEventPhases(startGGEventId.value);

    phases.value = startGGPhases.value.map((phase) => ({
        icon: 'tournament',
        label: phase.name,
        value: phase.id + '',
    }));
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

    const matches = [];
    phaseGroupData.sets.nodes.forEach((set) => {
        let match = [];

        set.slots.forEach((slot) => {
            match.push([slot.entrant?.id, slot.standing?.placement, slot.standing?.stats?.score?.value]);
        });

        matches.push(match);
    });

    // Matches: 0 = A, 1 = B, 2 = C, 3 = D, 4 = F, 5 = G
    // Match: Player A, Player B
    // Player: [StartGG ID, Placement, Score]
    screenData.value = {
        phase: phaseData.name,
        phaseGroup: phaseGroupData.displayIdentifier,
        matches: matches,
    };

    console.log(screenData.value);
};
</script>

<style lang="scss" scoped></style>
