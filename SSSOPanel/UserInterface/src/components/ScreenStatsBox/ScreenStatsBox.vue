<template>
    <div
        ref="boxRef"
        class="screen-stats-box"
        :class="[side, { low: stats.health < lowHealth, failed: stats.failed }]"
    >
        <div class="tint muted"></div>
        <div class="noise"></div>
        <svg
            v-if="size.width"
            class="ring"
            :viewBox="`0 0 ${size.width} ${size.height}`"
        >
            <mask
                :id="maskId"
                maskUnits="userSpaceOnUse"
            >
                <path
                    :d="ring.path"
                    :style="{ strokeWidth: ring.stroke, strokeDasharray: `${stats.health * ring.length} ${ring.length}` }"
                />
            </mask>
            <image
                :href="currentTheme.background"
                v-bind="ring.image"
                :mask="`url(#${maskId})`"
            />
        </svg>

        <div class="inner">
            <div class="tint muted"></div>
            <div class="noise"></div>

            <ScreenRollingNumber
                class="score"
                :value="stats.score"
            />
            <span
                v-if="stats.failed"
                class="failed-tag"
                >FAILED</span
            >
            <div
                v-else
                class="list"
            >
                <div>
                    <span class="label">Combo</span>
                    <!-- Only "still a full combo" is known mid-song, the tier shows in the results -->
                    <span><ScreenRollingNumber :value="stats.combo" />x<template v-if="stats.fullCombo"> (FC)</template></span>
                </div>
                <div>
                    <span class="label">Multi</span>
                    <span><ScreenRollingNumber :value="stats.multiplier" />x</span>
                </div>
                <div>
                    <span class="label">Acc</span>
                    <span><ScreenRollingNumber :value="stats.accuracy" />%</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue';
import ScreenRollingNumber from '@/components/ScreenRollingNumber/ScreenRollingNumber.vue';
import { currentTheme } from '@/modules/useTheme';

const props = defineProps({
    // left drains clockwise, right counter-clockwise, so both sides mirror
    side: {
        type: String,
        default: 'left',
    },
    // { score, combo, multiplier, accuracy, health (0 to 1), failed, fullCombo }
    stats: {
        type: Object,
        required: true,
    },
    lowHealth: {
        type: Number,
        default: 0.3,
    },
});

// Ring thickness and box corner radius, in vw like the rest of the overlay
const RING_WIDTH = 0.2;
const CORNER_RADIUS = 0.5;

const maskId = `stats-ring-${props.side}`;
const boxRef = ref(null);
const size = ref({ width: 0, height: 0 });

// The box takes the height of the sets box next to it, so the ring is built from its measured size
let observer = null;
onMounted(() => {
    observer = new ResizeObserver(([entry]) => {
        size.value = { width: entry.contentRect.width, height: entry.contentRect.height };
    });
    observer.observe(boxRef.value);
});
onUnmounted(() => observer?.disconnect());

// A line along the edge starting at the top center. Butt caps end it with a straight cut across the edge.
const ring = computed(() => {
    const { width: w, height: h } = size.value;
    const vw = window.innerWidth / 100;
    const stroke = RING_WIDTH * vw;
    const i = stroke / 2;
    const corner = CORNER_RADIUS * vw;
    const r = corner - i;
    // The ring is visible from the top along the path, so draining clockwise needs the counter-clockwise path
    const sweep = props.side === 'right' ? 1 : 0;

    const path = sweep === 1 ? `M${w / 2},${i} H${w - corner} A${r},${r} 0 0 1 ${w - i},${corner} V${h - corner} A${r},${r} 0 0 1 ${w - corner},${h - i} H${corner} A${r},${r} 0 0 1 ${i},${h - corner} V${corner} A${r},${r} 0 0 1 ${corner},${i} Z` : `M${w / 2},${i} H${corner} A${r},${r} 0 0 0 ${i},${corner} V${h - corner} A${r},${r} 0 0 0 ${corner},${h - i} H${w - corner} A${r},${r} 0 0 0 ${w - i},${h - corner} V${corner} A${r},${r} 0 0 0 ${w - corner},${i} Z`;

    // Chrome ignores pathLength for dashes set from CSS, so the dashes use the real length
    const length = 2 * (w - 2 * corner) + 2 * (h - 2 * corner) + 2 * Math.PI * r;

    // The tint is 16:9, sized by hand like background-size: cover (an svg image ignores slice)
    const coverWidth = Math.max(w, (h * 16) / 9);
    const coverHeight = (coverWidth * 9) / 16;
    const image = { x: (w - coverWidth) / 2, y: (h - coverHeight) / 2, width: coverWidth, height: coverHeight };

    return { path, length, stroke, image };
});
</script>

<style lang="scss" scoped>
.screen-stats-box {
    position: relative;
    overflow: hidden;
    border-radius: 0.5vw;
    // Fixed, so both sides match and the sets box between them stays centered
    width: 12.5vw;
    color: #eee;

    & .tint {
        background: url('../../assets/background.svg');
        background-size: cover;
        position: absolute;
        inset: 0;

        // Muted version of the grainy background, a step lighter than the band behind it
        &.muted {
            filter: contrast(1) brightness(0.22) grayscale(2);
        }
    }

    & .noise {
        background: url('../../assets/noise.png');
        background-size: 5em;
        position: absolute;
        inset: 0;
        opacity: 0.4;
        mix-blend-mode: soft-light;
    }

    & .ring {
        position: absolute;
        inset: 0;
        width: 100%;
        height: 100%;

        & path {
            fill: none;
            stroke: #fff;
            stroke-linecap: butt;
            transition: stroke-dasharray 0.3s linear;
        }
    }

    // Pulses instead of changing color, the overlay has no warning colors
    &.low .ring {
        animation: lowHealth 0.8s ease-in-out infinite alternate;
    }

    & .inner {
        position: relative;
        height: calc(100% - 0.4vw);
        margin: 0.2vw;
        border-radius: 0.3vw;
        overflow: hidden;
        display: flex;
        flex-direction: column;
        justify-content: center;
        gap: 0.5vw;
        padding: 0 1vw;
        white-space: nowrap;

        & > *:not(.tint):not(.noise) {
            position: relative;
            z-index: 10;
        }

        & .score {
            font-weight: 600;
            font-size: 1.2vw;
            letter-spacing: 0.1vw;
            --digit-gap: 0.1vw;
        }

        & .list {
            display: grid;
            gap: 0.25vw;
            font-size: 0.8vw;
            letter-spacing: 0.05vw;
            --digit-gap: 0.05vw;

            & > div {
                display: flex;
                justify-content: space-between;
                gap: 1vw;
            }

            & .label {
                color: rgba(255, 255, 255, 0.6);
            }
        }

        // Same as the "OF" in the sets box
        & .failed-tag {
            background: #111;
            color: #ededed;
            font-weight: bold;
            font-size: 0.75vw;
            letter-spacing: 0.25vw;
            padding: 0.25vw 0.5vw;
            border-radius: 0.25vw;
        }
    }

    &.failed .inner .score {
        color: rgba(255, 255, 255, 0.4);
        text-decoration: line-through;
    }

    // The score (and FAILED) sits on the side facing the sets box, the list always spans the box
    &.left .inner {
        align-items: flex-end;
    }

    &.right .inner {
        align-items: flex-start;
    }

    & .inner .list {
        align-self: stretch;
    }
}

@keyframes lowHealth {
    from {
        opacity: 1;
    }
    to {
        opacity: 0.35;
    }
}
</style>
