import { inject, onMounted, onUnmounted, ref } from 'vue';
import VoiceBotService from './VoiceBotService';

// Follows the backend's voice bot status (local process or remote url) and keeps
// VoiceBotService connected to whichever bot is in use.
export default function useVoiceBot() {
    const emitter = inject('emitter');

    const status = ref({ status: 'stopped', mode: 'local', url: '', error: '', logs: [] });

    const onStatus = (next) => {
        status.value = next;
        VoiceBotService.setKey(next.key);

        // A local bot listens right after it starts, before discord login is done
        if (next.mode === 'remote' || next.status === 'starting' || next.status === 'running') {
            VoiceBotService.connect(next.url);
        } else {
            VoiceBotService.disconnect();
        }
    };

    onMounted(() => {
        emitter.on('voicebot-status-response', onStatus);
        window.electronAPI.send('message', JSON.stringify({ command: 'voicebot-status-get' }));
    });

    onUnmounted(() => {
        emitter.off('voicebot-status-response', onStatus);
    });

    return { status, bot: VoiceBotService.state };
}
