<template>
    <dialog ref="dialogRef">
        <SpinHeader
            :label="dialogTitle"
            v-if="dialogTitle"
        />

        <p v-if="dialogMessage">{{ dialogMessage }}</p>

        <div class="actions">
            <SpinButton
                label="Close"
                @click="close"
            />
        </div>
    </dialog>
</template>

<script setup>
import { ref, inject, onMounted, onUnmounted } from 'vue';
const emitter = inject('emitter');

const dialogRef = ref();

const dialogTitle = ref(false);
const dialogMessage = ref(false);

const close = () => {
    dialogRef.value.close();
};

onMounted(() => {
    emitter.on('alert-show', (options) => {
        dialogTitle.value = options.title ?? false;
        dialogMessage.value = options.message ?? false;

        dialogRef.value.showModal();
    });

    emitter.on('alert-close', () => {
        close();
    });
});

onUnmounted(() => {
    emitter.off('alert-show');
    emitter.off('alert-close');
});
</script>

<style lang="scss" scoped>
dialog {
    background: rgb(var(--colorBase));
    color: rgb(var(--colorBaseText));
    border-radius: 6px;
    padding: 20px;
    border: 0;
    width: 90%;
    max-width: 500px;
    flex-direction: column;
    gap: 10px;

    & p {
        color: rgba(var(--colorBaseText), 0.6);
        line-height: 1.5rem;
    }

    & .actions {
        margin-top: 20px;
        display: flex;
        gap: 10px;
        justify-content: flex-end;
    }

    &[open] {
        display: flex;
    }
    &::backdrop {
        backdrop-filter: blur(5px);
        background: rgba(0, 0, 0, 0.4);
    }
}
</style>
