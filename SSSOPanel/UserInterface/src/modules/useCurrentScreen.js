import { inject, onMounted, ref } from 'vue';

function useCurrentScreen() {
    const emitter = inject('emitter');
    const currentScreen = ref('');

    onMounted(() => {
        window.external.sendMessage(
            JSON.stringify({
                command: 'current-route-get',
            }),
        );
    });

    emitter.on('screen-navigate-response', (data) => {
        currentScreen.value = data.Path;
    });

    emitter.on('current-route-get-response', (data) => {
        currentScreen.value = data.Path;
    });

    return currentScreen;
}

export default useCurrentScreen;
