<template>
    <!-- Not a <label>, that forwards hover and clicks on the text to the first button inside -->
    <div
        class="setup-input"
        :class="`type-${type}`"
    >
        <div class="text">
            <div class="label">{{ label }}</div>
            <div
                class="hint"
                v-if="hint"
            >
                {{ hint }}
            </div>
        </div>
        <div class="input">
            <slot />
        </div>
    </div>
</template>

<script setup>
defineProps({
    label: {
        type: String,
        required: true,
    },
    hint: {
        type: [String, Boolean],
        default: false,
    },
    type: {
        type: String,
        default: 'default',
    },
});
</script>

<style lang="scss">
.setup-input {
    display: grid;
    gap: 10px;

    &.type-horizontal {
        // minmax so a long word in a hint wraps instead of pushing the input out of a narrow window
        grid-template-columns: minmax(0, 1fr) auto;
        gap: 50px;
        align-items: center;

        // Narrow window: the input goes under its text instead of squeezing it
        @media (max-width: 600px) {
            grid-template-columns: minmax(0, 1fr);
            gap: 10px;
            justify-items: start;
        }
    }
    &.type-path {
        & .input {
            // minmax, the input's own min width pushed the buttons out of narrow windows
            grid-template-columns: minmax(0, 1fr) auto auto;
        }
    }

    & .text {
        display: flex;
        flex-direction: column;
        gap: 4px;

        & .label {
            font-size: 0.9rem;
        }
        & .hint {
            color: rgba(var(--colorBaseText), 0.4);
            line-height: 1.25rem;
            overflow-wrap: anywhere;
        }
    }
    & .input {
        display: grid;
        gap: 5px;

        & input[type='text'],
        & input[type='password'],
        & input[type='number'] {
            background: rgba(var(--colorBaseText), 0.07);
            border: 0;
            color: rgb(var(--colorBaseText));
            font-family: 'Work Sans', 'Noto Sans JP', sans-serif;
            font-size: 1rem;
            border-radius: 4px;
            padding: 0 15px;
            height: 40px;
            transition: 0.15s ease-in-out all;

            &:disabled {
                opacity: 0.4;
                cursor: not-allowed;
            }
            &:not(:disabled):hover {
                background: rgba(var(--colorBaseText), 0.14);
            }
        }
        & .select {
            position: relative;
            cursor: pointer;

            & select {
                width: 100%;
                appearance: none;
                background: rgba(var(--colorBaseText), 0.07);
                border: 0;
                color: rgb(var(--colorBaseText));
                font-family: 'Work Sans', 'Noto Sans JP', sans-serif;
                font-size: 1rem;
                border-radius: 4px;
                padding: 0 15px;
                height: 40px;
                margin-right: 25px;

                & option {
                    background: rgb(var(--colorBase));
                    color: rgb(var(--colorBaseText));
                }
            }

            & .mdi {
                pointer-events: none;
                position: absolute;
                right: 10px;
                top: 9px;
                font-size: 22px;
            }
        }
    }
}
</style>
