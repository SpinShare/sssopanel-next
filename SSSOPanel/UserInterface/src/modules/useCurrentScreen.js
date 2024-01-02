import { inject, onMounted, onUnmounted, ref } from 'vue';

function useCurrentScreen() {
    const emitter = inject('emitter');
    const currentScreen = ref('');

    onMounted(() => {
        emitter.on('screen-navigate-response', (data) => {
            currentScreen.value = data.Path;
        });

        emitter.on('current-route-get-response', (data) => {
            currentScreen.value = data.Path;
        });

        window.external.sendMessage(
            JSON.stringify({
                command: 'current-route-get',
            }),
        );
    });

    onUnmounted(() => {
        emitter.off('screen-navigate-response');
        emitter.off('current-route-get-response');
    });

    return currentScreen;
}

export default useCurrentScreen;
