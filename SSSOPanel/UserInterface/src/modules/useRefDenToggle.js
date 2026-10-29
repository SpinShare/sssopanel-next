import { inject, onMounted, onUnmounted, ref } from 'vue';

// The ref den button on the panel pages. Pages used to keep their own copy, some never loaded the state,
// so the first click only "turned off" what was already on (two clicks to toggle)
export default function useRefDenToggle() {
    const emitter = inject('emitter');
    const refDenIsVisible = ref(false);
    // While our own set is on its way, an older state-get-response would flip the button back
    let pendingSets = 0;

    // Wildcard, pages off() every state listener of their own when they unmount
    const onAnyMessage = (type, data) => {
        if (type === 'state-set-response') {
            pendingSets = Math.max(0, pendingSets - 1);
        } else if (type !== 'state-get-response' || pendingSets > 0) {
            return;
        }
        refDenIsVisible.value = data?.refDenIsVisible ?? false;
    };

    const toggleRefDen = () => {
        refDenIsVisible.value = !refDenIsVisible.value;
        pendingSets++;
        window.electronAPI.send('message', JSON.stringify({ command: 'state-set', data: { refDenIsVisible: refDenIsVisible.value } }));
    };

    onMounted(() => {
        emitter.on('*', onAnyMessage);
        window.electronAPI.send('message', JSON.stringify({ command: 'state-get' }));
    });

    onUnmounted(() => {
        emitter.off('*', onAnyMessage);
    });

    return { refDenIsVisible, toggleRefDen };
}
