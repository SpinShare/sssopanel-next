import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

// https://vitejs.dev/config/
export default defineConfig({
    base: './',
    // Same floor as before vite 7, screens also run in OBS browser sources with an older chromium
    build: {
        target: 'chrome87',
    },
    // Off the default 5173 so other projects' dev servers don't end up in the panel, Program.cs loads this port
    server: {
        port: 5274,
        strictPort: true,
    },
    plugins: [vue()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
});
