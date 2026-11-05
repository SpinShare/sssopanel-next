<template>
    <div
        class="spin-menu"
        ref="root"
    >
        <SpinButton
            :icon="icon"
            :color="color"
            v-tooltip="open ? false : tooltip"
            @click="open = !open"
        />
        <transition name="spin-menu">
            <!-- Opens upward, it lives in the footer -->
            <div
                v-if="open"
                class="items"
            >
                <slot />
            </div>
        </transition>
    </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref } from 'vue';
import SpinButton from '@/components/Common/SpinButton.vue';

defineProps({
    icon: {
        type: String,
        required: true,
    },
    color: {
        type: String,
        default: 'default',
    },
    tooltip: {
        type: String,
        default: '',
    },
});

const open = ref(false);
const root = ref(null);

// Stays open while toggling items, closes on a click anywhere else or Escape
const onPointerDown = (event) => {
    if (open.value && !root.value?.contains(event.target)) open.value = false;
};
const onKeyDown = (event) => {
    if (event.key === 'Escape') open.value = false;
};

onMounted(() => {
    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
});
onUnmounted(() => {
    document.removeEventListener('pointerdown', onPointerDown);
    document.removeEventListener('keydown', onKeyDown);
});
</script>

<style lang="scss" scoped>
.spin-menu {
    position: relative;

    & .items {
        position: absolute;
        right: 0;
        bottom: calc(100% + 10px);
        z-index: 100;
        min-width: 200px;
        display: flex;
        flex-direction: column;
        gap: 5px;
        padding: 8px;
        border-radius: 8px;
        background: rgb(var(--colorBase));
        border: 1px solid rgba(255, 255, 255, 0.1);
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);

        & :deep(button) {
            justify-content: flex-start;
            width: 100%;
        }
    }
}

.spin-menu-enter-active,
.spin-menu-leave-active {
    transition: opacity 0.15s ease-out, transform 0.15s ease-out;
}
.spin-menu-enter-from,
.spin-menu-leave-to {
    opacity: 0;
    transform: translateY(5px);
}
</style>
