<template>
    <ScreenLayout>
        <section class="screen-match-recap">
            <div class="tint themed"></div>
            <div class="noise"></div>

            <!-- Straight on the themed background with avatars, like match-overview -->
            <div class="content">
                <ScreenMatchResults
                    v-if="shownResults"
                    :results="shownResults"
                    :player1="player1"
                    :player2="player2"
                    :winner-name="winnerName"
                    :history="history"
                    :slide="resultsSlide"
                    :backdrop="false"
                    avatars
                >
                    <!-- How chat's Twitch prediction went, once it paid out -->
                    <template #below-title>
                        <transition name="chat-pick">
                            <div
                                v-if="chatPick"
                                class="chat-pick"
                            >
                                <span class="mdi mdi-twitch"></span>
                                <span class="status">{{ chatPick.status }}</span>
                                <span>{{ chatPick.text }}</span>
                            </div>
                        </transition>
                    </template>
                </ScreenMatchResults>
            </div>

            <div class="marquees">
                <div class="marquee top">
                    <template
                        v-for="n in 10"
                        :key="n"
                    >
                        <span>MATCH RESULTS</span>
                    </template>
                </div>
                <div class="marquee bottom">
                    <template
                        v-for="n in 10"
                        :key="n"
                    >
                        <span>MATCH RESULTS</span>
                    </template>
                </div>
            </div>
        </section>
    </ScreenLayout>
</template>

<script setup>
import ScreenLayout from '../../layouts/ScreenLayout.vue';
import ScreenMatchResults from '@/components/ScreenMatchResults/ScreenMatchResults.vue';
import { computed, inject, onMounted, onUnmounted, ref } from 'vue';
import { matchWinner } from '@/modules/useSrxd';
import useSpulti, { predictionVerdict, sideMap } from '@/modules/useSpulti';
const emitter = inject('emitter');

const player1 = ref(null);
const player2 = ref(null);
const ingame = ref({ player1: '', player2: '' });
const scores = ref(null);
const results = ref(null);
const history = ref([]);
const resultsSlide = ref(0);
const spulti = useSpulti();
const spultiMatchId = ref(null);
const chatPickVisible = ref(true);

// Chat's pick once Twitch paid out, the same call as the other screens (predictionVerdict)
const chatPick = computed(() => {
    if (!chatPickVisible.value || spulti.value.match?.id !== spultiMatchId.value) return null;
    const verdict = predictionVerdict(spulti.value.prediction);
    if (!verdict) return null;
    const players = { player1: player1.value, player2: player2.value };
    const player = players[sideMap(spulti.value.match, players)[verdict.side]];
    const name = player?.spinshareProfile?.username ?? player?.name ?? verdict.name;
    if (verdict.called == null) return { status: 'Chat was split!', text: `${verdict.share}% backed ${name}` };
    return verdict.called ? { status: 'Chat called it!', text: `${verdict.share}% backed ${name}` } : { status: 'Upset!', text: `only ${verdict.share}% backed ${name}` };
});

// Null without a winner or history (scores edited by hand), the cards then show the last results on their own
const winnerName = computed(() => {
    const winner = matchWinner(scores.value);
    if (!winner || !history.value.length) return null;

    const player = winner === 'player1' ? player1.value : player2.value;
    return player?.spinshareProfile?.username || ingame.value[winner] || null;
});

const shownResults = computed(() => history.value[history.value.length - 1]?.results ?? results.value);

const applyState = (state) => {
    player1.value = state?.currentMatch?.players?.player1 ?? player1.value;
    player2.value = state?.currentMatch?.players?.player2 ?? player2.value;
    ingame.value = state?.currentMatch?.ingame ?? ingame.value;
    scores.value = state?.currentMatch?.scores ?? scores.value;
    results.value = state?.currentMatch?.results ?? results.value;
    history.value = state?.currentMatch?.history ?? history.value;
    resultsSlide.value = state?.currentMatch?.resultsSlide ?? resultsSlide.value;
    spultiMatchId.value = state?.currentMatch?.spultiMatchId ?? spultiMatchId.value;
    chatPickVisible.value = state?.chatPickVisible ?? chatPickVisible.value;
};

onMounted(() => {
    emitter.on('state-get-response', (state) => {
        applyState(state);
    });
    emitter.on('state-set-response', (state) => {
        applyState(state);
    });

    window.electronAPI.send(
        'message',
        JSON.stringify({
            command: 'state-get',
        }),
    );
});
onUnmounted(() => {
    emitter.off('state-get-response');
    emitter.off('state-set-response');
});
</script>

<style lang="scss" scoped>
// Not .screen-match-results, that is the results component's root and scoped styles reach a child's root
.screen-match-recap {
    color: #222;
    position: relative;
    overflow: hidden;

    & .tint {
        background: url('../../assets/background.svg');
        background-size: cover;
        position: absolute;
        inset: 0;

        &.themed {
            background-image: var(--themeBackground);
            filter: none;
        }
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5em;
        position: absolute;
        inset: 0;
        mix-blend-mode: soft-light;
    }

    & .content {
        position: absolute;
        top: 5vw;
        bottom: 5vw;
        left: 0;
        right: 0;
        z-index: 10;
    }

    // Same slant as match-overview
    & .marquees {
        transform: rotate(-4deg) scale(1);
        position: absolute;
        top: 4vw;
        bottom: 4vw;
        left: 0;
        right: 0;
        z-index: 3;

        & .marquee {
            white-space: nowrap;
            display: flex;
            position: absolute;
            will-change: transform;
            font-size: 3vw;
            font-weight: 800;
            color: rgba(0, 0, 0, 0.2);

            & span {
                padding-right: 1em;
            }

            &.top {
                top: 0;
                left: 0;
                animation: marqueeLeft 10s infinite linear;
            }

            &.bottom {
                bottom: 0;
                left: 0;
                animation: marqueeRight 10s infinite linear;
            }
        }
    }
}

// 10 copies, so -10% is exactly one copy and the loop is seamless
@keyframes marqueeLeft {
    from {
        transform: translateX(-10%);
    }
    to {
        transform: translateX(0);
    }
}

@keyframes marqueeRight {
    from {
        transform: translateX(0);
    }
    to {
        transform: translateX(-10%);
    }
}

// A smaller dark tab under the title, same look as the title on this screen
.chat-pick {
    display: flex;
    align-items: center;
    gap: 0.6vw;
    padding: 0.4vw 1.2vw;
    border-radius: 0.5vw;
    background: #111;
    color: #ededed;
    font-size: 1vw;
    letter-spacing: 0.05vw;

    & .status {
        font-weight: 800;
    }
}
.chat-pick-enter-active {
    transition: opacity 0.4s cubic-bezier(0.22, 1, 0.36, 1);
}
.chat-pick-leave-active {
    transition: opacity 0.25s cubic-bezier(0.55, 0, 1, 0.45);
}
.chat-pick-enter-from,
.chat-pick-leave-to {
    opacity: 0;
}
</style>
