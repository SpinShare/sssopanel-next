<template>
    <dialog ref="dialogRef">
        <SpinHeader
            :label="dialogTitle"
            v-if="dialogTitle"
        />

        <p v-if="dialogMessage">{{ dialogMessage }}</p>

        <SpinInput
            v-for="toggle in dialogSwitches"
            :key="toggle.key"
            :label="toggle.label"
            type="horizontal"
        >
            <SpinSwitch v-model="toggle.value" />
        </SpinInput>

        <div class="actions">
            <SpinButton
                v-for="action in dialogActions"
                :key="action.label"
                :label="action.label"
                :color="action.color ?? 'default'"
                @click="runAction(action)"
            />
        </div>
    </dialog>
</template>

<script setup>
import { ref, inject, onMounted, onUnmounted } from 'vue';
import SpinInput from '@/components/Common/SpinInput.vue';
import SpinSwitch from '@/components/Common/SpinSwitch.vue';
const emitter = inject('emitter');

const dialogRef = ref();

const dialogTitle = ref(false);
const dialogMessage = ref(false);
const dialogActions = ref([]);
// Optional switches, actions get their values as { key: value }
const dialogSwitches = ref([]);

const close = () => {
    dialogRef.value.close();
};

const runAction = (action) => {
    close();
    action.click?.(Object.fromEntries(dialogSwitches.value.map((toggle) => [toggle.key, toggle.value])));
};

onMounted(() => {
    emitter.on('alert-show', (options) => {
        dialogTitle.value = options.title ?? false;
        dialogMessage.value = options.message ?? false;
        dialogActions.value = options.actions ?? [{ label: 'Close' }];
        dialogSwitches.value = (options.switches ?? []).map((toggle) => ({ ...toggle }));

        // A newer alert replaces the open one, showModal throws on an already open dialog
        if (!dialogRef.value.open) dialogRef.value.showModal();
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
