<template>
    <div class="whep-player">
        <video
            ref="videoRef"
            autoplay
            playsinline
            @loadeddata="onVideoReady"
            @playing="onVideoReady"
            :style="{ width: '100%', height: '100%', objectFit: 'cover' }"
        ></video>
        <div
            class="loading-overlay"
            :class="{ visible: showOverlay }"
        >
            <SpinLoader />
        </div>
    </div>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue';
import { WebRTCPlayer } from '@eyevinn/webrtc-player';
import SpinLoader from '@/components/Common/SpinLoader.vue';

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

const MIN_LOADER_MS = 600;

const videoRef = ref(null);
const showOverlay = ref(false);

let player = null;
let loadedUrl = '';
let loadStartTime = 0;
let hideTimer = null;

const clearHideTimer = () => {
    if (hideTimer) {
        clearTimeout(hideTimer);
        hideTimer = null;
    }
};

const hideLoader = () => {
    clearHideTimer();
    const elapsed = Date.now() - loadStartTime;
    const remaining = MIN_LOADER_MS - elapsed;
    if (remaining > 0) {
        hideTimer = setTimeout(() => {
            showOverlay.value = false;
        }, remaining);
    } else {
        showOverlay.value = false;
    }
};

const onVideoReady = () => {
    hideLoader();
};

const load = async () => {
    if (!player || !props.url || !props.active) return;
    if (loadedUrl === props.url) return;
    loadedUrl = props.url;
    clearHideTimer();
    loadStartTime = Date.now();
    showOverlay.value = true;
    try {
        await player.load(new URL(props.url));
    } catch (err) {
        console.error('[WhepPlayer] load failed', props.url, err);
        hideLoader();
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
        hideLoader();
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
    clearHideTimer();
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

<style lang="scss" scoped>
.whep-player {
    position: relative;
    width: 100%;
    height: 100%;
}

.loading-overlay {
    position: absolute;
    inset: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(0, 0, 0, 0.5);
    pointer-events: none;
    opacity: 0;
    transition: opacity 0.3s ease;

    &.visible {
        opacity: 1;
    }
}
</style>
