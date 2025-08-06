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
                v-if="spotifyActive"
                :key="`${spotifyActive}${spotifyTitle}${spotifyArtist}`"
            >
                <span class="mdi mdi-music"></span>
                <span>{{ spotifyTitle }}</span>
                <span>{{ spotifyArtist }}</span>
            </div>
        </transition>
    </div>
</template>

<script setup>
import { inject, onMounted, onUnmounted, ref } from 'vue';
const emitter = inject('emitter');

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

const spotifyActive = ref(false);
const spotifyArtist = ref('');
const spotifyTitle = ref('');

let spotifyUpdateInterval = null;
const updateSpotify = () => {
    window.external.sendMessage(
        JSON.stringify({
            command: 'screen-spotify-get',
        }),
    );
};

onMounted(() => {
    emitter.on('screen-spotify-get-response', (data) => {
        spotifyActive.value = data.active;
        spotifyArtist.value = data.artist;
        spotifyTitle.value = data.title;
    });

    promoUpdateInterval = setInterval(() => {
        activePromoIndex.value = (activePromoIndex.value + 1) % promos.value.length;
    }, 12000);
    spotifyUpdateInterval = setInterval(() => {
        updateSpotify();
    }, 1000);

    updateSpotify();
});

onUnmounted(() => {
    emitter.off('screen-spotify-get-response');

    clearInterval(promoUpdateInterval);
    clearInterval(spotifyUpdateInterval);
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
        gap: 1vw;
        font-size: 1.25vw;
        max-width: 35vw;

        & .mdi {
            font-size: 2vw;
        }
        & span {
            text-overflow: ellipsis;
            white-space: nowrap;
            word-break: break-all;
            overflow: hidden;

            &:nth-child(2) {
                color: rgba(255, 255, 255, 0.8);
                flex: 1;
            }
            &:nth-child(3) {
                font-weight: bold;
            }
        }
    }
}
</style>
