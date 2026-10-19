<template>
    <div
        class="screen-backdrop"
        v-if="$route.path.startsWith('/screen')"
    >
        <div class="top"></div>
        <div class="gameplay"></div>
        <div class="bottom"></div>
    </div>
    <router-view v-slot="{ Component, route }">
        <transition
            :name="route.path.startsWith('/screen') ? 'screen' : 'default'"
            mode="out-in"
        >
            <component
                :is="Component"
                :key="route.fullPath"
            />
        </transition>
    </router-view>
    <!-- Outside the pages, so a prompt survives page changes and isn't dropped mid-transition -->
    <AlertMessage v-if="$route.path.startsWith('/panel')" />
</template>

<script setup>
import { inject } from 'vue';
import useWinnerPrompt from '@/modules/useWinnerPrompt';
import useTheme from '@/modules/useTheme';
import useVoiceBot from '@/modules/useVoiceBot';
import AlertMessage from '@/components/Common/AlertMessage.vue';
const emitter = inject('emitter');
useWinnerPrompt();
useTheme();
// Keeps the panel connected to the voice bot on every page, results get posted through it
useVoiceBot();

window.electronAPI.on('message', (rawResponse) => {
    const response = JSON.parse(rawResponse);
    emitter.emit(response.Command, response.Data);
});
</script>

<style lang="scss">
#app {
    width: 100%;
    height: 100%;
    display: grid;
    overflow: hidden;

    & > main {
        overflow-y: scroll;
    }
}

// Solid backdrop the screens fade over. The gameplay band (1920x540 at 1080p) only opens up
// once a screen with a gameplay cutout is fully in place, and closes before it leaves.
.screen-backdrop {
    $gameplayHeight: 28.125vw;

    position: fixed;
    inset: 0;
    z-index: -1;

    & > div {
        position: absolute;
        left: 0;
        right: 0;
        background: rgb(var(--colorBase));
    }
    & .top {
        top: 0;
        bottom: calc(50% + #{$gameplayHeight * 0.5});
    }
    & .bottom {
        top: calc(50% + #{$gameplayHeight * 0.5});
        bottom: 0;
    }
    & .gameplay {
        // Overlaps by a pixel to avoid seams
        top: calc(50% - #{$gameplayHeight * 0.5} - 1px);
        height: calc(#{$gameplayHeight} + 2px);
        // Closed again before the screen visibly starts to leave
        transition: opacity 0.4s ease-out;
    }
}
// Waits for the previous screen to leave (1s) and the new one to enter (1s)
html.gameplay .screen-backdrop .gameplay {
    opacity: 0;
    transition: opacity 0.5s ease-out 2s;
}
</style>
