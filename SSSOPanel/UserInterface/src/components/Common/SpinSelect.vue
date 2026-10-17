<template>
    <div
        ref="DOMDropdown"
        @focusout="closeDropdown"
        tabindex="0"
        class="spin-select"
        :class="`${disabled ? 'disabled' : ''}`"
    >
        <div
            @click="toggleDropdown"
            class="button"
        >
            <span :class="`mdi mdi-${options.find((x) => x.value === modelValue)?.icon}`"></span>
            <span class="value">{{ options.find((x) => x.value === modelValue)?.label || 'Select' }}</span>
            <span :class="`mdi mdi-menu-${showDropdown ? 'up' : 'down'}`"></span>
        </div>
        <transition name="default">
            <div
                v-show="showDropdown"
                ref="DOMOptions"
                class="options"
            >
                <div
                    v-for="option in options"
                    :key="option.value"
                    @click="selectOption(option.value)"
                    :class="`option ${modelValue === option.value ? 'selected' : ''}`"
                >
                    <span :class="`mdi mdi-${option.icon}`"></span>
                    <span
                        class="label"
                        :title="option.label"
                        >{{ option.label }}</span
                    >
                </div>
            </div>
        </transition>
    </div>
</template>

<script setup>
import { nextTick, ref } from 'vue';

const props = defineProps({
    modelValue: String,
    options: {
        type: Array,
        required: true,
    },
    disabled: {
        type: Boolean,
        default: false,
    },
});

const DOMDropdown = ref(null);
const DOMOptions = ref(null);
const showDropdown = ref(false);
const selectedValue = ref(props.modelValue);
const emit = defineEmits(['update:modelValue']);

const toggleDropdown = () => {
    if (props.disabled) return;
    showDropdown.value = !showDropdown.value;

    // Long lists scroll, start at the picked option
    if (showDropdown.value) {
        nextTick(() => DOMOptions.value?.querySelector('.selected')?.scrollIntoView({ block: 'nearest' }));
    }
};

const selectOption = (value) => {
    if (props.disabled) return;
    selectedValue.value = value;
    emit('update:modelValue', value);
    showDropdown.value = false;
};

const closeDropdown = () => {
    showDropdown.value = false;
};
</script>

<style lang="scss" scoped>
.spin-select {
    position: relative;
    min-width: 175px;
    // Long labels (channel names, scenes) get cut off instead of stretching the row
    max-width: 200px;
    transition: 0.15s ease-in-out all;

    & .button {
        cursor: pointer;
        background: rgba(var(--colorBaseText), 0.07);
        color: rgb(var(--colorBaseText));
        border-radius: 4px;
        padding: 0 15px;
        height: 40px;
        display: flex;
        justify-content: flex-start;
        align-items: center;
        gap: 6px;
        position: relative;
        overflow: hidden;
        transition: 0.15s ease-in-out all;

        & .value {
            flex-grow: 1;
            min-width: 0;
            overflow: hidden;
            white-space: nowrap;
            text-overflow: ellipsis;
            font-family: 'Work Sans', sans-serif;
            font-size: 1rem;
        }

        & .mdi {
            width: 24px;
            height: 24px;
            display: flex;
            justify-content: center;
            align-items: center;
            font-size: 20px;

            &.loading {
                animation: loadingLoop 0.4s linear infinite;
            }
        }
    }
    & .options {
        position: absolute;
        width: 100%;
        background: rgb(var(--colorBase2));
        color: rgb(var(--colorBaseText));
        z-index: 1;
        border-bottom-left-radius: 4px;
        border-bottom-right-radius: 4px;
        max-height: min(280px, 45vh);
        overflow-x: hidden;
        overflow-y: auto;
        overscroll-behavior: contain;
        box-shadow: 0 8px 16px rgba(0, 0, 0, 0.3);

        & .option {
            cursor: pointer;
            display: grid;
            grid-template-columns: 24px minmax(0, 1fr);
            gap: 10px;
            align-items: center;
            color: var(--colorBaseText);
            transition: 0.15s ease-in-out all;
            padding: 0 15px;
            height: 40px;

            & .mdi {
                width: 24px;
                height: 24px;
                display: flex;
                justify-content: center;
                align-items: center;
                font-size: 20px;
            }

            & .label {
                overflow: hidden;
                white-space: nowrap;
                text-overflow: ellipsis;
            }

            &.selected {
                background: rgb(var(--colorBase3));
            }
            &:hover {
                background: rgb(var(--colorBase3));
            }
        }
    }

    &.disabled {
        opacity: 0.3;

        & .button {
            cursor: not-allowed;
        }
    }
    &:not(.disabled) {
        & .button {
            &:hover {
                cursor: pointer;
                background: rgba(var(--colorBaseText), 0.14);
            }
        }
    }
}
</style>
