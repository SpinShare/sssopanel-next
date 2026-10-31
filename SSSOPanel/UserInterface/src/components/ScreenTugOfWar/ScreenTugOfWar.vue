<template>
    <div
        class="tug-row"
        :class="{ shown: !!tug }"
    >
        <!-- Grows and shrinks instead of popping in, so the row above slides instead of jumping -->
        <div
            class="tug"
            v-if="shownTug"
        >
            <div class="tug-bar">
                <div class="tint muted"></div>
                <div class="noise"></div>
                <div
                    class="tug-fill left"
                    :style="{ width: Math.max(0, shownLead) * 50 + '%' }"
                >
                    <div class="tint themed"></div>
                    <div class="noise"></div>
                </div>
                <div
                    class="tug-fill right"
                    :style="{ width: Math.max(0, -shownLead) * 50 + '%' }"
                >
                    <div class="tint themed"></div>
                    <div class="noise"></div>
                </div>
                <!-- Keyed by side, so a lead change fades out on one side and in on the other -->
                <transition name="lead">
                    <span
                        v-if="leadSide"
                        :key="leadSide"
                        class="tug-lead"
                        :class="leadSide"
                    >
                        +<ScreenRollingNumber :value="Math.abs(shownTug.diff).toLocaleString()" />
                    </span>
                </transition>
            </div>
        </div>
    </div>
</template>

<script setup>
import { onUnmounted, ref, watch } from 'vue';

import ScreenRollingNumber from '@/components/ScreenRollingNumber/ScreenRollingNumber.vue';

const props = defineProps({
    // { diff, signedLead } of the live lead, null while there is nothing to compare
    tug: { type: Object, default: null },
    round: { type: Number, default: 0 },
});

// Last bar data, kept on screen while the row slides closed
const shownTug = ref(null);
watch(
    () => props.tug,
    (value) => {
        if (value) shownTug.value = value;
    },
);

// Side of the lead pill. A tie keeps it up for a bit (rolling to +0) instead of blinking out
const TUG_TIE_LINGER = 3000;
const leadSide = ref(null);
let leadTimer = null;
watch(
    () => props.tug?.diff ?? 0,
    (diff) => {
        if (diff !== 0) {
            clearTimeout(leadTimer);
            leadTimer = null;
            leadSide.value = diff > 0 ? 'left' : 'right';
        } else if (leadSide.value && !leadTimer) {
            leadTimer = setTimeout(() => {
                leadTimer = null;
                leadSide.value = null;
            }, TUG_TIE_LINGER);
        }
    },
);
// A new song starts tied, the old side shouldn't show up as +0
watch(
    () => props.round,
    () => {
        clearTimeout(leadTimer);
        leadTimer = null;
        leadSide.value = null;
    },
);
onUnmounted(() => clearTimeout(leadTimer));

// One animated value for both fills. It slides through the middle when the lead changes hands, so the two
// sides never grow and shrink at the same time (which looked like both were ahead for a moment)
const TUG_EASE = 0.1; // seconds, how quickly the bar catches up
const shownLead = ref(0);
let tugFrame = null;
let tugFrameTime = 0;
const animateTug = (time) => {
    const target = props.tug?.signedLead ?? 0;
    const dt = tugFrameTime ? (time - tugFrameTime) / 1000 : 0;
    tugFrameTime = time;

    shownLead.value += (target - shownLead.value) * (1 - Math.exp(-dt / TUG_EASE));
    if (Math.abs(target - shownLead.value) < 0.0005) shownLead.value = target;

    tugFrame = shownLead.value === target ? null : requestAnimationFrame(animateTug);
};
watch(
    () => props.tug?.signedLead ?? 0,
    () => {
        if (tugFrame) return;
        tugFrameTime = 0;
        tugFrame = requestAnimationFrame(animateTug);
    },
);
onUnmounted(() => tugFrame && cancelAnimationFrame(tugFrame));
</script>

<style lang="scss" scoped>
.tug-row {
    display: grid;
    grid-template-rows: 0fr;
    opacity: 0;
    transition: grid-template-rows 0.5s ease-in-out, opacity 0.5s ease-in-out;

    &.shown {
        grid-template-rows: 1fr;
        opacity: 1;
    }

    & > .tug {
        overflow: hidden;
        min-height: 0;
    }
}

.tug {
    display: grid;
    align-items: center;

    // Muted version of the grainy background, a step lighter than the band behind it
    & .tint.muted {
        filter: contrast(1) brightness(0.22) grayscale(2);
    }

    & .tint {
        background: url('../../assets/background.svg');
        background-size: cover;
        position: absolute;
        inset: 0;
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5em;
        position: absolute;
        inset: 0;
        opacity: 0.4;
        mix-blend-mode: soft-light;
    }

    & .tug-bar {
        margin-top: 1vw;
        height: 2vw;
        border-radius: 0.5vw;
        position: relative;
        overflow: hidden;

        // Middle line both bars grow out from
        &::after {
            content: '';
            position: absolute;
            left: calc(50% - 0.0625vw);
            top: 0;
            bottom: 0;
            width: 0.125vw;
            background: rgba(255, 255, 255, 0.4);
            z-index: 20;
        }

        & .tug-fill {
            position: absolute;
            top: 0;
            bottom: 0;
            overflow: hidden;

            &.left {
                right: 50%;
            }

            &.right {
                left: 50%;
            }

            & .tint {
                background-image: var(--themeBackground);
                filter: none;
            }
        }

        // Next to the middle line on the leading side, a dark pill like the "OF" in the sets box
        // so it reads the same on the fill, the track and every theme
        & .tug-lead {
            position: absolute;
            top: 50%;
            transform: translateY(-50%);
            display: flex;
            align-items: center;
            font-size: 0.8vw;
            font-weight: 600;
            letter-spacing: 0.1vw;
            --digit-gap: 0.1vw;
            background: #111;
            color: #ededed;
            padding: 0.2vw 0.5vw;
            border-radius: 0.25vw;
            z-index: 10;
            white-space: nowrap;

            &.left {
                right: calc(50% + 0.5vw);
            }

            &.right {
                left: calc(50% + 0.5vw);
            }
        }
    }
}

.lead-enter-active,
.lead-leave-active {
    transition: opacity 0.25s ease-out;
}
.lead-enter-from,
.lead-leave-to {
    opacity: 0;
}
</style>
