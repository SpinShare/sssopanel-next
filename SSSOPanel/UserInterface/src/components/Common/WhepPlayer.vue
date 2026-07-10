<template>
    <video
        ref="videoRef"
        autoplay
        playsinline
        :style="{ width: '100%', height: '100%', objectFit: 'cover' }"
    ></video>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue';
import { WebRTCPlayer } from '@eyevinn/webrtc-player';

const props = defineProps({
    url: {
        type: String,
        default: '',
    },
    muted: {
        type: Boolean,
        default: false,
    },
    active: {
        type: Boolean,
        default: true,
    },
});

const videoRef = ref(null);
let player = null;
let loadedUrl = '';

const load = async () => {
    if (!player || !props.url || !props.active) return;
    if (loadedUrl === props.url) return;
    loadedUrl = props.url;
    try {
        await player.load(new URL(props.url));
    } catch (err) {
        console.error('[WhepPlayer] load failed', props.url, err);
    }
};

const ensurePlayer = () => {
    if (player || !videoRef.value) return;
    player = new WebRTCPlayer({
        video: videoRef.value,
        type: 'whep',
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }],
    });
    player.on('no-media', () => {
        console.warn('[WhepPlayer] no-media', props.url);
    });
    player.on('media-recovered', () => {
        console.log('[WhepPlayer] media-recovered', props.url);
    });
};

const applyActive = () => {
    if (props.active) {
        load();
        videoRef.value?.play().catch(() => {
            // ignored
        });
    } else {
        videoRef.value?.pause();
    }
};

const applyMuted = () => {
    if (videoRef.value) videoRef.value.muted = props.muted;
};

watch(
    () => props.url,
    () => {
        loadedUrl = '';
        applyActive();
    },
);
watch(() => props.active, applyActive);
watch(() => props.muted, applyMuted);

onMounted(() => {
    ensurePlayer();
    applyMuted();
    applyActive();
});

onUnmounted(() => {
    if (player) {
        try {
            player.destroy();
        } catch (err) {
            console.error('[WhepPlayer] destroy failed', err);
        }
        player = null;
    }
    if (videoRef.value) videoRef.value.srcObject = null;
});
</script>
