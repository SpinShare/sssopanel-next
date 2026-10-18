import { inject, onMounted, onUnmounted, ref } from 'vue';
import { getTheme } from '@/modules/themes';

// Shared, the screens read the logo, background and name from here
export const currentTheme = ref(getTheme());

// Used once in App.vue, applies the overlay theme and follows changes saved on the settings page
export default function useTheme() {
    const emitter = inject('emitter');

    const applyTheme = (data) => {
        currentTheme.value = getTheme(data?.theme);
        const root = document.documentElement;
        root.dataset.theme = currentTheme.value.id;
        // Absolute, a relative url() in a custom property can resolve against the stylesheet using it
        root.style.setProperty('--themeBackground', `url("${new URL(currentTheme.value.background, location.href).href}")`);
    };

    // Right away, so the screens don't flash without a background until theme-get answers
    applyTheme();

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
