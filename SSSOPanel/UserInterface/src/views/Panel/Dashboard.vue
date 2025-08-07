<template>
    <AppLayout title="Dashboard">
        <div class="buttons">
            <SpinButton
                v-for="button in buttons"
                :key="button.panelPath"
                :label="button.label"
                :icon="button.icon"
                :color="currentScreen === button.screenPath ? 'primary' : 'default'"
                @click="router.push({ path: button.panelPath })"
            />
        </div>
    </AppLayout>
</template>

<script setup>
import AppLayout from '../../layouts/AppLayout.vue';
import router from '@/router';
import SpinButton from '@/components/Common/SpinButton.vue';
import useCurrentScreen from '@/modules/useCurrentScreen';

const currentScreen = useCurrentScreen();

const buttons = [
    {
        label: 'Init',
        icon: 'power',
        panelPath: '/panel/init',
        screenPath: '/screen/',
    },
    {
        label: 'Countdown',
        icon: 'clock-fast',
        panelPath: '/panel/countdown',
        screenPath: '/screen/countdown',
    },
    {
        label: 'Brackets',
        icon: 'tournament',
        panelPath: '/panel/brackets',
        screenPath: '/screen/brackets',
    },
    {
        label: 'Match (Overview)',
        icon: 'controller',
        panelPath: '/panel/match-overview',
        screenPath: '/screen/match-overview',
    },
    {
        label: 'Match (Ingame)',
        icon: 'controller',
        panelPath: '/panel/match-ingame',
        screenPath: '/screen/match-ingame',
    },
    {
        label: 'Commentators',
        icon: 'microphone-message',
        panelPath: '/panel/commentators',
        screenPath: '/screen/commentators',
    },
    {
        label: 'End (Stream)',
        icon: 'hand-wave',
        panelPath: '/panel/end-stream',
        screenPath: '/screen/end-stream',
    },
    
    // TODO: DESIGN END OF TOURNAMENT PAGE
    {
        label: 'End (Tournament)',
        icon: 'flag-checkered',
        panelPath: '/panel/end-tournament',
        screenPath: '/screen/end-tournament',
    }, 
];
</script>

<style lang="scss" scoped>
.buttons {
    display: grid;
    gap: 10px;

    & button {
        justify-content: flex-start;
        height: 50px;
    }
}
</style>
