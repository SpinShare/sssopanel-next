<template>
    <div class="screen-footer">
        <div class="promos">
            <transition
                name="promo"
                mode="out-in"
            >
                <div :key="activePromoIndex">
                    <span>{{ promos[activePromoIndex].title }}</span>
                    <span>{{ promos[activePromoIndex].subtitle }}</span>
                </div>
            </transition>
        </div>
        <transition
            name="promo"
            mode="out-in"
        >
            <div
                class="music"
                v-if="musicActive && !isGameplayScreen"
                :key="`${musicActive}${musicTitle}${musicArtist}`"
            >
                <img
                    v-if="musicCover && !musicCoverFailed"
                    :src="musicCover"
                    alt=""
                    @error="musicCoverFailed = true"
                />
                <span
                    v-else
                    class="mdi mdi-music"
                ></span>
                <span class="title">{{ musicTitle }}</span>
                <template v-if="musicArtist">
                    <span>-</span>
                    <span class="artist">{{ musicArtist }}</span>
                </template>
            </div>
        </transition>
    </div>
</template>

<script setup>
import { computed, inject, onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { gameplayScreens } from '@/router';
const emitter = inject('emitter');
const route = useRoute();

// The game is playing its own music there, so the now playing would just be wrong
const isGameplayScreen = computed(() => gameplayScreens.includes(route.path));

const promos = ref([
    {
        title: 'spinsha.re',
        subtitle: 'Find, download and share your Spin Rhythm XD custom charts!',
    },
    {
        title: 'patreon.com/SpinShare',
        subtitle: 'Support our team for a fancy profile badge!',
    },
    {
        title: 'bsky.app/profile/spinsha.re',
        subtitle: 'Follow us for updates, statistics and announcements!',
    },
    {
        title: 'spinsha.re/client-next',
        subtitle: 'Download the new desktop client and receive a profile card!',
    },
    {
        title: 'discord.io/SpinShare',
        subtitle: 'Connect with the Spin Rhythm XD customs community!',
    },
]);
const activePromoIndex = ref(0);
let promoUpdateInterval = null;

const musicActive = ref(false);
const musicArtist = ref('');
const musicTitle = ref('');
const musicCover = ref('');
const musicCoverFailed = ref(false);

let musicUpdateInterval = null;
const updateMusic = () => {
    if (isGameplayScreen.value) return;

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'screen-music-get',
        }),
    );
};

onMounted(() => {
    emitter.on('screen-music-get-response', (data) => {
        // Tuna serves every cover from the same URL, so tie it to the song to refresh it
        const cover = data.cover ? `${data.cover}?song=${encodeURIComponent(data.artist + data.title)}` : '';
        if (cover !== musicCover.value) musicCoverFailed.value = false;

        musicActive.value = data.active;
        musicArtist.value = data.artist;
        musicTitle.value = data.title;
        musicCover.value = cover;
    });

    promoUpdateInterval = setInterval(() => {
        activePromoIndex.value = (activePromoIndex.value + 1) % promos.value.length;
    }, 12000);
    musicUpdateInterval = setInterval(() => {
        updateMusic();
    }, 1000);

    updateMusic();
});

onUnmounted(() => {
    emitter.off('screen-music-get-response');

    clearInterval(promoUpdateInterval);
    clearInterval(musicUpdateInterval);
});
</script>

<style lang="scss" scoped>
.screen-footer {
    background: #000;
    height: 3vw;
    display: grid;
    grid-template-columns: 1fr auto;
    align-items: center;
    padding: 0 2vw;
    position: relative;
    z-index: 100;

    & .promos {
        font-size: 1vw;
        position: relative;

        & > div {
            position: absolute;
            top: -0.5vw;
            display: flex;
            gap: 1vw;
            align-items: center;
            letter-spacing: 0.125rem;

            & span:nth-child(1) {
                font-weight: bold;
                letter-spacing: 0.025rem;
                font-size: 1.25vw;
            }
            & span:nth-child(2) {
                color: rgba(255, 255, 255, 0.8);
            }
        }
    }
    & .music {
        display: flex;
        align-items: center;
        gap: 0.75vw;
        font-size: 1.25vw;
        max-width: 35vw;

        & .mdi {
            font-size: 2vw;
        }
        & img {
            flex-shrink: 0;
            height: 2vw;
            width: 2vw;
            object-fit: cover;
            border-radius: 0.25vw;
        }
        & span {
            text-overflow: ellipsis;
            white-space: nowrap;
            overflow: hidden;
            flex-shrink: 0;

            // The artists give up their space first, the title is only cut off once they are gone
            &.title {
                color: rgba(255, 255, 255, 0.8);
                flex-shrink: 1;
            }
            &.artist {
                font-weight: bold;
                flex-shrink: 9999;
            }
        }
    }
}
</style>
