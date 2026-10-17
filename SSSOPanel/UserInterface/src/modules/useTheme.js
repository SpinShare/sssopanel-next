import { inject, onMounted, onUnmounted, ref } from 'vue';

// Shared, the screens with an event logo read the year from here
export const currentTheme = ref({ theme: 'spring', year: '' });

// Used once in App.vue, applies the overlay theme and follows changes saved on the settings page
export default function useTheme() {
    const emitter = inject('emitter');

    const applyTheme = (data) => {
        currentTheme.value = { theme: data?.theme || 'spring', year: data?.year ?? '' };
        document.documentElement.dataset.theme = currentTheme.value.theme;
    };

    onMounted(() => {
        emitter.on('theme-get-response', applyTheme);

        window.electronAPI.send(
            'message',
            JSON.stringify({
                command: 'theme-get',
            }),
        );
    });

    onUnmounted(() => {
        emitter.off('theme-get-response', applyTheme);
    });
}
