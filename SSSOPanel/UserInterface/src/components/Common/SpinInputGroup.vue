<template>
    <div class="group">
        <header>
            <span>{{ header }}</span>
            <span
                v-if="collapsable"
                class="mdi"
                :class="{
                    'mdi-chevron-down': isOpen,
                    'mdi-chevron-up': !isOpen,
                }"
                @click="isOpen = !isOpen"
            ></span>
        </header>
        <div
            class="items"
            v-show="!collapsable || isOpen"
        >
            <slot />
        </div>
    </div>
</template>

<script setup>
import { ref } from 'vue';

const props = defineProps({
    header: {
        type: String,
        default: '',
    },
    collapsable: {
        type: Boolean,
        default: false,
    },
    collapsed: {
        type: Boolean,
        default: false,
    },
});

const isOpen = ref(!props.collapsed);
</script>

<style lang="scss" scoped>
.group {
    display: grid;
    gap: 15px;

    &:not(:first-child) header {
        margin-top: 15px;
    }
    & header {
        font-size: 0.8rem;
        font-weight: bold;
        padding: 0 20px;
        background: rgba(var(--colorBaseText), 0.8);
        color: rgb(var(--colorBase));
        margin: 0 -20px;
        display: flex;
        align-items: center;

        & span:nth-child(1) {
            flex-grow: 1;
            padding: 6px 0;
        }
        & span:nth-child(2) {
            font-size: 22px;
            width: 24px;
            height: 24px;
            display: flex;
            justify-content: center;
            align-items: center;

            &:hover {
                background: rgba(0, 0, 0, 0.1);
                cursor: pointer;
            }
        }
    }
    & .items {
        display: grid;
        gap: 15px;
    }
}
</style>
