<template>
    <span class="rolling-number">
        <span
            v-for="char in chars"
            :key="char.key"
            class="slot"
            :class="{ digit: char.digit }"
        >
            <transition name="roll">
                <span
                    :key="char.value"
                    class="char"
                    >{{ char.value }}</span
                >
            </transition>
        </span>
    </span>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
    value: {
        type: [String, Number],
        required: true,
    },
});

// Keyed from the right, so digits stay in their slot when the number gets longer (999 > 1,000)
const chars = computed(() => {
    const text = String(props.value);
    return text.split('').map((value, index) => ({ key: text.length - index, value, digit: /\d/.test(value) }));
});
</script>

<style lang="scss" scoped>
.rolling-number {
    display: inline-flex;

    & .slot {
        position: relative;
        display: inline-block;
        overflow: hidden;
    }

    & .char {
        display: inline-block;
    }

    // Every digit gets the width of a 0, so numbers don't shift around while they change.
    // Set --digit-gap to the parent's letter-spacing to keep the spacing between digits.
    & .slot.digit {
        width: calc(1ch + var(--digit-gap, 0px));

        & .char {
            width: 100%;
            text-align: center;
            letter-spacing: 0;
        }
    }
}

// Has to finish before the next update, scores come in about every 100ms
.roll-enter-active,
.roll-leave-active {
    transition: transform 0.08s ease-out;
}
.roll-leave-active {
    position: absolute;
    left: 0;
    top: 0;
}
.roll-enter-from {
    transform: translateY(-100%);
}
.roll-leave-to {
    transform: translateY(100%);
}
</style>
