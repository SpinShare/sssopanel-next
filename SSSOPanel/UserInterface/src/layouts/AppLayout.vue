<template>
    <section class="layout-app">
        <header>
            <SpinButton
                v-if="router.currentRoute.value.fullPath !== '/panel'"
                icon="arrow-left"
                color="transparent"
                @click="router.push({ path: '/panel' })"
            />
            <div class="title">
                {{ props.title }}
            </div>

            <nav>
                <SpinButton
                    icon="window-restore"
                    color="transparent"
                    v-tooltip="'Open Screen (Windowed)'"
                    @click="openScreen"
                />
                <SpinButton
                    icon="fullscreen"
                    color="transparent"
                    v-tooltip="'Open Screen (Fullscreen)'"
                    @click="openScreenFullscreen"
                />
                <SpinButton
                    icon="cog"
                    color="transparent"
                    v-tooltip="'Settings'"
                    @click="router.push({ path: '/panel/settings' })"
                />
            </nav>
        </header>
        <main>
            <slot />
        </main>
        <footer>
            <div class="live-screen">
                <span>Active Live Screen</span>
                <span>{{ currentScreen }}</span>
            </div>
            <slot name="transition" />
        </footer>

        <UpdateBanner />
        <AlertMessage />
    </section>
</template>

<script setup>
import UpdateBanner from '@/components/UpdateBanner.vue';
import AlertMessage from '@/components/Common/AlertMessage.vue';
import { inject, onMounted, onUnmounted, ref } from 'vue';
import router from '@/router';
import useCurrentScreen from '@/modules/useCurrentScreen';

const props = defineProps({
    title: {
        type: String,
        default: 'SpinShare',
    },
});

const currentScreen = useCurrentScreen();

const openScreen = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'open-screen',
            data: {
                fullscreen: false,
            },
        }),
    );
};
const openScreenFullscreen = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'open-screen',
            data: {
                fullscreen: true,
            },
        }),
    );
};
</script>

<style lang="scss" scoped>
.layout-app {
    width: 100%;
    height: 100%;
    display: grid;
    overflow: hidden;
    grid-template-rows: 50px 1fr;

    & > header {
        display: flex;
        gap: 15px;
        align-items: center;
        padding: 0 20px;
        border-bottom: 1px solid rgba(255, 255, 255, 0.1);

        & .title {
            flex-grow: 1;
        }

        & nav {
            display: flex;
            gap: 5px;
        }
    }

    & > main {
        overflow-y: auto;
        display: flex;
        flex-direction: column;
        gap: 20px;
        padding: 20px;
    }
    & > footer {
        display: flex;
        gap: 15px;
        align-items: center;
        padding: 10px 20px;
        border-top: 1px solid rgba(255, 255, 255, 0.1);

        & .live-screen {
            flex-grow: 1;
            display: flex;
            flex-direction: column;
            gap: 4px;
            justify-content: center;

            & span:nth-child(1) {
                font-weight: bold;
                font-size: 0.8rem;
            }
            & span:nth-child(2) {
                color: rgba(255, 255, 255, 0.6);
                line-height: 0.8rem;
            }
        }
    }
}
</style>
